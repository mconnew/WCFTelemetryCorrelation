// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.ServiceModel.TelemetryCorrelation.Tests
{
    public class BasicEventsTests
    {
        [Fact]
        public void BasicHttpClientActivityPropagation()
        {
            using (var subscription = DiagnosticsHelper.SubscribeToListener())
            {
                ServiceHost host = null;
                ChannelFactory<ITestService> factory = null;
                ITestService channel = null;
                try
                {
                    var helper = TestHelper.BasicHttp;
                    var activity = new Activity("Root");
                    activity.AddBaggage("foo", "bar");
                    var id = activity.Id;
                    activity.Start();
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
                    TestHelper.Cleanup(channel, factory, host);
                }
            }
        }

        [Fact]
        public void NetTcpClientActivityPropagation()
        {
            using (var subscription = DiagnosticsHelper.SubscribeToListener())
            {
                ServiceHost host = null;
                ChannelFactory<ITestService> factory = null;
                ITestService channel = null;
                try
                {
                    var helper = TestHelper.NetTcp;
                    var activity = new Activity("Root");
                    activity.AddBaggage("foo", "bar");
                    var id = activity.Id;
                    activity.Start();
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
                    TestHelper.Cleanup(channel, factory, host);
                }
            }
        }

        [Fact]
        public void NetTcpMultipleConcurrentRequestsEventsWritten()
        {
            IList<KeyValuePair<string, object>> events;
            using (var subscription = DiagnosticsHelper.SubscribeToListener())
            {
                ServiceHost host = null;
                ChannelFactory<ITestService> factory = null;
                ITestService channel = null;
                try
                {
                    var helper = TestHelper.NetTcp;
                    var activity = new Activity("Root");
                    activity.AddBaggage("foo", "bar");
                    var id = activity.Id;
                    activity.Start();
                    host = helper.CreateServiceHost();
                    host.Open();

                    factory = helper.CreateChannelFactory();
                    channel = factory.CreateChannel();
                    ((IChannel)channel).Open();
                    var task = Task.Run(() =>
                    {
                        var activity2 = new Activity("Root");
                        activity2.AddBaggage("foo2", "bar2");
                        var id2 = activity.Id;
                        activity2.Start();
                        channel.Sleep(5000);
                        activity2.Stop();
                    });
                    // Make sure the Task has had time to start and make the outgoing request.
                    Thread.Sleep(1000);
                    var receivedRootId = channel.GetActivityRootId();
                    Assert.Equal(activity.RootId, receivedRootId);
                    // Make sure the channel.Sleep call hasn't finished yet
                    Assert.False(task.IsCompleted, "Unable to verify behavior as long running call completed too quickly");
                    task.Wait();
                    events = subscription.Events;
                }
                finally
                {
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
        public void BasicHttpBaseEventsWritten()
        {
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
            Assert.Equal(13, events.Count);
        }

        [Fact]
        public void NetTcpBaseEventsWritten()
        {
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
            Assert.Equal(12, events.Count);
        }

        [Fact]
        public void HttpSoap12ReplyAction()
        {
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
            Assert.Equal(13, events.Count);
        }
    }
}
