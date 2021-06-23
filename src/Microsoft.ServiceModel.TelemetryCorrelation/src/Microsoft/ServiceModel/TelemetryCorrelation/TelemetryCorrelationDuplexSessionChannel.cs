// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    internal class TelemetryCorrelationDuplexSessionChannel : ChannelBase, IDuplexSessionChannel
    {
        private readonly IDuplexSessionChannel _innerDuplexSessionChannel;
        private readonly bool _isServer;
        private readonly Hashtable _pendingActivities;

        //TODO: Should we consider ConditionalWeakTable as is done in Application Insights code?
        //private static ConditionalWeakTable<string, IOperationHolder<DependencyTelemetry>> _pendingDependencyTelemetry { get; set; } = new ConditionalWeakTable<string, IOperationHolder<DependencyTelemetry>>();
        // Chanded from using _pendingDependencyTelemetry to using _pendingActivities with a Tuple
        //private static ConcurrentDictionary<System.Xml.UniqueId, IOperationHolder<DependencyTelemetry>> _pendingDependencyTelemetry = new ConcurrentDictionary<System.Xml.UniqueId, IOperationHolder<DependencyTelemetry>>();

        public TelemetryCorrelationDuplexSessionChannel(IDuplexSessionChannel channel, ChannelFactoryBase channelFactory) : this(channel, (ChannelManagerBase)channelFactory)
        {
            _isServer = false;
            _pendingActivities = new Hashtable();
        }

        public TelemetryCorrelationDuplexSessionChannel(IDuplexSessionChannel channel, ChannelListenerBase channelListener) : this(channel, (ChannelManagerBase)channelListener)
        {
            _isServer = true;
        }

        private TelemetryCorrelationDuplexSessionChannel(IDuplexSessionChannel channel, ChannelManagerBase channelManager) : base(channelManager)
        {
            _innerDuplexSessionChannel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        #region IDuplexSessionChannel
        #region IInputChannel
        EndpointAddress IInputChannel.LocalAddress => _innerDuplexSessionChannel.LocalAddress;

        IAsyncResult IInputChannel.BeginReceive(AsyncCallback callback, object state)
        {
            Debug.WriteLine($"IInputChannel.BeginReceive enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer}");
            return _innerDuplexSessionChannel.BeginReceive(callback, state);
        }

        IAsyncResult IInputChannel.BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
        {
            Debug.WriteLine($"IInputChannel.BeginReceive enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer}");
            return _innerDuplexSessionChannel.BeginReceive(timeout, callback, state);
        }

        IAsyncResult IInputChannel.BeginTryReceive(TimeSpan timeout, AsyncCallback callback, object state)
        {
            Debug.WriteLine($"IInputChannel.BeginTryReceive enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer}");
            Debug.WriteLine($"IInputChannel.BeginTryReceive enter con't Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

            var btr = _innerDuplexSessionChannel.BeginTryReceive(timeout, callback, state);

            Debug.WriteLine($"IInputChannel.BeginTryReceivee after _innerDuplexSessionChannel.BeginTryReceive, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer}");
            Debug.WriteLine($"IInputChannel.BeginTryReceive after con't Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

            return btr;
        }

        IAsyncResult IInputChannel.BeginWaitForMessage(TimeSpan timeout, AsyncCallback callback, object state)
        {
            Debug.WriteLine($"IInputChannel.BeginWaitForMessage enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer}");
            return _innerDuplexSessionChannel.BeginWaitForMessage(timeout, callback, state);
        }

        Message IInputChannel.EndReceive(IAsyncResult result)
        {
            Debug.WriteLine($"IInputChannel.EndReceive enter  Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}");
            var message = _innerDuplexSessionChannel.EndReceive(result);
            Debug.WriteLine($"IInputChannel.EndReceive after _innerDuplexSessionChannel.EndReceive,  Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, message: {message}");
            if (_isServer)
            {
                message = ActivityHelper.StartReceiveMessage(message);
                Debug.WriteLine($"IInputChannel.EndReceive after ActivityHelper.StartReceiveMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, message: {message}");
            }
            else
            {
                ActivityHelper.StopSendMessage(message, _pendingActivities);
            }

            return message;
        }

        bool IInputChannel.EndTryReceive(IAsyncResult result, out Message message)
        {
            Debug.WriteLine($"IInputChannel.EndTryReceive enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer}");

            bool success = _innerDuplexSessionChannel.EndTryReceive(result, out message);

            Debug.WriteLine($"IInputChannel.EndTryReceive after, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, message: {message}, isServer:{_isServer}");

            if (success && message != null)
            {
                if (_isServer)
                {
                    message = ActivityHelper.StartReceiveMessage(message);
                    Debug.WriteLine($"IInputChannel.EndTryReceive after ActivityHelper.StartReceiveMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, message: {message}, isServer:{_isServer}");
                }
                else
                {
                    ActivityHelper.StopSendMessage(message, _pendingActivities);
                    Debug.WriteLine($"IInputChannel.EndTryReceive after ActivityHelper.StopSendMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, message: {message}, isServer:{_isServer}");

                }
            }

            return success;
        }

        bool IInputChannel.EndWaitForMessage(IAsyncResult result)
        {
            Debug.WriteLine($"IInputChannel.EndWaitForMessage enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer}");

            return _innerDuplexSessionChannel.EndWaitForMessage(result);
        }

        Message IInputChannel.Receive()
        {
            Debug.WriteLine($"IInputChannel.Receive Enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, ");

            IOperationHolder<DependencyTelemetry> opHolder = null;
            
            var message = _innerDuplexSessionChannel.Receive();
            Debug.WriteLine($"IInputChannel.Receive _innerDuplexSessionChannel.Receive, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, message: {message}");
            if (_isServer)
            {
                message = ActivityHelper.StartReceiveMessage(message);
                Debug.WriteLine($"IInputChannel.Receive ActivityHelper.StartReceiveMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, message: {message}");
            }
            else
            {
                ActivityHelper.StopSendMessage(message, _pendingActivities);
                Debug.WriteLine($"IInputChannel.Receive ActivityHelper.StopSendMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, opHolder.Telemetry.Context.Operation.Id: {opHolder.Telemetry.Context.Operation.Id}, " +
                    $"opHolder.Telemetry.Context.Operation.ParentId {opHolder.Telemetry.Context.Operation.ParentId}");
            }

            Debug.WriteLine($"IInputChannel.Receive Exit, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, ");
            return message;
        }

        Message IInputChannel.Receive(TimeSpan timeout)
        {
            Debug.WriteLine($"IInputChannel.Receive, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, ");

            var message = _innerDuplexSessionChannel.Receive(timeout);
            Debug.WriteLine($"IInputChannel.Receive _innerDuplexSessionChannel.Receive, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, message: {message}");
            if (_isServer)
            {
                message = ActivityHelper.StartReceiveMessage(message);
                Debug.WriteLine($"IInputChannel.Receive ActivityHelper.StartReceiveMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, message: {message}");
            }
            else
            {
                ActivityHelper.StopSendMessage(message, _pendingActivities);
                Debug.WriteLine($"IInputChannel.Receive ActivityHelper.StopSendMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}");
            }

            return message;
        }

        bool IInputChannel.TryReceive(TimeSpan timeout, out Message message)
        {
            Debug.WriteLine($"IInputChannel.TryReceive Enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer{_isServer}");

            var success = _innerDuplexSessionChannel.TryReceive(timeout, out message);

            Debug.WriteLine($"IInputChannel.TryReceive after_innerDuplexSessionChannel.TryReceive, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, out message: {message},  isServer{_isServer}");
            if (success && message != null)
            {
                if (_isServer)
                {
                    message = ActivityHelper.StartReceiveMessage(message);
                    Debug.WriteLine($"IInputChannel.TryReceive after ActivityHelper.StartReceiveMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, message: {message},  isServer{_isServer}");
                }
                else
                {
                    ActivityHelper.StopSendMessage(message, _pendingActivities);
                    Debug.WriteLine($"IInputChannel.TryReceive after ActivityHelper.StopSendMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer{_isServer}");
                }
            }

            Debug.WriteLine($"IInputChannel.TryReceive Exit, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer{_isServer} ");

            return success;
        }

        bool IInputChannel.WaitForMessage(TimeSpan timeout)
        {
            Debug.WriteLine($"IInputChannel.WaitForMessage enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer}");

            return _innerDuplexSessionChannel.WaitForMessage(timeout);
        }
        #endregion // IInputChannel

        #region IOutputChannel
        EndpointAddress IOutputChannel.RemoteAddress => _innerDuplexSessionChannel.RemoteAddress;

        Uri IOutputChannel.Via => _innerDuplexSessionChannel.Via;

        IAsyncResult IOutputChannel.BeginSend(Message message, AsyncCallback callback, object state)
        {
            Debug.WriteLine($"IOutputChannel.BeginSend, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, message {message}, _isServer:{ _isServer}");
            Debug.WriteLine($"IOutputChannel.BeginSend (OperationContext.Current != null): {OperationContext.Current != null}");

            if (!_isServer)
            {
                message = ActivityHelper.StartSendMessage(message, _pendingActivities);
                Debug.WriteLine($"IOutputChannel.BeginSend ActivityHelper.StartSendMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, message: {message}");
            }

            var bs =  _innerDuplexSessionChannel.BeginSend(message, callback, state);
            Debug.WriteLine($"IOutputChannel.BeginSend _innerDuplexSessionChannel.BeginSend, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, _isServer:{ _isServer}");
            return bs;
        }

        IAsyncResult IOutputChannel.BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            Debug.WriteLine($"IOutputChannel.BeginSend enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, message: {message}, _isServer:{ _isServer}");
            Debug.WriteLine($"IOutputChannel.BeginSend (OperationContext.Current != null): {OperationContext.Current != null}");
            if (!_isServer)
            {
                message = ActivityHelper.StartSendMessage(message, _pendingActivities);
                Debug.WriteLine($"IOutputChannel.BeginSend after ActivityHelper.StartSendMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, message: {message}");
            }

            var bs =  _innerDuplexSessionChannel.BeginSend(message, timeout, callback, state);
            Debug.WriteLine($"IOutputChannel.BeginSend after innerDuplexSessionChannel.BeginSend, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId},  _isServer:{ _isServer}");
            return bs;

        }

        void IOutputChannel.EndSend(IAsyncResult result)
        {
            Debug.WriteLine($"IOutputChannel.EndSend enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer} ");
            _innerDuplexSessionChannel.EndSend(result);
            Debug.WriteLine($"IOutputChannel.EndSend after _innerDuplexSessionChannel.EndSend, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer} ");
            if (_isServer)
            {
                // TODO: Capture the response activity info in the IAsyncResult and unwrap
                ActivityHelper.StopReceiveMessage(OperationContext.Current?.RequestContext?.RequestMessage, null);
                Debug.WriteLine($"IOutputChannel.EndSend, ActivityHelper.StopReceiveMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, ");
            }
        }

        void IOutputChannel.Send(Message message)
        {
            Debug.WriteLine($"IOutputChannel.Send enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, message: {message}, isServer: {_isServer}");
            Debug.WriteLine($"IOutputChannel.BeginSend (OperationContext.Current != null): {OperationContext.Current != null}");

            if (!_isServer)
            {
                message = ActivityHelper.StartSendMessage(message, _pendingActivities);
                Debug.WriteLine($"IOutputChannel.Send after ActivityHelper.StartSendMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer}, message: {message}");
            }

            _innerDuplexSessionChannel.Send(message);
            Debug.WriteLine($"IOutputChannel.Send after _innerDuplexSessionChannel.Send, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer}");
            if (_isServer)
            {
                ActivityHelper.StopReceiveMessage(OperationContext.Current?.RequestContext?.RequestMessage, message);
                Debug.WriteLine($"IOutputChannel.Send after ActivityHelper.StopReceiveMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer}");
            }
        }

        void IOutputChannel.Send(Message message, TimeSpan timeout)
        {
            //IOperationHolder<DependencyTelemetry> opHolder = null;
            Debug.WriteLine($"IOutputChannel.Send timeout enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer}, message: {message}, isServer: {_isServer}");
            Debug.WriteLine($"IOutputChannel.BeginSend (OperationContext.Current != null): {OperationContext.Current != null}");

            if (!_isServer)
            {
                message = ActivityHelper.StartSendMessage(message, _pendingActivities);
                Debug.WriteLine($"IOutputChannel.Send timeout after ActivityHelper.StartSendMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer}, message: {message}");
            }

            _innerDuplexSessionChannel.Send(message, timeout);

            Debug.WriteLine($"IOutputChannel.Send timeout after _innerDuplexSessionChannel.Send, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer}, message: {message}");

            if (_isServer)
            {
                ActivityHelper.StopReceiveMessage(OperationContext.Current?.RequestContext?.RequestMessage, message);
                Debug.WriteLine($"IOutputChannel.Send timeout, after ActivityHelper.StopReceiveMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer}");
            }
        }
        #endregion // IOutputChannel

        IDuplexSession ISessionChannel<IDuplexSession>.Session => _innerDuplexSessionChannel.Session;

        #endregion // IDuplexSessionChannel

        #region ChannelBase
        protected override void OnAbort()
        {
            _innerDuplexSessionChannel.Abort();
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerDuplexSessionChannel.BeginClose(timeout, callback, state);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerDuplexSessionChannel.BeginOpen(timeout, callback, state);
        }

        protected override void OnClose(TimeSpan timeout)
        {
            _innerDuplexSessionChannel.Close();
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            _innerDuplexSessionChannel.EndClose(result);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            _innerDuplexSessionChannel.EndOpen(result);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            _innerDuplexSessionChannel.Open(timeout);
        }
        #endregion // ChannelBase
    }
}