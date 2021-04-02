// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    public class TelemetryCorrelationBehavior : IServiceBehavior, IEndpointBehavior
    {
        public TelemetryCorrelationBehavior()
        {
            DiagnosticSourceBridgeEventListener.Init();
        }

        #region IServiceBehavior
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {

        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            IOperationBehavior behavior = new ActivityRestoringOperationBehavior();
            foreach (ServiceEndpoint endpoint in serviceDescription.Endpoints)
            {
                // TODO: Make configurable as someone might want data on e.g. MEX endpoints
                if (!endpoint.IsSystemEndpoint)
                {
                    foreach (OperationDescription operation in endpoint.Contract.Operations)
                    {
                        // Multiple endpoints might use the same contract. E.g. HTTP and Net.Tcp endpoints for the same contract.
                        // In that case, the operation will have the behavior added the first time it's encountered.
                        if (operation.OperationBehaviors.Contains(typeof(ActivityRestoringOperationBehavior)))
                        {
                            continue;
                        }

                        operation.OperationBehaviors.Add(behavior);
                    }
                }
            }
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (var endpoint in serviceDescription.Endpoints)
            {
                // TODO: Make configurable as someone might want data on e.g. MEX endpoints
                if (!endpoint.IsSystemEndpoint)
                {
                    endpoint.Binding = InsertTelemetryCorrelationBindingElement(endpoint.Binding);
                }
            }
        }
        #endregion // IServiceBehavior

        #region IEndpointBehavior
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            endpoint.Binding = InsertTelemetryCorrelationBindingElement(endpoint.Binding);
        }
        #endregion // IEndpointBehavior

        private Binding InsertTelemetryCorrelationBindingElement(Binding endpointBinding)
        {
            if (endpointBinding.CreateBindingElements().Find<TelemetryCorrelationBindingElement>() != null)
            {
                return endpointBinding;
            }

            var customBinding = new CustomBinding(endpointBinding);
            var bindingElements = customBinding.Elements;
            int position;
            for (position = 0; position < bindingElements.Count; position++)
            {
                BindingElement be = bindingElements[position];
                if (be is MessageEncodingBindingElement || be is TransportBindingElement)
                {
                    break;
                }
            }

            customBinding.Elements.Insert(position, new TelemetryCorrelationBindingElement());

            return customBinding;
        }
    }
}
