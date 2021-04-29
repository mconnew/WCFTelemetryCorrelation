// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    internal class TelemetryCorrelationRequestChannel : ChannelBase, IRequestChannel
    {
        private readonly IRequestChannel _innerRequestChannel;
        private readonly bool _isServer;

        public TelemetryCorrelationRequestChannel(IRequestChannel channel, ChannelFactoryBase channelFactory) : this(channel, (ChannelManagerBase)channelFactory)
        {
            _isServer = false;
        }

        public TelemetryCorrelationRequestChannel(IRequestChannel channel, ChannelListenerBase channelListener) : this(channel, (ChannelManagerBase)channelListener)
        {
            _isServer = true;
        }

        private TelemetryCorrelationRequestChannel(IRequestChannel channel, ChannelManagerBase channelManager) : base(channelManager)
        {
            _innerRequestChannel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        #region IRequestChannel
        EndpointAddress IRequestChannel.RemoteAddress => _innerRequestChannel.RemoteAddress;

        public Uri Via => _innerRequestChannel.Via;

        public IAsyncResult BeginRequest(Message message, AsyncCallback callback, object state)
        {
            if (_isServer)
            {
                message = ActivityHelper.StartReceiveMessage(message);
            }
            else
            {
                message = ActivityHelper.StartSendMessage(message);
            }

            return _innerRequestChannel.BeginRequest(message, callback, state);
        }

        public IAsyncResult BeginRequest(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            if (_isServer)
            {
                message = ActivityHelper.StartReceiveMessage(message);
            }
            else
            {
                message = ActivityHelper.StartSendMessage(message);
            }

            return _innerRequestChannel.BeginRequest(message, timeout, callback, state);
        }

        public Message EndRequest(IAsyncResult result)
        {
            var message = _innerRequestChannel.EndRequest(result);
            if (_isServer)
            {
                Debugger.Break();
            }

            return message;
        }

        public Message Request(Message message)
        {
            if (_isServer)
            {
                ActivityHelper.StartReceiveMessage(message);
            }
            else
            {
                message = ActivityHelper.StartSendMessage(message);
            }

            var response = _innerRequestChannel.Request(message);

            if (_isServer)
            {
                Debugger.Break();
            }
            else
            {
                ActivityHelper.StopSendMessage(message, response);
            }

            return response;
        }

        public Message Request(Message message, TimeSpan timeout)
        {
            Debug.WriteLine($"TelemetryCorrelationRequestChannel.Request enter, Activity - Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer}, RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

            if (_isServer)
            {
                ActivityHelper.StartReceiveMessage(message); 
                Debug.WriteLine($"TelemetryCorrelationRequestChannel.Request isServer(true), after ActivityHelper.StartReceiveMessage, Activity - Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer}, RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

            }
            else
            {
                message = ActivityHelper.StartSendMessage(message);
                Debug.WriteLine($"TelemetryCorrelationRequestChannel.Request isServer(fase), after ActivityHelper.StartSendMessage, Activity - Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer}, RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

            }

            var response = _innerRequestChannel.Request(message, timeout);
            Debug.WriteLine($"TelemetryCorrelationRequestChannel.Request after innerRequestChannel.Request, Activity -Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, isServer:{_isServer},  RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");


            if (_isServer)
            {
                Debugger.Break();
            }
            else
            {
                ActivityHelper.StopSendMessage(message, response);
                Debug.WriteLine($"TelemetryCorrelationRequestChannel.Request isServer(fase), after ActivityHelper.StopSendMessage, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

            }

            return response;
        }
        #endregion // IRequestChannel

        #region ChannelBase
        protected override void OnAbort()
        {
            _innerRequestChannel.Abort();
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerRequestChannel.BeginClose(timeout, callback, state);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerRequestChannel.BeginOpen(timeout, callback, state);
        }

        protected override void OnClose(TimeSpan timeout)
        {
            _innerRequestChannel.Close();
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            _innerRequestChannel.EndClose(result);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            _innerRequestChannel.EndOpen(result);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            _innerRequestChannel.Open(timeout);
        }
        #endregion // ChannelBase
    }
}