// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.ApplicationInsights;
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
            TelemetryClient tc = new TelemetryClient();
            tc.TrackTrace("GetActivityRootId start");

            var activity = Activity.Current;
            if (activity == null)
            {
                return null;
            }

            tc.TrackTrace("GetActivityRootId end");

            return activity.RootId;
        }

        public Dictionary<string, string> GetBaggage()
        {
            TelemetryClient tc = new TelemetryClient();
            tc.TrackTrace("GetBaggage start"); 

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

            tc.TrackTrace("GetBaggage end");

            return dictionary;
        }

        public void Sleep(int duration)
        {
            Thread.Sleep(duration);
        }
    }
}
