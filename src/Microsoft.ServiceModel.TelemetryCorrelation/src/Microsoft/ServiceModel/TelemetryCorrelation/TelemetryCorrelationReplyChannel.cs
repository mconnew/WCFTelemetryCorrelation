// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    internal class TelemetryCorrelationReplyChannel : ChannelBase, IReplyChannel
    {
        private readonly IReplyChannel _innerReplyChannel;

        public TelemetryCorrelationReplyChannel(IReplyChannel channel, ChannelManagerBase channelManager) : base(channelManager)
        {
            _innerReplyChannel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        #region IReplyChannel
        public EndpointAddress LocalAddress => _innerReplyChannel.LocalAddress;

        public RequestContext ReceiveRequest()
        {
            return new TelemetryCorrelationRequestContext(_innerReplyChannel.ReceiveRequest());
        }

        public RequestContext ReceiveRequest(TimeSpan timeout)
        {
            return new TelemetryCorrelationRequestContext(_innerReplyChannel.ReceiveRequest(timeout));
        }

        public IAsyncResult BeginReceiveRequest(AsyncCallback callback, object state)
        {
            return _innerReplyChannel.BeginReceiveRequest(callback, state);
        }

        public IAsyncResult BeginReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerReplyChannel.BeginReceiveRequest(timeout, callback, state);
        }

        public RequestContext EndReceiveRequest(IAsyncResult result)
        {
            return new TelemetryCorrelationRequestContext(_innerReplyChannel.EndReceiveRequest(result));
        }

        public bool TryReceiveRequest(TimeSpan timeout, out RequestContext context)
        {
            var success = _innerReplyChannel.TryReceiveRequest(timeout, out context);
            if(success && context != null)
            {
                context = new TelemetryCorrelationRequestContext(context);
            }

            return success;
        }

        public IAsyncResult BeginTryReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerReplyChannel.BeginTryReceiveRequest(timeout, callback, state);
        }

        public bool EndTryReceiveRequest(IAsyncResult result, out RequestContext context)
        {
            var success = _innerReplyChannel.EndTryReceiveRequest(result, out context);
            if (success && context != null)
            {
                context = new TelemetryCorrelationRequestContext(context);
            }

            return success;
        }

        public bool WaitForRequest(TimeSpan timeout)
        {
            return _innerReplyChannel.WaitForRequest(timeout);
        }

        public IAsyncResult BeginWaitForRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerReplyChannel.BeginWaitForRequest(timeout, callback, state);
        }

        public bool EndWaitForRequest(IAsyncResult result)
        {
            return _innerReplyChannel.EndWaitForRequest(result);
        }
        #endregion // IReplyChannel

        #region ChannelBase
        protected override void OnAbort()
        {
            _innerReplyChannel.Abort();
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerReplyChannel.BeginClose(timeout, callback, state);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return _innerReplyChannel.BeginOpen(timeout, callback, state);
        }

        protected override void OnClose(TimeSpan timeout)
        {
            _innerReplyChannel.Close();
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            _innerReplyChannel.EndClose(result);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            _innerReplyChannel.EndOpen(result);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            _innerReplyChannel.Open(timeout);
        }
        #endregion // ChannelBase
    }
}