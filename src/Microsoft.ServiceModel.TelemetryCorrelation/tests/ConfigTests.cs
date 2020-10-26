using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Xunit;

namespace Microsoft.ServiceModel.TelemetryCorrelation.Tests
{ 
    public class ConfigTests
    {
        [Fact]
        public void BasicHttpClientActivityPropagation()
        {
            using (var subscription = DiagnosticsHelper.SubscribeToListener())
            {
                ServiceHost host = null;
                ChannelFactory<ITestService> factory = null;
                ITestService channel = null;
                try
                {
                    var activity = new Activity("Root");
                    activity.AddBaggage("foo", "bar");
                    var id = activity.Id;
                    activity.Start();
                    host = new ServiceHost(typeof(ConfigBasedService));
                    host.Open();

                    factory = new ChannelFactory<ITestService>("configBasedHttpService");
                    channel = factory.CreateChannel();
                    var baggage = channel.GetBaggage();

                    Assert.NotNull(baggage);
                    Assert.Single(baggage);
                    Assert.True(baggage.ContainsKey("foo"));
                    Assert.Equal("bar", baggage["foo"]);

                    var receivedRootId = channel.GetActivityRootId();
                    Assert.Equal(activity.RootId, receivedRootId);
                }
                finally
                {
                    TestHelper.Cleanup(channel, factory, host);
                }
            }
        }

        [Fact]
        public void NetTcpClientActivityPropagation()
        {
            using (var subscription = DiagnosticsHelper.SubscribeToListener())
            {
                ServiceHost host = null;
                ChannelFactory<ITestService> factory = null;
                ITestService channel = null;
                try
                {
                    var activity = new Activity("Root");
                    activity.AddBaggage("foo", "bar");
                    var id = activity.Id;
                    activity.Start();
                    host = new ServiceHost(typeof(ConfigBasedService));
                    host.Open();

                    factory = new ChannelFactory<ITestService>("configBasedNetTcpService");
                    channel = factory.CreateChannel();
                    var baggage = channel.GetBaggage();

                    Assert.NotNull(baggage);
                    Assert.Single(baggage);
                    Assert.True(baggage.ContainsKey("foo"));
                    Assert.Equal("bar", baggage["foo"]);

                    var receivedRootId = channel.GetActivityRootId();
                    Assert.Equal(activity.RootId, receivedRootId);
                }
                finally
                {
                    TestHelper.Cleanup(channel, factory, host);
                }
            }
        }
    }
}
