// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    internal class TelemetryCorrelationChannelListener<TChannel> : ChannelListenerBase<TChannel> where TChannel : class, IChannel
    {
        private readonly IChannelListener<TChannel> _innerChannelListener;

        public TelemetryCorrelationChannelListener(IChannelListener<TChannel> innerChannelListener)
        {
            _innerChannelListener = innerChannelListener;
        }

        protected override void OnAbort()
        {
            _innerChannelListener.Abort();
        }

        protected override void OnClose(TimeSpan timeout)
        {
            _innerChannelListener.Close(timeout);
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            _innerChannelListener.EndClose(result);
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerChannelListener.BeginClose(timeout, callback, state);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            _innerChannelListener.Open(timeout);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerChannelListener.BeginOpen(timeout, callback, state);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            _innerChannelListener.EndOpen(result);
        }

        public override T GetProperty<T>()
        {
            return _innerChannelListener.GetProperty<T>();
        }

        protected override bool OnWaitForChannel(TimeSpan timeout)
        {
            return _innerChannelListener.WaitForChannel(timeout);
        }

        protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerChannelListener.BeginWaitForChannel(timeout, callback, state);
        }

        protected override bool OnEndWaitForChannel(IAsyncResult result)
        {
            return _innerChannelListener.EndWaitForChannel(result);
        }

        public override Uri Uri => _innerChannelListener.Uri;

        protected override TChannel OnAcceptChannel(TimeSpan timeout)
        {
            TChannel channel = _innerChannelListener.AcceptChannel(timeout);
            if (channel == null)
            {
                return null;
            }

            if (typeof(TChannel) == typeof(IRequestChannel))
                return (TChannel)(object)new TelemetryCorrelationRequestChannel((IRequestChannel)channel, this);

            if (typeof(TChannel) == typeof(IReplyChannel))
                return (TChannel)(object)new TelemetryCorrelationReplyChannel((IReplyChannel)channel, this);

            if (typeof(TChannel) == typeof(IDuplexSessionChannel))
                return (TChannel)(object)new TelemetryCorrelationDuplexSessionChannel((IDuplexSessionChannel)channel, this);

            return channel;
        }

        protected override IAsyncResult OnBeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerChannelListener.BeginAcceptChannel(timeout, callback, state);
        }

        protected override TChannel OnEndAcceptChannel(IAsyncResult result)
        {
            TChannel channel = _innerChannelListener.EndAcceptChannel(result);
            if (channel == null)
            {
                return null;
            }

            if (typeof(TChannel) == typeof(IRequestChannel))
                return (TChannel)(object)new TelemetryCorrelationRequestChannel((IRequestChannel)channel, this);

            if (typeof(TChannel) == typeof(IReplyChannel))
                return (TChannel)(object)new TelemetryCorrelationReplyChannel((IReplyChannel)channel, this);

            if (typeof(TChannel) == typeof(IDuplexSessionChannel))
                return (TChannel)(object)new TelemetryCorrelationDuplexSessionChannel((IDuplexSessionChannel)channel, this);

            return channel;
        }
    }
}