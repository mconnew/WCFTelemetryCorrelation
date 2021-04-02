// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility
{
    internal class TestServiceBehavior : IServiceBehavior
    {
        public TestServiceBehavior(ExtensibilityType extensibilityType)
        {
            ExtensibilityType = extensibilityType;
        }

        internal ExtensibilityType ExtensibilityType { get; private set; }

        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach(var channelDispatcherBase in serviceHostBase.ChannelDispatchers)
            {
                var channelDispatcher = channelDispatcherBase as ChannelDispatcher;
                foreach(var endpointDispatcher in channelDispatcher.Endpoints)
                {
                    if(endpointDispatcher.IsSystemEndpoint)
                    {
                        continue;
                    }

                    if ((ExtensibilityType | ExtensibilityType.DispatchMessageInspector) == ExtensibilityType.DispatchMessageInspector)
                    {
                        endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new DispatchMessageInspector());
                    }

                    if ((ExtensibilityType & ExtensibilityType.DispatchOperationSelector) == ExtensibilityType.DispatchOperationSelector)
                    {
                        endpointDispatcher.DispatchRuntime.OperationSelector = new DispatchOperationSelector(endpointDispatcher.DispatchRuntime);
                    }

                    if ((ExtensibilityType & ExtensibilityType.InstanceProvider) == ExtensibilityType.InstanceProvider)
                    {
                        endpointDispatcher.DispatchRuntime.InstanceProvider = new InstanceProvider(endpointDispatcher.DispatchRuntime);
                    }
                }
            }

            var operationBehavior = new TestOperationBehavior(this);
            foreach (var serviceEndpoint in serviceDescription.Endpoints)
            {
                foreach (var operationDescription in serviceEndpoint.Contract.Operations)
                {
                    operationDescription.Behaviors.Add(operationBehavior);
                }
            }

        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
        }

        internal void AddExtensibilityType(ExtensibilityType extensibilityType)
        {
            ExtensibilityType |= extensibilityType;
        }
    }
}
