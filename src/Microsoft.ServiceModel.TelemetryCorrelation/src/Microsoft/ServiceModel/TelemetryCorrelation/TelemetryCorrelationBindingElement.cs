using System;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    public class TelemetryCorrelationBindingElement : BindingElement
    {
        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!CanBuildChannelFactory<TChannel>(context))
            {
                throw new ArgumentException(string.Format(SR.ChannelTypeNotSupported, nameof(TChannel)), nameof(TChannel));
            }

            var innerChannelFactory = context.BuildInnerChannelFactory<TChannel>();
            if (typeof(TChannel) == typeof(IDuplexSessionChannel) ||
                typeof(TChannel) == typeof(IRequestChannel))
            {
                return new TelemetryCorrelationChannelFactory<TChannel>(innerChannelFactory);
            }

            return innerChannelFactory;
        }

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            return context.CanBuildInnerChannelFactory<TChannel>();
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!CanBuildChannelListener<TChannel>(context))
            {
                throw new ArgumentException(string.Format(SR.ChannelTypeNotSupported, nameof(TChannel)), nameof(TChannel));
            }

            var innerChannelListener = context.BuildInnerChannelListener<TChannel>();
            if (typeof(TChannel) == typeof(IDuplexSessionChannel) ||
                typeof(TChannel) == typeof(IRequestChannel) ||
                typeof(TChannel) == typeof(IReplyChannel))
            {
                return new TelemetryCorrelationChannelListener<TChannel>(innerChannelListener);
            }

            return innerChannelListener;
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            return context.CanBuildInnerChannelListener<TChannel>();
        }

        public override BindingElement Clone()
        {
            return this;
        }

        public override T GetProperty<T>(BindingContext context)
        {
            return context.GetInnerProperty<T>();
        }
    }
}