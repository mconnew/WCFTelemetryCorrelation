// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;

namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    internal class ActivityRestoringOperationInvoker : IOperationInvoker
    {
        private readonly IOperationInvoker _innerInvoker;

        public ActivityRestoringOperationInvoker(IOperationInvoker invoker)
        {
            _innerInvoker = invoker;
        }

        private void RestoreIncomingActivity()
        {
            OperationContext.Current.IncomingMessageProperties.RestoreCurrentActivity();
        }

        private void SaveOutgoingActivity(bool overwrite)
        {
            var opCtx = OperationContext.Current;
            if (Activity.Current != null)
            {
                if (overwrite || !opCtx.OutgoingMessageProperties.ContainsKey(ActivityHelper.CurrentActivityPropertyName))
                {
                    opCtx.OutgoingMessageProperties[ActivityHelper.CurrentActivityPropertyName] = Activity.Current;
                }
            }
        }

        #region IOperationInvoker
        public object[] AllocateInputs()
        {
            return _innerInvoker.AllocateInputs();
        }

        public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
            RestoreIncomingActivity();
            try
            {
                return _innerInvoker.Invoke(instance, inputs, out outputs);
            }
            finally
            {
                SaveOutgoingActivity(overwrite: false);
            }
        }

        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        {
            RestoreIncomingActivity();
            try
            {
                return _innerInvoker.InvokeBegin(instance, inputs, callback, state);
            }
            finally
            {
                SaveOutgoingActivity(overwrite: false);
            }
        }

        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
        {
            try
            {
                return _innerInvoker.InvokeEnd(instance, out outputs, result);
            }
            finally
            {
                SaveOutgoingActivity(overwrite: true);
            }
        }

        public bool IsSynchronous => _innerInvoker.IsSynchronous;
        #endregion
    }
}