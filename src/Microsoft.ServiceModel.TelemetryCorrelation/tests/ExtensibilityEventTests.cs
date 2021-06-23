// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.ApplicationInsights.DependencyCollector;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Xunit;
using System.Collections;
using System;

namespace Microsoft.ServiceModel.TelemetryCorrelation.Tests
{
    public class ExtensibilityEventTests
    {
        [Fact]
        public void BasicHttpDispatchMessageInspector()
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

            var data = TestHelper.GetOnlyTwoDataItem(events, "DispatchMessageInspectorAfterReceive");
            //Assert.Equal("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.DispatchMessageInspector", TestHelper.GetValue<string>(data, "TypeName"));
            var result = Array.Find(data, e => e.Contains("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.DispatchMessageInspector"));
            Assert.NotNull(result);
            data = TestHelper.GetOnlyTwoDataItem(events, "DispatchMessageInspectorBeforeSend");
            //Assert.Equal("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.DispatchMessageInspector", TestHelper.GetValue<string>(data, "TypeName"));
            result = Array.Find(data, e => e.Contains("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.DispatchMessageInspector"));
            Assert.NotNull(result);
        }

        [Fact]
        public void BasicHttpDispatchFormatter()
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
                    host =helper.CreateServiceHost();
                    TestHelper.AddInstanceProvider(host);
                    host.Open();

                    factory =helper.CreateChannelFactory();
                    TestHelper.AddClientMessageInspector(factory); 
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

            //There are two events, 1 without TestHelper.AddClientMessageExpector with TypeName = Microsoft.VisualStudio.Diagnostics.ServiceModelSink.StubClientEventSink
            //and as a reulst of TestHelper.AddClientMessageExpector  TypeName: Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.ClientMessageInspector

            var data = TestHelper.GetOnlyTwoDataItem(events, "ClientMessageInspectorAfterReceive");
            //Assert.Equal("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.ClientMessageInspector", TestHelper.GetValue<string>(data, "TypeName"));
            var result = Array.Find(data, e => e.Contains("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.ClientMessageInspector"));
            Assert.NotNull(result);
            data = TestHelper.GetOnlyTwoDataItem(events, "ClientMessageInspectorBeforeSend");
            //Assert.Equal("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.ClientMessageInspector", TestHelper.GetValue<string>(data, "TypeName"));
            result = Array.Find(data, e => e.Contains("Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility.ClientMessageInspector"));
            Assert.NotNull(result);
        }
    }
}
