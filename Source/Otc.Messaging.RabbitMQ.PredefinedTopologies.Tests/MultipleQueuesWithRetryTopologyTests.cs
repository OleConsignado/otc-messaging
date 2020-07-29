using Microsoft.Extensions.Logging;
using Otc.Messaging.Abstractions;
using Otc.Messaging.RabbitMQ.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace Otc.Messaging.RabbitMQ.PredefinedTopologies.Tests
{
    public class MultipleQueuesWithRetryTopologyTests
    {
        [Fact]
        public void ThreeQueues_ThreeRetries_Success()
        {
            var configuration = new RabbitMQConfiguration();

            configuration.AddTopology<MultipleQueuesWithRetryTopologyFactory>(
                "mytopic", "q1", "q2", "q3", 4000, 8000, 16000);

            var exchanges = configuration.Topologies["mytopic"].Exchanges;
            var exchange = exchanges.First();

            Assert.Equal("mytopic", exchange.Name);
            Assert.Equal(3, exchange.Queues.Count);

            for (int i = 1; i < 4; i++)
            {
                Assert.Equal($"q{i}-wait-0", GetSingleQueueDlxName(exchanges, "mytopic", $"q{i}"));
                Assert.Equal($"q{i}-retry-0", GetSingleQueueDlxName(exchanges, $"q{i}-wait-0"));
                Assert.Equal($"q{i}-wait-1", GetSingleQueueDlxName(exchanges, $"q{i}-retry-0"));
                Assert.Equal($"q{i}-retry-1", GetSingleQueueDlxName(exchanges, $"q{i}-wait-1"));
                Assert.Equal($"q{i}-wait-2", GetSingleQueueDlxName(exchanges, $"q{i}-retry-1"));
                Assert.Equal($"q{i}-retry-2", GetSingleQueueDlxName(exchanges, $"q{i}-wait-2"));
                Assert.Equal($"q{i}-dead", GetSingleQueueDlxName(exchanges, $"q{i}-retry-2"));

                Assert.Equal(4000, GetSingleQueueTtl(exchanges, $"q{i}-wait-0"));
                Assert.Equal(8000, GetSingleQueueTtl(exchanges, $"q{i}-wait-1"));
                Assert.Equal(16000, GetSingleQueueTtl(exchanges, $"q{i}-wait-2"));
            }
        }

        [Fact]
        public void TwoQueues_SingleRetry_Success()
        {
            var configuration = new RabbitMQConfiguration();

            configuration.AddTopology<MultipleQueuesWithRetryTopologyFactory>(
                "mytopic", "q1", "q2", 4000);

            var exchanges = configuration.Topologies["mytopic"].Exchanges;
            var exchange = exchanges.First();

            Assert.Equal("mytopic", exchange.Name);
            Assert.Equal(2, exchange.Queues.Count);

            Assert.Equal("q1-wait-0", GetSingleQueueDlxName(exchanges, "mytopic", "q1"));
            Assert.Equal("q1-retry-0", GetSingleQueueDlxName(exchanges, "q1-wait-0"));
            Assert.Equal("q1-dead", GetSingleQueueDlxName(exchanges, "q1-retry-0"));

            Assert.Equal(4000, GetSingleQueueTtl(exchanges, "q1-wait-0"));

            Assert.Equal("q2-wait-0", GetSingleQueueDlxName(exchanges, "mytopic", "q2"));
            Assert.Equal("q2-retry-0", GetSingleQueueDlxName(exchanges, "q2-wait-0"));
            Assert.Equal("q2-dead", GetSingleQueueDlxName(exchanges, "q2-retry-0"));

            Assert.Equal(4000, GetSingleQueueTtl(exchanges, "q2-wait-0"));
        }

        [Fact]
        public void TwoQueues_NoRetry_Success()
        {
            var configuration = new RabbitMQConfiguration();

            configuration.AddTopology<MultipleQueuesWithRetryTopologyFactory>(
                "mytopic", "q1", "q2");

            var exchanges = configuration.Topologies["mytopic"].Exchanges;
            var exchange = exchanges.First();

            Assert.Equal("mytopic", exchange.Name);
            Assert.Equal(2, exchange.Queues.Count);

            Assert.Equal("q1-dead", GetSingleQueueDlxName(exchanges, "mytopic", "q1"));
            Assert.Equal("q2-dead", GetSingleQueueDlxName(exchanges, "mytopic", "q2"));
        }

        [Fact]
        public void SingleQueue_Error()
        {
            var configuration = new RabbitMQConfiguration();

            var ex = Assert.Throws<ArgumentException>(() =>
            {
                configuration.AddTopology<MultipleQueuesWithRetryTopologyFactory>(
                    "mytopic", "q1", 4000);
            });

            Assert.Contains("minimum of 2 queues", ex.Message);
        }

        [Fact]
        public void NoQueue_Error()
        {
            var configuration = new RabbitMQConfiguration();

            var ex = Assert.Throws<ArgumentException>(() =>
            {
                configuration.AddTopology<MultipleQueuesWithRetryTopologyFactory>(
                    "mytopic", 4000);
            });

            Assert.Contains("minimum of 2 queues", ex.Message);
        }

        [Fact]
        public void Invalid_Argument_Type_Error()
        {
            var configuration = new RabbitMQConfiguration();

            var ex = Assert.Throws<ArgumentException>(() =>
            {
                configuration.AddTopology<MultipleQueuesWithRetryTopologyFactory>(
                    "mytopic", "q1", "q2", 10000, 1.1M);
            });

            Assert.Contains("must be of type string or int", ex.Message);
        }

        [Fact]
        public void TwoQueues_TwoRetries_IntegratedTest_Success()
        {
            var configuration = new RabbitMQConfiguration
            {
                Hosts = new List<string> { "localhost" },
                User = "guest",
                Password = "guest"
            };

            configuration.AddTopology<MultipleQueuesWithRetryTopologyFactory>(
                "test-funout", "test-funout-q1", "test-funout-q2", 4000, 8000);

            using (var bus = new RabbitMQMessaging(configuration, new LoggerFactory()))
            {
                bus.EnsureTopology("test-funout");

                bus.CreatePublisher().
                    Publish(Encoding.UTF8.GetBytes("Simple Message"), "test-funout");

                var messages = new List<IMessageContext>();
                var sub = bus.Subscribe((message, messageContext) =>
                {
                    messages.Add(messageContext);
                }, "test-funout-q1", "test-funout-q2");

                sub.Start();
                Thread.Sleep(500);
                sub.Stop();

                Assert.Equal(2, messages.Count);
                Assert.Contains(messages, x => x.Queue == "test-funout-q1");
                Assert.Contains(messages, x => x.Queue == "test-funout-q2");
            }
        }

        private string GetSingleQueueDlxName(IEnumerable<Exchange> exchanges,
            string exchangeName, string queueName = null)
            => (string)GetSingleQueueArgument(exchanges,
                exchangeName, queueName, "x-dead-letter-exchange");


        private int GetSingleQueueTtl(IEnumerable<Exchange> exchanges,
            string exchangeName, string queueName = null)
            => (int)GetSingleQueueArgument(exchanges,
                exchangeName, queueName, "x-message-ttl");

        private object GetSingleQueueArgument(IEnumerable<Exchange> exchanges,
            string exchangeName, string queueName, string argumentName)
        {
            if (queueName == null)
            {
                queueName = exchangeName;
            }

            var singleQueueArguments = exchanges
                .Single(e => e.Name == exchangeName)
                .Queues.Where(q => q.Name == queueName)
                .Select(q => q.Arguments)
                .Single();

            if (singleQueueArguments.ContainsKey(argumentName))
            {
                return singleQueueArguments[argumentName];
            }

            return null;
        }
    }
}
