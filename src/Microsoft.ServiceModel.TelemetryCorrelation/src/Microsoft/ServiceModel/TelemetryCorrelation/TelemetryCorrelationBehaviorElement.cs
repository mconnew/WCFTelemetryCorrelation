using System;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    public class TelemetryCorrelationBehaviorElement : BehaviorExtensionElement
    {
        public override Type BehaviorType => typeof(TelemetryCorrelationBehavior);

        protected override object CreateBehavior()
        {
            return new TelemetryCorrelationBehavior();
        }
    }
}
