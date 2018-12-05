using System;

namespace Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility
{
    [Flags]
    internal enum ExtensibilityType
    {
        None = 0,
        DispatchMessageInspector = 1,
        DispatchMessageFormatter = 2,
        DispatchOperationSelector = 4,
        DispatchParameterInspector = 8,
        InstanceProvider = 16,
        ClientMessageInspector = 32,
    }
}
