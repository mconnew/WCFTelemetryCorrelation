// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    internal class TelemetryCorrelationChannelFactory<TChannel> : ChannelFactoryBase<TChannel>
    {
        private IChannelFactory<TChannel> _innerChannelFactory;

        public TelemetryCorrelationChannelFactory(IChannelFactory<TChannel> innerChannelFactory)
        {
            _innerChannelFactory = innerChannelFactory;
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerChannelFactory.BeginClose(timeout, callback, state);
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            _innerChannelFactory.EndClose(result);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            _innerChannelFactory.Open(timeout);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerChannelFactory.BeginOpen(timeout, callback, state);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            _innerChannelFactory.EndOpen(result);
        }

        protected override TChannel OnCreateChannel(EndpointAddress address, Uri via)
        {
            TChannel channel = _innerChannelFactory.CreateChannel(address, via);
            if(typeof(TChannel) == typeof(IRequestChannel))
                return (TChannel)(object)new TelemetryCorrelationRequestChannel((IRequestChannel)channel, this);

            if (typeof(TChannel) == typeof(IReplyChannel))
                return (TChannel)(object)new TelemetryCorrelationReplyChannel((IReplyChannel)channel, this);

            if (typeof(TChannel) == typeof(IDuplexSessionChannel))
                return (TChannel)(object)new TelemetryCorrelationDuplexSessionChannel((IDuplexSessionChannel)channel, this);

            return channel;
        }

        public override T GetProperty<T>()
        {
            return _innerChannelFactory.GetProperty<T>();
        }

        protected override void OnAbort()
        {
            _innerChannelFactory.Abort();
        }

        protected override void OnClose(TimeSpan timeout)
        {
            _innerChannelFactory.Close(timeout);
        }
    }
}