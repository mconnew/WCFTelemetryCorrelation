// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Microsoft.ServiceModel.TelemetryCorrelation.Tests
{
    [ServiceContract]
    internal interface ITestService
    {
        [OperationContract]
        void DoWork();

        [OperationContract]
        void Done();

        [OperationContract]
        Dictionary<string, string> GetBaggage();

        [OperationContract]
        string GetActivityRootId();

        [OperationContract]
        string GetActivityRootId2Hop([CallerMemberName] string instancePath = "");

        [OperationContract]
        Task<string> GetActivityRootId2Async();

        [OperationContract]
        void Sleep(int duration);
    }
}
