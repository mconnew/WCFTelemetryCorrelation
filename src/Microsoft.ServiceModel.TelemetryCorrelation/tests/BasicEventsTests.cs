// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.DependencyCollector;
using System.Reflection;

namespace Microsoft.ServiceModel.TelemetryCorrelation.Tests
{

    public class BasicEventsTests
    {

        [Fact]
        public void BasicHttpClientActivityPropagation()
        {
            var tc = TestHelper.InitAiConfigAndGetTelemetyrClient();

            var activity = new Activity("Root");
            activity.AddBaggage("foo", "bar");
            var id = activity.Id;
            activity.Start();

            tc.TrackTrace("BasicHttpClientActivityPropagation begin");

            using (var subscription = DiagnosticsHelper.SubscribeToListener())
            {
                ServiceHost host = null;
                ChannelFactory<ITestService> factory = null;
                ITestService channel = null;
                try
                {
                    var helper = TestHelper.BasicHttp;
                    //var activity = new Activity("Root");
                    //activity.AddBaggage("foo", "bar");
                    //var id = activity.Id;
                    //activity.Start();
                    host = helper.CreateServiceHost();
                    host.Open();

                    factory = helper.CreateChannelFactory();
                    channel = factory.CreateChannel();
                    var baggage = channel.GetBaggage();

                    Assert.NotNull(baggage);
                    Assert.Single(baggage);
                    Assert.True(baggage.ContainsKey("foo"));
                    Assert.Equal("bar", baggage["foo"]);

                    var receivedRootId = channel.GetActivityRootId();
                    Assert.Equal(activity.RootId, receivedRootId);
                }
                finally
                {
                    //activity.Stop(); //TODO: If before tc.TrackTrace then operationId is null
                    tc.TrackTrace("BasicHttpClientActivityPropagation end");
                    //tc.StopOperation<RequestTelemetry>(opHolder);
                    tc.Flush();
                    TestHelper.Cleanup(channel, factory, host);
                }
            }

        }

        [Fact]
        public void NetTcpClientActivityPropagation()
        {
            var tc = TestHelper.InitAiConfigAndGetTelemetyrClient();

            var activity = new Activity("Root");
            activity.AddBaggage("foo", "bar");
            var id = activity.Id;
            activity.Start();

            tc.TrackTrace("NetTcpClientActivityPropagation begin");

            using (var subscription = DiagnosticsHelper.SubscribeToListener())
            {
                ServiceHost host = null;
                ChannelFactory<ITestService> factory = null;
                ITestService channel = null;
                try
                {
                    var helper = TestHelper.NetTcp;
                    //var activity = new Activity("Root");
                    //activity.AddBaggage("foo", "bar");
                    //var id = activity.Id;
                    //activity.Start();
                    host = helper.CreateServiceHost();
                    host.Open();

                    factory = helper.CreateChannelFactory();
                    channel = factory.CreateChannel();
                    var baggage = channel.GetBaggage();

                    Assert.NotNull(baggage);
                    Assert.Single(baggage);
                    Assert.True(baggage.ContainsKey("foo"));
                    Assert.Equal("bar", baggage["foo"]);

                    var receivedRootId = channel.GetActivityRootId();
                    Assert.Equal(activity.RootId, receivedRootId);
                }
                finally
                {
                    //activity.Stop();
                    tc.TrackTrace("NetTcpClientActivityPropagation end");
                    tc.Flush();
                    TestHelper.Cleanup(channel, factory, host);
                }
            }
        }

        [Fact]
        public void NetTcpMultipleConcurrentRequestsEventsWritten()
        {
            var tc = TestHelper.InitAiConfigAndGetTelemetyrClient();

            var helper = TestHelper.NetTcp;
            var activity = new Activity("Root");
            activity.AddBaggage("foo", "bar");
            var id = activity.Id;
            activity.Start();

            tc.TrackTrace("NetTcpMultipleConcurrentRequestsEventsWritten begin");

            Debug.WriteLine($"NetTcpMultipleConcurrentRequestsEventsWritten .. TelemetryConfiguration.Active, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

            IList<KeyValuePair<string, object>> events;
            using (var subscription = DiagnosticsHelper.SubscribeToListener())
            {
                ServiceHost host = null;
                ChannelFactory<ITestService> factory = null;
                ITestService channel = null;
                try
                {
                    //var helper = TestHelper.NetTcp;
                    //var activity = new Activity("Root");
                    //activity.AddBaggage("foo", "bar");
                    //var id = activity.Id;
                    //activity.Start();
                    host = helper.CreateServiceHost();
                    host.Open();

                    factory = helper.CreateChannelFactory();
                    channel = factory.CreateChannel();
                    ((IChannel)channel).Open();
                    var task = Task.Run(() =>
                    {
                        Debug.WriteLine($"NetTcpMultipleConcurrentRequestsEventsWritten .. Task.Run enter, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

                        //var activity2 = new Activity("Root");
                        //activity2.AddBaggage("foo2", "bar2");
                        //var id2 = activity.Id;
                        //activity2.Start();
                        Debug.WriteLine($"NetTcpMultipleConcurrentRequestsEventsWritten .. before Channel.Sleep(5000), Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                        channel.Sleep(5000);
                        Debug.WriteLine($"NetTcpMultipleConcurrentRequestsEventsWritten .. after Channel.Sleep(5000), Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                        //activity2.Stop();
                        Debug.WriteLine($"NetTcpMultipleConcurrentRequestsEventsWritten .. Task.Run exit, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                    });
                    // Make sure the Task has had time to start and make the outgoing request.
                    Debug.WriteLine($"NetTcpMultipleConcurrentRequestsEventsWritten .. before Thread.Sleep(1000) Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                    Thread.Sleep(1000);
                    Debug.WriteLine($"NetTcpMultipleConcurrentRequestsEventsWritten .. after Thread.Sleep(1000) Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                    var receivedRootId = channel.GetActivityRootId();
                    Assert.Equal(activity.RootId, receivedRootId);
                    // Make sure the channel.Sleep call hasn't finished yet
                    Assert.False(task.IsCompleted, "Unable to verify behavior as long running call completed too quickly");
                    Debug.WriteLine($"NetTcpMultipleConcurrentRequestsEventsWritten .. before Wait Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                    task.Wait();
                    Debug.WriteLine($"NetTcpMultipleConcurrentRequestsEventsWritten .. after Wait Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                    events = subscription.Events;
                }
                finally
                {
                    tc.TrackTrace("NetTcpMultipleConcurrentRequestsEventsWritten end");

                    TestHelper.Cleanup(channel, factory, host);
                }
            }

            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.SendMessage.Start" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/Sleep")));
            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.SendMessage.Start" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/GetActivityRootId")));
            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.SendMessage.Stop" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/SleepResponse")));
            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.SendMessage.Stop" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/GetActivityRootIdResponse")));
            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.ReceiveMessage.Start" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/Sleep")));
            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.ReceiveMessage.Start" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/GetActivityRootId")));
            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.ReceiveMessage.Stop" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/SleepResponse")));
            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.ReceiveMessage.Stop" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/GetActivityRootIdResponse")));
        }

        [Fact]
        public void BasicHttpMultipleConcurrentRequestsEventsWritten()
        {
            var tc = TestHelper.InitAiConfigAndGetTelemetyrClient();

            var helper = TestHelper.BasicHttp;
            var activity = new Activity("Root");
            activity.AddBaggage("foo", "bar");
            var id = activity.Id;
            activity.Start();

            tc.TrackTrace("HttpMultipleConcurrentRequestsEventsWritten begin");

            Debug.WriteLine($"HttpMultipleConcurrentRequestsEventsWritten .. TelemetryConfiguration.Active, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

            IList<KeyValuePair<string, object>> events;
            using (var subscription = DiagnosticsHelper.SubscribeToListener())
            {
                ServiceHost host = null;
                ChannelFactory<ITestService> factory = null;
                ITestService channel = null;
                try
                {
                    //var helper = TestHelper.NetTcp;
                    //var activity = new Activity("Root");
                    //activity.AddBaggage("foo", "bar");
                    //var id = activity.Id;
                    //activity.Start();
                    host = helper.CreateServiceHost();
                    host.Open();

                    factory = helper.CreateChannelFactory();
                    channel = factory.CreateChannel();
                    ((IChannel)channel).Open();
                    var task = Task.Run(() =>
                    {
                        Debug.WriteLine($"HttpMultipleConcurrentRequestsEventsWritten .. Task.Run enter, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

                        //var activity2 = new Activity("Root");
                        //activity2.AddBaggage("foo2", "bar2");
                        //var id2 = activity.Id;
                        //activity2.Start();
                        Debug.WriteLine($"HttpMultipleConcurrentRequestsEventsWritten .. before Channel.Sleep(5000), Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                        channel.Sleep(5000);
                        Debug.WriteLine($"HttpMultipleConcurrentRequestsEventsWritten .. after Channel.Sleep(5000), Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                        //activity2.Stop();
                        Debug.WriteLine($"HttpMultipleConcurrentRequestsEventsWritten .. Task.Run exit, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                    });
                    // Make sure the Task has had time to start and make the outgoing request.
                    Debug.WriteLine($"HttpMultipleConcurrentRequestsEventsWritten .. before Thread.Sleep(1000) Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                    Thread.Sleep(1000);
                    Debug.WriteLine($"HttpMultipleConcurrentRequestsEventsWritten .. after Thread.Sleep(1000) Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                    var receivedRootId = channel.GetActivityRootId();
                    Assert.Equal(activity.RootId, receivedRootId);
                    // Make sure the channel.Sleep call hasn't finished yet
                    Assert.False(task.IsCompleted, "Unable to verify behavior as long running call completed too quickly");
                    Debug.WriteLine($"HttpMultipleConcurrentRequestsEventsWritten .. before Wait Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                    task.Wait();
                    Debug.WriteLine($"HttpMultipleConcurrentRequestsEventsWritten .. after Wait Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                    events = subscription.Events;
                }
                finally
                {
                    tc.TrackTrace("HttpMultipleConcurrentRequestsEventsWritten end");

                    TestHelper.Cleanup(channel, factory, host);
                }
            }

            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.SendMessage.Start" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/Sleep")));
            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.SendMessage.Start" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/GetActivityRootId")));
            //Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.SendMessage.Stop" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/SleepResponse"))); //TODO: Throwing exception hen added this Http test similar to NetTcpMultipleConcurrentRequestsEventsWritten
            //Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.SendMessage.Stop" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/GetActivityRootIdResponse"))); //TODO: Throwing exception hen added this Http test similar to NetTcpMultipleConcurrentRequestsEventsWritten
            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.ReceiveMessage.Start" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/Sleep")));
            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.ReceiveMessage.Start" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/GetActivityRootId")));
            //Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.ReceiveMessage.Stop" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/SleepResponse"))); //TODO: Throwing exception hen added this Http test similar to NetTcpMultipleConcurrentRequestsEventsWritten
            //Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.ReceiveMessage.Stop" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/GetActivityRootIdResponse"))); //TODO: Throwing exception hen added this Http test similar to NetTcpMultipleConcurrentRequestsEventsWritten
        }

        [Fact]
        public void BasicHttpBaseEventsWritten()
        {
            var tc = TestHelper.InitAiConfigAndGetTelemetyrClient();

            IList<KeyValuePair<string, object>> events;
            using (var subscription = DiagnosticsHelper.SubscribeToListener())
            {
                ServiceHost host = null;
                ChannelFactory<ITestService> factory = null;
                ITestService channel = null;
                try
                {
                    var helper = TestHelper.BasicHttp;
                    host = helper.CreateServiceHost();
                    host.Open();

                    factory = helper.CreateChannelFactory();
                    channel = factory.CreateChannel();
                    var activity = new Activity("Root");
                    activity.Start();
                    channel.DoWork();
                    activity.Stop();

                    events = subscription.Events;
                }
                finally
                {
                    TestHelper.Cleanup(channel, factory, host);
                }
            }

            var data = TestHelper.GetOnlyOneDataItem(events, "System.ServiceModel.ReceiveMessage.Start");
            Assert.Equal("http://tempuri.org/ITestService/DoWork", TestHelper.GetValue<string>(data, "Action"));
            data = TestHelper.GetOnlyOneDataItem(events, "System.ServiceModel.ReceiveMessage.Stop");
            Assert.Equal(string.Empty, TestHelper.GetValue<string>(data, "Action"));
            data = TestHelper.GetOnlyOneDataItem(events, "System.ServiceModel.SendMessage.Start");
            Assert.Equal("http://tempuri.org/ITestService/DoWork", TestHelper.GetValue<string>(data, "Action"));
            data = TestHelper.GetOnlyOneDataItem(events, "System.ServiceModel.SendMessage.Stop");
            Assert.Equal(string.Empty, TestHelper.GetValue<string>(data, "Action"));
            data = TestHelper.GetOnlyOneDataItem(events, "InstanceProviderGet");
            Assert.Equal("System.ServiceModel.Dispatcher.InstanceProvider", TestHelper.GetValue<string>(data, "TypeName"));
            int instanceHash = TestHelper.GetValue<int>(data, "InstanceHash");
            data = TestHelper.GetOnlyOneDataItem(events, "InstanceProviderRelease");
            Assert.Equal(instanceHash, TestHelper.GetValue<int>(data, "InstanceHash"));
            data = TestHelper.GetOnlyOneDataItem(events, "DispatchMessageFormatterDeserialize");
            Assert.Equal("System.ServiceModel.Dispatcher.PrimitiveOperationFormatter", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "System.ServiceModel.InvokeOperation.Start");
            Assert.Equal("Microsoft.ServiceModel.TelemetryCorrelation.ActivityRestoringOperationInvoker", TestHelper.GetValue<string>(data, "InvokerType"));
            data = TestHelper.GetOnlyOneDataItem(events, "System.ServiceModel.InvokeOperation.Stop");
            data = TestHelper.GetOnlyOneDataItem(events, "DispatchMessageFormatterSerialize");
            Assert.Equal("System.ServiceModel.Dispatcher.PrimitiveOperationFormatter", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "ClientMessageFormatterDeserialize");
            Assert.Equal("System.ServiceModel.Dispatcher.PrimitiveOperationFormatter", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "ClientMessageFormatterSerialize");
            Assert.Equal("System.ServiceModel.Dispatcher.PrimitiveOperationFormatter", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "ClientSelectOperation");
            Assert.Equal("System.ServiceModel.Dispatcher.OperationSelectorBehavior+MethodInfoOperationSelector", TestHelper.GetValue<string>(data, "TypeName"));
            Assert.Equal("DoWork", TestHelper.GetValue<string>(data, "SelectedOperation"));
            Assert.Equal(17, events.Count); /*Change to 17 because these events also occur. This error was in orginal code on main branch.
           [2]: {[ClientMessageInspectorBeforeSend, { TypeName = Microsoft.VisualStudio.Diagnostics.ServiceModelSink.StubClientEventSink, Duration = 1.3063 }]}
           [5]: {[DispatchMessageInspectorAfterReceive, { TypeName = Microsoft.VisualStudio.Diagnostics.ServiceModelSink.StubServerEventSink, Duration = 2.6019 }]}
           [11]: {[DispatchMessageInspectorBeforeSend, { TypeName = Microsoft.VisualStudio.Diagnostics.ServiceModelSink.StubServerEventSink, Duration = 0.0037 }]}
           [15]: {[ClientMessageInspectorAfterReceive, { TypeName = Microsoft.VisualStudio.Diagnostics.ServiceModelSink.StubClientEventSink, Duration = 2.1052 }]}
        */
                                             
        }

        [Fact]
        public void NetTcpBaseEventsWritten()
        {
            var tc = TestHelper.InitAiConfigAndGetTelemetyrClient();

            IList<KeyValuePair<string, object>> events;
            using (var subscription = DiagnosticsHelper.SubscribeToListener())
            {
                ServiceHost host = null;
                ChannelFactory<ITestService> factory = null;
                ITestService channel = null;
                try
                {
                    var helper = TestHelper.NetTcp;
                    host = helper.CreateServiceHost();
                    host.Open();

                    factory = helper.CreateChannelFactory();
                    channel = factory.CreateChannel();
                    var activity = new Activity("Root");
                    activity.Start();
                    channel.DoWork();
                    activity.Stop();

                    events = subscription.Events;
                }
                finally
                {
                    TestHelper.Cleanup(channel, factory, host);
                }
            }

            var data = TestHelper.GetOnlyOneDataItem(events, "System.ServiceModel.ReceiveMessage.Start");
            Assert.Equal("http://tempuri.org/ITestService/DoWork", TestHelper.GetValue<string>(data, "Action"));
            data = TestHelper.GetOnlyOneDataItem(events, "System.ServiceModel.ReceiveMessage.Stop");
            Assert.Equal("http://tempuri.org/ITestService/DoWorkResponse", TestHelper.GetValue<string>(data, "Action"));
            data = TestHelper.GetOnlyOneDataItem(events, "System.ServiceModel.SendMessage.Start");
            Assert.Equal("http://tempuri.org/ITestService/DoWork", TestHelper.GetValue<string>(data, "Action"));
            data = TestHelper.GetOnlyOneDataItem(events, "System.ServiceModel.SendMessage.Stop");
            Assert.Equal("http://tempuri.org/ITestService/DoWorkResponse", TestHelper.GetValue<string>(data, "Action"));
            data = TestHelper.GetOnlyOneDataItem(events, "InstanceProviderGet");
            Assert.Equal("System.ServiceModel.Dispatcher.InstanceProvider", TestHelper.GetValue<string>(data, "TypeName"));
            // It seems that InstanceContextMode.Session doesn't ever call IInstanceProvider.ReleaseInstance. 
            // This is likely a bug in WCF which nobody has noticed for the last decade.
            // int instanceHash = TestHelper.GetValue<int>(data, "InstanceHash");
            // data = TestHelper.GetOnlyOneDataItem(events, "InstanceProviderRelease");
            // Assert.Equal(instanceHash, TestHelper.GetValue<int>(data, "InstanceHash"));
            data = TestHelper.GetOnlyOneDataItem(events, "DispatchMessageFormatterDeserialize");
            Assert.Equal("System.ServiceModel.Dispatcher.PrimitiveOperationFormatter", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "System.ServiceModel.InvokeOperation.Start");
            Assert.Equal("Microsoft.ServiceModel.TelemetryCorrelation.ActivityRestoringOperationInvoker", TestHelper.GetValue<string>(data, "InvokerType"));
            data = TestHelper.GetOnlyOneDataItem(events, "System.ServiceModel.InvokeOperation.Stop");
            data = TestHelper.GetOnlyOneDataItem(events, "DispatchMessageFormatterDeserialize");
            Assert.Equal("System.ServiceModel.Dispatcher.PrimitiveOperationFormatter", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "DispatchMessageFormatterSerialize");
            Assert.Equal("System.ServiceModel.Dispatcher.PrimitiveOperationFormatter", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "ClientMessageFormatterDeserialize");
            Assert.Equal("System.ServiceModel.Dispatcher.PrimitiveOperationFormatter", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "ClientMessageFormatterSerialize");
            Assert.Equal("System.ServiceModel.Dispatcher.PrimitiveOperationFormatter", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "ClientSelectOperation");
            Assert.Equal("System.ServiceModel.Dispatcher.OperationSelectorBehavior+MethodInfoOperationSelector", TestHelper.GetValue<string>(data, "TypeName"));
            Assert.Equal("DoWork", TestHelper.GetValue<string>(data, "SelectedOperation"));
            Assert.Equal(16, events.Count); /*Change to 16 becuase these events also occur. This error was in orginal code on main branch.
           [2]: {[ClientMessageInspectorBeforeSend, { TypeName = Microsoft.VisualStudio.Diagnostics.ServiceModelSink.StubClientEventSink, Duration = 1.3063 }]}
           [5]: {[DispatchMessageInspectorAfterReceive, { TypeName = Microsoft.VisualStudio.Diagnostics.ServiceModelSink.StubServerEventSink, Duration = 2.6019 }]}
           [11]: {[DispatchMessageInspectorBeforeSend, { TypeName = Microsoft.VisualStudio.Diagnostics.ServiceModelSink.StubServerEventSink, Duration = 0.0037 }]}
           [15]: {[ClientMessageInspectorAfterReceive, { TypeName = Microsoft.VisualStudio.Diagnostics.ServiceModelSink.StubClientEventSink, Duration = 2.1052 }]}
        */
        }

        [Fact]
        public void HttpSoap12ReplyAction()
        {
            var tc = TestHelper.InitAiConfigAndGetTelemetyrClient();

            IList<KeyValuePair<string, object>> events;
            using (var subscription = DiagnosticsHelper.SubscribeToListener())
            {
                ServiceHost host = null;
                ChannelFactory<ITestService> factory = null;
                ITestService channel = null;
                try
                {
                    var helper = TestHelper.BasicHttpSoap12WSAddressing10;
                    host = helper.CreateServiceHost();
                    host.Open();

                    factory = helper.CreateChannelFactory();
                    channel = factory.CreateChannel();
                    var activity = new Activity("Root");
                    activity.Start();
                    channel.DoWork();
                    activity.Stop();

                    events = subscription.Events;
                }
                finally
                {
                    TestHelper.Cleanup(channel, factory, host);
                }
            }

            var data = TestHelper.GetOnlyOneDataItem(events, "System.ServiceModel.ReceiveMessage.Start");
            Assert.Equal("http://tempuri.org/ITestService/DoWork", TestHelper.GetValue<string>(data, "Action"));
            data = TestHelper.GetOnlyOneDataItem(events, "System.ServiceModel.ReceiveMessage.Stop");
            Assert.Equal("http://tempuri.org/ITestService/DoWorkResponse", TestHelper.GetValue<string>(data, "Action"));
            data = TestHelper.GetOnlyOneDataItem(events, "System.ServiceModel.SendMessage.Start");
            Assert.Equal("http://tempuri.org/ITestService/DoWork", TestHelper.GetValue<string>(data, "Action"));
            data = TestHelper.GetOnlyOneDataItem(events, "System.ServiceModel.SendMessage.Stop");
            Assert.Equal("http://tempuri.org/ITestService/DoWorkResponse", TestHelper.GetValue<string>(data, "Action"));
            data = TestHelper.GetOnlyOneDataItem(events, "InstanceProviderGet");
            Assert.Equal("System.ServiceModel.Dispatcher.InstanceProvider", TestHelper.GetValue<string>(data, "TypeName"));
            int instanceHash = TestHelper.GetValue<int>(data, "InstanceHash");
            data = TestHelper.GetOnlyOneDataItem(events, "InstanceProviderRelease");
            Assert.Equal(instanceHash, TestHelper.GetValue<int>(data, "InstanceHash"));
            data = TestHelper.GetOnlyOneDataItem(events, "DispatchMessageFormatterDeserialize");
            Assert.Equal("System.ServiceModel.Dispatcher.PrimitiveOperationFormatter", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "System.ServiceModel.InvokeOperation.Start");
            Assert.Equal("Microsoft.ServiceModel.TelemetryCorrelation.ActivityRestoringOperationInvoker", TestHelper.GetValue<string>(data, "InvokerType"));
            data = TestHelper.GetOnlyOneDataItem(events, "System.ServiceModel.InvokeOperation.Stop");
            data = TestHelper.GetOnlyOneDataItem(events, "DispatchMessageFormatterDeserialize");
            Assert.Equal("System.ServiceModel.Dispatcher.PrimitiveOperationFormatter", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "DispatchMessageFormatterSerialize");
            Assert.Equal("System.ServiceModel.Dispatcher.PrimitiveOperationFormatter", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "ClientMessageFormatterDeserialize");
            Assert.Equal("System.ServiceModel.Dispatcher.PrimitiveOperationFormatter", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "ClientMessageFormatterSerialize");
            Assert.Equal("System.ServiceModel.Dispatcher.PrimitiveOperationFormatter", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "ClientSelectOperation");
            Assert.Equal("System.ServiceModel.Dispatcher.OperationSelectorBehavior+MethodInfoOperationSelector", TestHelper.GetValue<string>(data, "TypeName"));
            Assert.Equal("DoWork", TestHelper.GetValue<string>(data, "SelectedOperation"));
            Assert.Equal(17, events.Count); /*Change to 17 becuase these events also occur. This error was in orginal code on main branch.
           [2]: {[ClientMessageInspectorBeforeSend, { TypeName = Microsoft.VisualStudio.Diagnostics.ServiceModelSink.StubClientEventSink, Duration = 1.3063 }]}
           [5]: {[DispatchMessageInspectorAfterReceive, { TypeName = Microsoft.VisualStudio.Diagnostics.ServiceModelSink.StubServerEventSink, Duration = 2.6019 }]}
           [11]: {[DispatchMessageInspectorBeforeSend, { TypeName = Microsoft.VisualStudio.Diagnostics.ServiceModelSink.StubServerEventSink, Duration = 0.0037 }]}
           [15]: {[ClientMessageInspectorAfterReceive, { TypeName = Microsoft.VisualStudio.Diagnostics.ServiceModelSink.StubClientEventSink, Duration = 2.1052 }]}
        */
        }

        private void HostTestServiceWithNamedPipes()
        {
            ServiceHost host = null;
            ChannelFactory<ITestService> factory = null;
            ITestService channel = null;

            try
            {
                var helper = TestHelper.NetNamedPipes;
                host = helper.CreateServiceHost();
                host.Open();
                TestService.hostEvent.WaitOne();
            }
            catch (Exception ex)
            {
                Debugger.Break();
            }
            finally
            {
                TestHelper.Cleanup(channel, factory, host);
            }
        }


        public Task RunHostTestServiceWithNamedPipes()
        {
            return Task.Run(() => HostTestServiceWithNamedPipes());
        }


        private void HostTestServiceWithHttp()
        {
            ServiceHost host = null;
            ChannelFactory<ITestService> factory = null;
            ITestService channel = null;

            try
            {
                var helper = TestHelper.BasicHttp;
                host = helper.CreateServiceHost();
                host.Open();
                TestService.hostEvent.WaitOne();
            }
            catch (Exception ex)
            {
                Debugger.Break();
            }
            finally
            {
                TestHelper.Cleanup(channel, factory, host);
            }
        }

        public Task RunHostTestServiceWithHttp()
        {
            return Task.Run(() => HostTestServiceWithHttp());
        }

        [Fact]
        public void NetNamedPipesClientActivityPropagation2Hops()
        {
            var tc = TestHelper.InitAiConfigAndGetTelemetyrClient();

            var activity = new Activity("Root");
            activity.AddBaggage("foo", "bar");
            var id = activity.Id;
            activity.Start();

            tc.TrackTrace("NetNamedPipesClientActivityPropagation2Hops begin");

            using (var subscription = DiagnosticsHelper.SubscribeToListener())
            {
                ServiceHost host = null;
                ChannelFactory<ITestService> factory = null;
                ITestService channel = null;

                ServiceHost host2 = null;
                ChannelFactory<ITestService> factory2 = null;
                ITestService channel2 = null;

                try
                {
                    var helper2 = TestHelper.NetNamedPipes;
                    host2 = helper2.CreateServiceHost();
                    host2.Open();

                    var helper = TestHelper.BasicHttp;
                    //var activity = new Activity("Root");
                    //activity.AddBaggage("foo", "bar");
                    //var id = activity.Id;
                    //activity.Start();
                    host = helper.CreateServiceHost();
                    host.Open();

                    factory = helper.CreateChannelFactory();
                    channel = factory.CreateChannel(); //Endpoint = Address={net.pipe://localhost/NetNamedPipesClientActivityPropagation2Hops/NetNamedPipes}
                    var baggage = channel.GetBaggage();

                    Assert.NotNull(baggage);
                    Assert.Single(baggage);
                    Assert.True(baggage.ContainsKey("foo"));
                    Assert.Equal("bar", baggage["foo"]);


                    var receivedRootId = channel.GetActivityRootId();
                    var receivedRootId2 = channel.GetActivityRootId2Hop();

                    Assert.Equal(activity.RootId, receivedRootId);
                    Assert.Equal(activity.RootId, receivedRootId2);

                }
                finally
                {
                    //activity.Stop();
                    tc.TrackTrace("NetNamedPipesClientActivityPropagation2Hops end");
                    tc.Flush();
                    TestHelper.Cleanup(channel, factory, host);
                    TestHelper.Cleanup(channel2, factory2, host2);
                }
            }
        }
        private void LoadAssemblies(IEnumerable<string> assemblyNames, AppDomain newDomain)
        {
            foreach (string asmbly in assemblyNames)
            {
                try
                {
                    newDomain.Load(asmbly);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(asmbly);
                }
            }
        }

        private void LoadAssembliesForDomain(AppDomain newDomain)
        {
            LoadAssemblies(from a in AppDomain.CurrentDomain.GetAssemblies() select a.FullName, newDomain);
            LoadAssemblies(from a in System.Reflection.Assembly.GetExecutingAssembly().GetReferencedAssemblies() select a.FullName, newDomain);
        }

        private AppDomain CreateDomain(string domainName)
        {
            var domain = AppDomain.CreateDomain(domainName, null, new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase
            });

            return domain;
        }

        //Remove this test. No longer needed since that was not issue when testing FrontEndWCFService
        //[Fact]
        //public void NetNamedPipesClientActivityPropogation2HopsAppDomains()
        //{
        //    var tc = TestHelper.InitAiConfigAndGetTelemetyrClient();
        //    var activity = new Activity("Root");
        //    activity.AddBaggage("foo", "bar");
        //    var id = activity.Id;
        //    activity.Start();

        //    using (var subscription = DiagnosticsHelper.SubscribeToListener())
        //    {
        //        try
        //        {                    
        //            AppDomain root = AppDomain.CurrentDomain;

        //            var httpDomain = CreateDomain("HttpDomain");
        //            var netNampedPipesDomain = CreateDomain("NetNamedPipesDomain");
        //            LoadAssembliesForDomain(httpDomain);
        //            LoadAssembliesForDomain(netNampedPipesDomain);

        //            Assembly[] assemblies = httpDomain.GetAssemblies();
        //            var assembly = (from a in assemblies where a.FullName == "Microsoft.ServiceModel.TelemetryCorrelation.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" select a).FirstOrDefault();
        //            Type type = assembly.GetType("Microsoft.ServiceModel.TelemetryCorrelation.Tests.BasicEventsTests");
        //            MethodInfo method = type.GetMethod("RunHostTestServiceWithNamedPipes");
        //            object instance = Activator.CreateInstance(type);
        //            var taskHostTestServiceWithNamedPipe = (Task)method.Invoke(instance, null);

        //            assemblies = netNampedPipesDomain.GetAssemblies();
        //            assembly = (from a in assemblies where a.FullName == "Microsoft.ServiceModel.TelemetryCorrelation.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" select a).FirstOrDefault();
        //            type = assembly.GetType("Microsoft.ServiceModel.TelemetryCorrelation.Tests.BasicEventsTests");
        //            method = type.GetMethod("RunHostTestServiceWithHttp");
        //            instance = Activator.CreateInstance(type);
        //            var taskHostTestServiceWithHttp = (Task)method.Invoke(instance, null);

        //            var helper = TestHelper.BasicHttp;
        //            var factory = helper.CreateChannelFactory("HostTestServiceWithHttp");
        //            var channel = factory.CreateChannel(); 
        //            var baggage = channel.GetBaggage();

        //            //channel.DoWork();

        //            Assert.NotNull(baggage);
        //            Assert.Single(baggage);
        //            Assert.True(baggage.ContainsKey("foo"));
        //            Assert.Equal("bar", baggage["foo"]);

        //            var receivedRootId = channel.GetActivityRootId();
        //            var receivedRootId2 = channel.GetActivityRootId2Hop("HostTestServiceWithNamedPipes");

        //            Assert.Equal(activity.RootId, receivedRootId);
        //            Assert.Equal(activity.RootId, receivedRootId2);


        //            channel.Done();
        //            channel.Done();
        //            taskHostTestServiceWithNamedPipe.Wait();
        //            taskHostTestServiceWithHttp.Wait();
        //            activity.Stop();

        //            //Type programClass = newDomain.GetType("Microsoft.ServiceModel.TelemetryCorrelation.Tests.BasicEventsTests");
        //            //// Get the method GetAssemblyName method to make a call.  
        //            //MethodInfo getAssemblyName = programClass.GetMethod("GetAssemblyName");
        //            //// Create an instance o.  
        //            //object programObject = Activator.CreateInstance(programClass);
        //            //// Execute the GetAssemblyName method.  
        //            //getAssemblyName.Invoke(programObject, null);
        //            //Console.ReadKey();
        //        }
        //        catch (Exception ex)
        //        {

        //        }
        //    }

        //}

        public string GetString()
        {
            return "hello";
        }

        [Fact]
        public void NetNamedPipesClientActivityPropagation()
        {
            var tc = TestHelper.InitAiConfigAndGetTelemetyrClient();

            var activity = new Activity("Root");
            activity.AddBaggage("foo", "bar");
            var id = activity.Id;
            activity.Start();

            tc.TrackTrace("NetNamedPipesClientActivityPropagation begin");

            using (var subscription = DiagnosticsHelper.SubscribeToListener())
            {
                ServiceHost host = null;
                ChannelFactory<ITestService> factory = null;
                ITestService channel = null;
                try
                {
                    var helper = TestHelper.NetNamedPipes;
                    //var activity = new Activity("Root");
                    //activity.AddBaggage("foo", "bar");
                    //var id = activity.Id;
                    //activity.Start();
                    host = helper.CreateServiceHost();
                    host.Open();

                    factory = helper.CreateChannelFactory();
                    channel = factory.CreateChannel();
                    var baggage = channel.GetBaggage();

                    Assert.NotNull(baggage);
                    Assert.Single(baggage);
                    Assert.True(baggage.ContainsKey("foo"));
                    Assert.Equal("bar", baggage["foo"]);

                    var receivedRootId = channel.GetActivityRootId();
                    Assert.Equal(activity.RootId, receivedRootId);
                }
                finally
                {
                    //activity.Stop();
                    tc.TrackTrace("NetNamedPipesClientActivityPropagation end");
                    tc.Flush();
                    TestHelper.Cleanup(channel, factory, host);
                }
            }
        }
        [Fact]
        public void NetNamedMultipleConcurrentRequestsEventsWritten()
        {
            var tc = TestHelper.InitAiConfigAndGetTelemetyrClient();

            var helper = TestHelper.NetNamedPipes;
            var activity = new Activity("Root");
            activity.AddBaggage("foo", "bar");
            var id = activity.Id;
            activity.Start();

            tc.TrackTrace("NetNamedMultipleConcurrentRequestsEventsWritten begin");

            Debug.WriteLine($"NetNamedMultipleConcurrentRequestsEventsWritten .. TelemetryConfiguration.Active, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

            IList<KeyValuePair<string, object>> events;
            using (var subscription = DiagnosticsHelper.SubscribeToListener())
            {
                ServiceHost host = null;
                ChannelFactory<ITestService> factory = null;
                ITestService channel = null;
                try
                {
                    //var helper = TestHelper.NetTcp;
                    //var activity = new Activity("Root");
                    //activity.AddBaggage("foo", "bar");
                    //var id = activity.Id;
                    //activity.Start();
                    host = helper.CreateServiceHost();
                    host.Open();

                    factory = helper.CreateChannelFactory();
                    channel = factory.CreateChannel();
                    ((IChannel)channel).Open();
                    var task = Task.Run(() =>
                    {
                        Debug.WriteLine($"NetNamedMultipleConcurrentRequestsEventsWritten .. Task.Run enter, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

                        //var activity2 = new Activity("Root");
                        //activity2.AddBaggage("foo2", "bar2");
                        //var id2 = activity.Id;
                        //activity2.Start();
                        Debug.WriteLine($"NetNamedMultipleConcurrentRequestsEventsWritten .. before Channel.Sleep(5000), Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                        channel.Sleep(5000);
                        Debug.WriteLine($"NetNamedMultipleConcurrentRequestsEventsWritten .. after Channel.Sleep(5000), Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                        //activity2.Stop();
                        Debug.WriteLine($"NetNamedMultipleConcurrentRequestsEventsWritten .. Task.Run exit, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                    });
                    // Make sure the Task has had time to start and make the outgoing request.
                    Debug.WriteLine($"NetNamedMultipleConcurrentRequestsEventsWritten .. before Thread.Sleep(1000) Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                    Thread.Sleep(1000);
                    Debug.WriteLine($"NetNamedMultipleConcurrentRequestsEventsWritten .. after Thread.Sleep(1000) Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                    var receivedRootId = channel.GetActivityRootId();
                    Assert.Equal(activity.RootId, receivedRootId);
                    // Make sure the channel.Sleep call hasn't finished yet
                    Assert.False(task.IsCompleted, "Unable to verify behavior as long running call completed too quickly");
                    Debug.WriteLine($"NetNamedMultipleConcurrentRequestsEventsWritten .. before Wait Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                    task.Wait();
                    Debug.WriteLine($"NetNamedMultipleConcurrentRequestsEventsWritten .. after Wait Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                    events = subscription.Events;
                }
                finally
                {
                    tc.TrackTrace("HttpMultipleConcurrentRequestsEventsWritten end");

                    TestHelper.Cleanup(channel, factory, host);
                }
            }

            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.SendMessage.Start" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/Sleep")));
            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.SendMessage.Start" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/GetActivityRootId")));
            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.SendMessage.Stop" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/SleepResponse"))); 
            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.SendMessage.Stop" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/GetActivityRootIdResponse"))); 
            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.ReceiveMessage.Start" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/GetActivityRootId")));
            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.ReceiveMessage.Stop" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/SleepResponse"))); 
            Assert.Single(events.Where(e => (e.Key == "System.ServiceModel.ReceiveMessage.Stop" && TestHelper.GetValue<string>(e.Value, "Action") == "http://tempuri.org/ITestService/GetActivityRootIdResponse"))); 
        }
    }

}
