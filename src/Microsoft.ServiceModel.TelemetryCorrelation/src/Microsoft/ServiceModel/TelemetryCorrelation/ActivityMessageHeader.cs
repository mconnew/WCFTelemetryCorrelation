using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    [DataContract(Name = ActivityHeaderName, Namespace = ActivityHeaderNamespace)]
    internal class ActivityMessageHeader
    {
        public const string ActivityHeaderName = "Activity";
        public const string ActivityHeaderNamespace = "http://scemas.microsoft.com/telemetrycorrelation/2018";

        public ActivityMessageHeader(Activity activity)
        {
            Id = activity.Id;
            var arraySize = activity.Baggage.Count();
            Baggage = new CorrelationData[arraySize];
            int idx = 0;
            foreach (var item in activity.Baggage)
            {
                Baggage[idx++] = new CorrelationData { Key = item.Key, Value = item.Value };
            }
        }

        [DataMember(Name = "Request-Id")]
        public string Id { get; set; }

        [DataMember(Name = "Correlation-Context")]
        public CorrelationData[] Baggage { get; set; }

        public static XmlObjectSerializer Serializer { get; } = new DataContractSerializer(typeof(ActivityMessageHeader));
    }

    [DataContract(Name = CorrelationDataName, Namespace = ActivityMessageHeader.ActivityHeaderNamespace)]
    internal class CorrelationData
    {
        public const string CorrelationDataName = "Item";

        [DataMember(Name = "Key")]
        public string Key { get; set; }

        [DataMember(Name = "Value")]
        public string Value { get; set; }
    }
}