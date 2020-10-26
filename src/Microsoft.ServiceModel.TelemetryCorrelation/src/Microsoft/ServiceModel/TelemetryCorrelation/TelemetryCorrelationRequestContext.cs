using System;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    internal class TelemetryCorrelationRequestContext : RequestContext
    {
        private RequestContext _innerContext;
        private Message _replyMessage;

        public TelemetryCorrelationRequestContext(RequestContext innerContext)
        {
            _innerContext = innerContext ?? throw new ArgumentNullException(nameof(innerContext));
            ActivityHelper.StartReceiveMessage(innerContext.RequestMessage);
        }

        public override Message RequestMessage => _innerContext.RequestMessage;

        public override void Abort()
        {
            ActivityHelper.StopReceiveMessage(RequestMessage, null);
            _innerContext.Abort();
        }

        public override IAsyncResult BeginReply(Message message, AsyncCallback callback, object state)
        {
            return _innerContext.BeginReply(message, callback, state);
        }

        public override IAsyncResult BeginReply(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            _replyMessage = message;
            return _innerContext.BeginReply(message, timeout, callback, state);
        }

        public override void Close()
        {
            _innerContext.Close();
        }

        public override void Close(TimeSpan timeout)
        {
            _innerContext.Close(timeout);
        }

        public override void EndReply(IAsyncResult result)
        {
            _innerContext.EndReply(result);
            ActivityHelper.StopReceiveMessage(RequestMessage, _replyMessage);
            _replyMessage = null;
        }

        public override void Reply(Message message)
        {
            _innerContext.Reply(message);
            ActivityHelper.StopReceiveMessage(RequestMessage, message);
        }

        public override void Reply(Message message, TimeSpan timeout)
        {
            _innerContext.Reply(message, timeout);
            ActivityHelper.StopReceiveMessage(RequestMessage, message);
        }
    }
}