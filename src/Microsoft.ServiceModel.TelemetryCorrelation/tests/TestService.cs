// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.ApplicationInsights;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

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

        public string GetActivityRootId2Hop([CallerMemberName] string instancePath = "")
        {
            TelemetryClient tc = new TelemetryClient();
            tc.TrackTrace("GetActivityRootId2Hop start");

            ServiceHost host = null;
            ChannelFactory<ITestService> factory = null;
            ITestService channel = null;

            try { 
                var helper = TestHelper.NetNamedPipes;
                factory = helper.CreateChannelFactory(instancePath);
                channel = factory.CreateChannel();
                tc.TrackTrace("before GetActivityRootId2Async");
                var id = channel.GetActivityRootId2Async().Result;
                tc.TrackTrace("after  GetActivityRootId2Async");

                return id;
            }
            finally
            {
                tc.TrackTrace("GetActivityRootId2Hop end");
                tc.Flush();
                TestHelper.Cleanup(channel, factory, host);
            }
        }

        public Task<string> GetActivityRootId2Async()
        {
            TelemetryClient tc = new TelemetryClient();
            tc.TrackTrace("GetActivityRootId start");

            var activity = Activity.Current;
            if (activity == null)
            {
                return null;
            }

            tc.TrackTrace("GetActivityRootId end");

            return Task.FromResult(activity.RootId);
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
