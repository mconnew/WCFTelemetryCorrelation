// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility
{
    class DispatchOperationSelector : IDispatchOperationSelector
    {
        private DispatchRuntime _dispatchRuntime;

        public DispatchOperationSelector(DispatchRuntime dispatchRuntime)
        {
            _dispatchRuntime = dispatchRuntime ?? throw new ArgumentNullException(nameof(dispatchRuntime)); ;
        }

        public string SelectOperation(ref Message message)
        {
            string action = message.Headers.Action;
            if (action == null)
            {
                action = "*";
            }

            foreach(var operation in _dispatchRuntime.Operations)
            {
                if (operation.Action == action)
                {
                    return operation.Name;
                }
            }

            return null;
        }
    }
}
