using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility
{
    internal class TestOperationBehavior : IOperationBehavior
    {
        private TestServiceBehavior _testServiceBehavior;

        public TestOperationBehavior(TestServiceBehavior testServiceBehavior)
        {
            _testServiceBehavior = testServiceBehavior;
        }

        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
        }

        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            if ((_testServiceBehavior.ExtensibilityType & ExtensibilityType.DispatchMessageFormatter) == ExtensibilityType.DispatchMessageFormatter)
            {
                dispatchOperation.Formatter = new DispatchMessageFormatter(dispatchOperation.Formatter);
            }

            if ((_testServiceBehavior.ExtensibilityType & ExtensibilityType.DispatchParameterInspector) == ExtensibilityType.DispatchParameterInspector)
            {
                dispatchOperation.ParameterInspectors.Add(new ParameterInspector());
            }
        }

        public void Validate(OperationDescription operationDescription)
        {
        }
    }
}