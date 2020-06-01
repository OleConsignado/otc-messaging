using Otc.Messaging.RabbitMQ.Configurations;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Otc.Messaging.RabbitMQ.PredefinedTopologies.Tests
{
    public class SimpleQueueWithRetryTopologyTests
    {
        [Fact]
        public void TwoRetries_Success()
        {
            var rabbitMQConfiguration = new RabbitMQConfiguration();

            rabbitMQConfiguration.AddTopology<SimpleQueueWithRetryTopologyFactory>(
                                "mytopic", 4000, 8000);

            var exchanges = rabbitMQConfiguration.Topologies["mytopic"].Exchanges;

            Assert.Equal("mytopic-wait-0", GetSingleQueueDlxName(exchanges, "mytopic"));
            Assert.Equal("mytopic-retry-0", GetSingleQueueDlxName(exchanges, "mytopic-wait-0"));
            Assert.Equal("mytopic-wait-1", GetSingleQueueDlxName(exchanges, "mytopic-retry-0"));
            Assert.Equal("mytopic-retry-1", GetSingleQueueDlxName(exchanges, "mytopic-wait-1"));
            Assert.Equal("mytopic-dead", GetSingleQueueDlxName(exchanges, "mytopic-retry-1"));

            Assert.Equal(4000, GetSingleQueueTtl(exchanges, "mytopic-wait-0"));
            Assert.Equal(8000, GetSingleQueueTtl(exchanges, "mytopic-wait-1"));
        }

        [Fact]
        public void SingleRetry_Success()
        {
            var rabbitMQConfiguration = new RabbitMQConfiguration();

            rabbitMQConfiguration.AddTopology<SimpleQueueWithRetryTopologyFactory>(
                                "mytopic", 4000);

            var exchanges = rabbitMQConfiguration.Topologies["mytopic"].Exchanges;

            Assert.Equal("mytopic-wait-0", GetSingleQueueDlxName(exchanges, "mytopic"));
            Assert.Equal("mytopic-retry-0", GetSingleQueueDlxName(exchanges, "mytopic-wait-0"));
            Assert.Equal("mytopic-dead", GetSingleQueueDlxName(exchanges, "mytopic-retry-0"));

            Assert.Equal(4000, GetSingleQueueTtl(exchanges, "mytopic-wait-0"));
        }

        [Fact]
        public void NoRetry_Success()
        {
            var rabbitMQConfiguration = new RabbitMQConfiguration();

            rabbitMQConfiguration.AddTopology<SimpleQueueWithRetryTopologyFactory>(
                                "mytopic");

            var exchanges = rabbitMQConfiguration.Topologies["mytopic"].Exchanges;

            Assert.Equal("mytopic-dead", GetSingleQueueDlxName(exchanges, "mytopic"));
        }

        private string GetSingleQueueDlxName(IEnumerable<Exchange> exchanges, string topicName)
            => (string)GetSingleQueueArgument(exchanges, topicName, "x-dead-letter-exchange");
        

        private int GetSingleQueueTtl(IEnumerable<Exchange> exchanges, string topicName)
            => (int)GetSingleQueueArgument(exchanges, topicName, "x-message-ttl");

        private object GetSingleQueueArgument(IEnumerable<Exchange> exchanges, 
            string topicName, string argumentName)
        {
            var singleQueueArguments = exchanges
                .Single(e => e.Name == topicName)
                .Queues.Select(q => q.Arguments)
                .Single();

            if (singleQueueArguments.ContainsKey(argumentName))
            {
                return singleQueueArguments[argumentName];
            }

            return null;
        }
    }
}
