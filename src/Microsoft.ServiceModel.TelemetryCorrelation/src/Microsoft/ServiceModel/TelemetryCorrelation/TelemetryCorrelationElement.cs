using System;
using System.Configuration;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;


namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    public class TelemetryCorrelationElement : BindingElementExtensionElement
    {
        public TelemetryCorrelationElement()
        {
        }

        public override Type BindingElementType => typeof(TelemetryCorrelationBindingElement);

        protected override BindingElement CreateBindingElement()
        {
            var telemetryCorrelationBindingElement = new TelemetryCorrelationBindingElement();
            ApplyConfiguration(telemetryCorrelationBindingElement);
            return telemetryCorrelationBindingElement;
        }
    }
}
