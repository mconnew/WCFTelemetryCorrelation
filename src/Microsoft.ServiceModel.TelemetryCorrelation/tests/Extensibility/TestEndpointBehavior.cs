// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility;

namespace Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility
{
    internal class TestEndpointBehavior : IEndpointBehavior
    {
        public TestEndpointBehavior(ExtensibilityType extensibilityType)
        {
            ExtensibilityType = extensibilityType;
        }

        internal ExtensibilityType ExtensibilityType { get; private set; }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            if ((ExtensibilityType & ExtensibilityType.ClientMessageInspector) == ExtensibilityType.ClientMessageInspector)
            {
                clientRuntime.MessageInspectors.Add(new ClientMessageInspector());
            }
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }

        internal void AddExtensibilityType(ExtensibilityType extensibilityType)
        {
            ExtensibilityType |= extensibilityType;
        }
    }
}