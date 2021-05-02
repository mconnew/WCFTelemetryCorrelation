// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    internal class ActivityRestoringOperationInvoker : IOperationInvoker
    {
        private readonly IOperationInvoker _innerInvoker;

        public ActivityRestoringOperationInvoker(IOperationInvoker invoker)
        {
            _innerInvoker = invoker;
        }

        private void RestoreIncomingActivity() //TODO: Not restoring to what was in ActivityHelper.StartOperation
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

            Debug.WriteLine($"ActivityRestoringOperationInvoker.Invoke enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId},Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

            RestoreIncomingActivity();

            Debug.WriteLine($"ActivityRestoringOperationInvoker.Invoke .. RestoreIncomingActivity, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId},Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
            try
            {
                return _innerInvoker.Invoke(instance, inputs, out outputs);
    
            }
            finally
            {
                Debug.WriteLine($"ActivityRestoringOperationInvoker.Invoke .. Invoke, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId},Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
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