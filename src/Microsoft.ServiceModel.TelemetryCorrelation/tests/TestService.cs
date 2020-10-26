using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;

namespace Microsoft.ServiceModel.TelemetryCorrelation.Tests
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
    internal class TestService : ITestService
    {
        public void DoWork()
        {
            
        }

        public string GetActivityRootId()
        {
            var activity = Activity.Current;
            if (activity == null)
            {
                return null;
            }

            return activity.RootId;
        }

        public Dictionary<string, string> GetBaggage()
        {
            var activity = Activity.Current;
            if(activity == null)
            {
                return null;
            }

            var dictionary = new Dictionary<string, string>();
            foreach(var item in activity.Baggage)
            {
                ((ICollection<KeyValuePair<string, string>>)dictionary).Add(item);
            }

            return dictionary;
        }

        public void Sleep(int duration)
        {
            Thread.Sleep(duration);
        }
    }
}
