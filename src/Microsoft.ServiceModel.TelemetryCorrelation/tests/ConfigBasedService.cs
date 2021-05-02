// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceModel.TelemetryCorrelation.Tests
{
    class ConfigBasedService : ITestService
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

        public string GetActivityRootId2Hop([CallerMemberName] string instancePath = "")
        {
            ServiceHost host = null;
            ChannelFactory<ITestService> factory = null;
            ITestService channel = null;

            try
            {
                var helper = TestHelper.NetNamedPipes;
                factory = helper.CreateChannelFactory(instancePath);
                channel = factory.CreateChannel();
                var id = channel.GetActivityRootId2Async().Result;

                return id;
            }
            finally
            {
                TestHelper.Cleanup(channel, factory, host);
            }
        }

        public Task<string> GetActivityRootId2Async()
        {
            var activity = Activity.Current;
            if (activity == null)
            {
                return null;
            }

            return Task.FromResult(activity.RootId);
        }


        public Dictionary<string, string> GetBaggage()
        {
            var activity = Activity.Current;
            if (activity == null)
            {
                return null;
            }

            var dictionary = new Dictionary<string, string>();
            foreach (var item in activity.Baggage)
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
