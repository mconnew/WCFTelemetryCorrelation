using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Xunit;

namespace Microsoft.ServiceModel.TelemetryCorrelation.Tests
{
    public class ExtensibilityEventTests
    {
        [Fact]
        public void BasicHttpDispatchMessageInspector()
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
                    host =helper.CreateServiceHost();
                    TestHelper.AddDispatchMessageInspector(host);
                    host.Open();

                    factory =helper.CreateChannelFactory();
                    channel =factory.CreateChannel();
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

            var data = TestHelper.GetOnlyOneDataItem(events, "DispatchMessageInspectorAfterReceive");
            Assert.Equal("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.DispatchMessageInspector", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "DispatchMessageInspectorBeforeSend");
            Assert.Equal("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.DispatchMessageInspector", TestHelper.GetValue<string>(data, "TypeName"));
        }

        [Fact]
        public void BasicHttpDispatchFormatter()
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
                    host =helper.CreateServiceHost();
                    TestHelper.AddDispatchFormatter(host);
                    host.Open();

                    factory =helper.CreateChannelFactory();
                    channel =factory.CreateChannel();
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

            var data = TestHelper.GetOnlyOneDataItem(events, "DispatchMessageFormatterDeserialize");
            Assert.Equal("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.DispatchMessageFormatter", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "DispatchMessageFormatterSerialize");
            Assert.Equal("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.DispatchMessageFormatter", TestHelper.GetValue<string>(data, "TypeName"));
        }

        [Fact]
        public void BasicHttpDispatchOperationSelector()
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
                    host =helper.CreateServiceHost();
                    TestHelper.AddDispatchOperationSelector(host);
                    host.Open();

                    factory =helper.CreateChannelFactory();
                    channel =factory.CreateChannel();
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

            var data = TestHelper.GetOnlyOneDataItem(events, "DispatchSelectOperation");
            Assert.Equal("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.DispatchOperationSelector", TestHelper.GetValue<string>(data, "TypeName"));
        }

        [Fact]
        public void BasicHttpDispatchParameterInspector()
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
                    host =helper.CreateServiceHost();
                    TestHelper.AddDispatchParameterInspector(host);
                    host.Open();

                    factory =helper.CreateChannelFactory();
                    channel =factory.CreateChannel();
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

            var data = TestHelper.GetOnlyOneDataItem(events, "ParameterInspectorBefore");
            Assert.Equal("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.ParameterInspector", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "ParameterInspectorAfter");
            Assert.Equal("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.ParameterInspector", TestHelper.GetValue<string>(data, "TypeName"));
        }

        [Fact]
        public void BasicHttpInstanceProvider()
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
                    host =helper.CreateServiceHost();
                    TestHelper.AddInstanceProvider(host);
                    host.Open();

                    factory =helper.CreateChannelFactory();
                    channel =factory.CreateChannel();
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

            var data = TestHelper.GetOnlyOneDataItem(events, "InstanceProviderGet");
            Assert.Equal("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.InstanceProvider", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "InstanceProviderRelease");
            Assert.Equal("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.InstanceProvider", TestHelper.GetValue<string>(data, "TypeName"));
        }

        [Fact]
        public void BasicHttpClientMessageInspector()
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
                    host =helper.CreateServiceHost();
                    TestHelper.AddInstanceProvider(host);
                    host.Open();

                    factory =helper.CreateChannelFactory();
                    TestHelper.AddClientMessageInspector(factory);
                    channel =factory.CreateChannel();
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

            var data = TestHelper.GetOnlyOneDataItem(events, "ClientMessageInspectorAfterReceive");
            Assert.Equal("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.ClientMessageInspector", TestHelper.GetValue<string>(data, "TypeName"));
            data = TestHelper.GetOnlyOneDataItem(events, "ClientMessageInspectorBeforeSend");
            Assert.Equal("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.ClientMessageInspector", TestHelper.GetValue<string>(data, "TypeName"));
        }
    }
}
