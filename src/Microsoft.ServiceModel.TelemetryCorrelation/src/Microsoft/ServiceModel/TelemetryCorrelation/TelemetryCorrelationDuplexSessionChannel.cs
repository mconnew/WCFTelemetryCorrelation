using System;
using System.Collections;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    internal class TelemetryCorrelationDuplexSessionChannel : ChannelBase, IDuplexSessionChannel
    {
        private readonly IDuplexSessionChannel _innerDuplexSessionChannel;
        private readonly bool _isServer;
        private readonly Hashtable _pendingActivities;

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
            return _innerDuplexSessionChannel.BeginReceive(callback, state);
        }

        IAsyncResult IInputChannel.BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerDuplexSessionChannel.BeginReceive(timeout, callback, state);
        }

        IAsyncResult IInputChannel.BeginTryReceive(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerDuplexSessionChannel.BeginTryReceive(timeout, callback, state);
        }

        IAsyncResult IInputChannel.BeginWaitForMessage(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerDuplexSessionChannel.BeginWaitForMessage(timeout, callback, state);
        }

        Message IInputChannel.EndReceive(IAsyncResult result)
        {
            var message = _innerDuplexSessionChannel.EndReceive(result);
            if (_isServer)
            {
                message = ActivityHelper.StartReceiveMessage(message);
            }
            else
            {
                ActivityHelper.StopSendMessage(message, _pendingActivities);
            }

            return message;
        }

        bool IInputChannel.EndTryReceive(IAsyncResult result, out Message message)
        {
            bool success = _innerDuplexSessionChannel.EndTryReceive(result, out message);
            if (success && message != null)
            {
                if (_isServer)
                {
                    message = ActivityHelper.StartReceiveMessage(message);
                }
                else
                {
                    ActivityHelper.StopSendMessage(message, _pendingActivities);
                }
            }

            return success;
        }

        bool IInputChannel.EndWaitForMessage(IAsyncResult result)
        {
            return _innerDuplexSessionChannel.EndWaitForMessage(result);
        }

        Message IInputChannel.Receive()
        {
            var message = _innerDuplexSessionChannel.Receive();
            if (_isServer)
            {
                message = ActivityHelper.StartReceiveMessage(message);
            }
            else
            {
                ActivityHelper.StopSendMessage(message, _pendingActivities);
            }

            return message;
        }

        Message IInputChannel.Receive(TimeSpan timeout)
        {
            var message = _innerDuplexSessionChannel.Receive(timeout);
            if (_isServer)
            {
                message = ActivityHelper.StartReceiveMessage(message);
            }
            else
            {
                ActivityHelper.StopSendMessage(message, _pendingActivities);
            }

            return message;
        }

        bool IInputChannel.TryReceive(TimeSpan timeout, out Message message)
        {
            var success = _innerDuplexSessionChannel.TryReceive(timeout, out message);
            if (success && message != null)
            {
                if (_isServer)
                {
                    message = ActivityHelper.StartReceiveMessage(message);
                }
                else
                {
                    ActivityHelper.StopSendMessage(message, _pendingActivities);
                }
            }

            return success;
        }

        bool IInputChannel.WaitForMessage(TimeSpan timeout)
        {
            return _innerDuplexSessionChannel.WaitForMessage(timeout);
        }
        #endregion // IInputChannel

        #region IOutputChannel
        EndpointAddress IOutputChannel.RemoteAddress => _innerDuplexSessionChannel.RemoteAddress;

        Uri IOutputChannel.Via => _innerDuplexSessionChannel.Via;

        IAsyncResult IOutputChannel.BeginSend(Message message, AsyncCallback callback, object state)
        {
            if (!_isServer)
            {
                message = ActivityHelper.StartSendMessage(message, _pendingActivities);
            }

            return _innerDuplexSessionChannel.BeginSend(message, callback, state);
        }

        IAsyncResult IOutputChannel.BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            if (!_isServer)
            {
                message = ActivityHelper.StartSendMessage(message, _pendingActivities);
            }

            return _innerDuplexSessionChannel.BeginSend(message, timeout, callback, state);
        }

        void IOutputChannel.EndSend(IAsyncResult result)
        {
            _innerDuplexSessionChannel.EndSend(result);
            if (_isServer)
            {
                // TODO: Capture the response activity info in the IAsyncResult and unwrap
                ActivityHelper.StopReceiveMessage(OperationContext.Current?.RequestContext?.RequestMessage, null);
            }
        }

        void IOutputChannel.Send(Message message)
        {
            if (!_isServer)
            {
                message = ActivityHelper.StartSendMessage(message, _pendingActivities);
            }

            _innerDuplexSessionChannel.Send(message);
            if (_isServer)
            {
                ActivityHelper.StopReceiveMessage(OperationContext.Current?.RequestContext?.RequestMessage, message);
            }
        }

        void IOutputChannel.Send(Message message, TimeSpan timeout)
        {
            if (!_isServer)
            {
                message = ActivityHelper.StartSendMessage(message, _pendingActivities);
            }

            _innerDuplexSessionChannel.Send(message, timeout);
            if(_isServer)
            {
                ActivityHelper.StopReceiveMessage(OperationContext.Current?.RequestContext?.RequestMessage, message);
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