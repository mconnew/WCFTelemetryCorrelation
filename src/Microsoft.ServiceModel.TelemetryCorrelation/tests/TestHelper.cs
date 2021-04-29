// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using Xunit;

namespace Microsoft.ServiceModel.TelemetryCorrelation.Tests
{
    internal class TestHelper
    {
        public static TelemetryClient InitAiConfigAndGetTelemetyrClient()
        {
            var config = TelemetryConfiguration.Active; //This reults in call to WcfTrackingTelemetryModule.Initialize
            if ((config.InstrumentationKey == null) ||(config.InstrumentationKey == "[AiKey]"))
                throw new Exception("ApplicationInsights.config not configured correctly");
            TelemetryClient tc = new TelemetryClient(config);
            tc.InstrumentationKey = config.InstrumentationKey;
            return tc;
        }


        public static TestHelper BasicHttp { get; } = new TestHelper(TestVariant.BasicHttp);
        public static TestHelper BasicHttpSoap12WSAddressing10 { get; } = new TestHelper(TestVariant.BasicHttpSoap12);
        public static TestHelper NetTcp { get; } = new TestHelper(TestVariant.NetTcp);

        public static TestHelper NetNamedPipes { get; } = new TestHelper(TestVariant.NetNamedPipes);

        private TestVariant _variant;

        public TestHelper(TestVariant variant)
        {
            _variant = variant;
        }

        public Binding Binding
        {
            get
            {
                Binding binding = null;
                switch(_variant)
                {
                    case TestVariant.BasicHttp:
                        binding = new BasicHttpBinding();
                        break;
                    case TestVariant.BasicHttpSoap12:
                        binding = new CustomBinding(
                            new TextMessageEncodingBindingElement(MessageVersion.Soap12WSAddressing10, Encoding.UTF8),
                            new HttpTransportBindingElement());
                        break;
                    case TestVariant.NetTcp:
                        binding = new NetTcpBinding();
                        break;
                    case TestVariant.NetNamedPipes:
                        binding = new NetNamedPipeBinding();
                        break;
                }

                binding.SendTimeout = TimeSpan.MaxValue;
                binding.ReceiveTimeout = TimeSpan.MaxValue;
                return binding;
            }
        }

        internal static void Cleanup(params object[] communicationObjects)
        {
            foreach(var o in communicationObjects)
            {
                var co = o as ICommunicationObject;
                try
                {
                    co?.Close();
                }
                catch
                {
                    co?.Abort();
                }
            }
        }

        private string RelativeAddress {
            get
            {
                return _variant.ToString();
            }
        } 

        private string GetBaseAddress(Binding binding)
        {
            UriBuilder uriBuilder;
            int port = 10000 + (int)_variant;
            string scheme = binding.Scheme;
            if (scheme != "net.pipe")
            {
                uriBuilder = new UriBuilder(scheme, "localhost", port);
            }
            else
            {
                uriBuilder = new UriBuilder(scheme, "localhost");
            }
            return uriBuilder.Uri.ToString();
        }

        private string GetRemoteAddress(Binding binding)
        {
            return GetBaseAddress(binding) + RelativeAddress;
        }

        public ServiceHost CreateServiceHost([CallerMemberName] string instancePath = "")
        {
            var binding = Binding;
            var baseAddress = GetBaseAddress(binding);
            var host = new ServiceHost(typeof(TestService), new Uri(baseAddress + instancePath + "/"));
            host.AddServiceEndpoint(typeof(ITestService), binding, RelativeAddress);
            host.Description.Behaviors.Add(new TelemetryCorrelationBehavior());
            return host;
        }

        internal ChannelFactory<ITestService> CreateChannelFactory([CallerMemberName] string instancePath = "")
        {
            var binding = Binding;
            var baseAddress = GetBaseAddress(binding);
            var factory = new ChannelFactory<ITestService>(binding, baseAddress + instancePath + "/" + RelativeAddress);
            factory.Endpoint.EndpointBehaviors.Add(new TelemetryCorrelationBehavior());
            return factory;
        }

        public static void AddDispatchMessageInspector(ServiceHost host)
        {
            AddServiceExtensibility(host, ExtensibilityType.DispatchMessageInspector);
        }

        internal static void AddDispatchFormatter(ServiceHost host)
        {
            AddServiceExtensibility(host, ExtensibilityType.DispatchMessageFormatter);
        }

        internal static void AddDispatchOperationSelector(ServiceHost host)
        {
            AddServiceExtensibility(host, ExtensibilityType.DispatchOperationSelector);
        }

        internal static void AddDispatchParameterInspector(ServiceHost host)
        {
            AddServiceExtensibility(host, ExtensibilityType.DispatchParameterInspector);
        }

        internal static void AddInstanceProvider(ServiceHost host)
        {
            AddServiceExtensibility(host, ExtensibilityType.InstanceProvider);
        }

        private static void AddServiceExtensibility(ServiceHost host, ExtensibilityType extensibilityType)
        {
            var testBehavior = host.Description.Behaviors.Find<TestServiceBehavior>();
            if (testBehavior == null)
            {
                host.Description.Behaviors.Add(new TestServiceBehavior(extensibilityType));
            }
            else
            {
                testBehavior.AddExtensibilityType(extensibilityType);
            }
        }


        internal static object GetOnlyOneDataItem(IList<KeyValuePair<string, object>> events, string key)
        {
            var filteredEvents = events.Where(e => e.Key == key);
            Assert.True(1 == filteredEvents.Count(), $"There were {filteredEvents.Count()} events with name \"{key}\"");
            return filteredEvents.First().Value;
        }

        public static TValue GetValue<TValue>(object data, string propertyName)
        {
            var prop = data.GetType().GetProperty(propertyName, typeof(TValue));
            if (prop == null) return default(TValue);
            return (TValue)prop.GetValue(data);
        }

        internal enum TestVariant
        {
            BasicHttp,
            BasicHttpSoap12,
            NetTcp,
            NetNamedPipes
        }

        internal static void AddClientMessageInspector(ChannelFactory factory)
        {
            AddChannelFactoryExtensibility(factory, ExtensibilityType.ClientMessageInspector);
        }

        private static void AddChannelFactoryExtensibility(ChannelFactory factory, ExtensibilityType extensibilityType)
        { 
            var testBehavior = factory.Endpoint.Behaviors.Find<TestEndpointBehavior>();
            if (testBehavior == null)
            {
                factory.Endpoint.Behaviors.Add(new TestEndpointBehavior(extensibilityType));
            }
            else
            {
                testBehavior.AddExtensibilityType(extensibilityType);
            }
        }


    }
}