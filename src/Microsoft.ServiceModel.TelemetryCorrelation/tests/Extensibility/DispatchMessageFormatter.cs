using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility
{
    class DispatchMessageFormatter : IDispatchMessageFormatter
    {
        IDispatchMessageFormatter _innerFormatter;

        public DispatchMessageFormatter(IDispatchMessageFormatter innerFormatter)
        {
            _innerFormatter = innerFormatter ?? throw new ArgumentNullException(nameof(innerFormatter));
        }

        public void DeserializeRequest(Message message, object[] parameters)
        {
            _innerFormatter.DeserializeRequest(message, parameters);
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            return _innerFormatter.SerializeReply(messageVersion, parameters, result);
        }
    }
}
