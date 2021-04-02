// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.ServiceModel;

namespace Microsoft.ServiceModel.TelemetryCorrelation.Tests
{
    [ServiceContract]
    internal interface ITestService
    {
        [OperationContract]
        void DoWork();

        [OperationContract]
        Dictionary<string, string> GetBaggage();

        [OperationContract]
        string GetActivityRootId();

        [OperationContract]
        void Sleep(int duration);
    }
}
