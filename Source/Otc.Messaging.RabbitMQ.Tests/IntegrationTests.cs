using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Otc.Extensions.Configuration;
using Otc.Messaging.Abstractions;
using Otc.Messaging.Abstractions.Exceptions;
using Otc.Messaging.RabbitMQ.Configurations;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Otc.Messaging.RabbitMQ.Tests
{
    public class IntegrationTests
    {
        private ITestOutputHelper OutputHelper { get; }
        private readonly IServiceProvider serviceProvider;

        public IntegrationTests(ITestOutputHelper output)
        {
            OutputHelper = output;

            IServiceCollection services = new ServiceCollection();

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var configuration = configurationBuilder.SafeGet<RabbitMQConfiguration>();

            serviceProvider = services
                .AddLogging(b => b.AddXUnit(OutputHelper).SetMinimumLevel(LogLevel.Trace))
                .AddRabbitMQ(configuration)
                .BuildServiceProvider();

            var bus = serviceProvider.GetService<IMessaging>();

            bus.EnsureTopology("IntegrationTests");
        }

        class MessageHandler
        {
            public ITestOutputHelper OutputHelper;
            public IList<string> Messages = new List<string>();
            public Stopwatch StopWatch = new Stopwatch();
            public int StopCount;

            public void Handle(IMessage message)
            {
                if (!StopWatch.IsRunning)
                    StopWatch.Start();

                var text = Encoding.UTF8.GetString(message.Body);

                OutputHelper?.WriteLine($"   Handling message: {text}");

                Messages.Add(text);

                if (Messages.Count() >= StopCount)
                {
                    StopWatch.Stop();
                }

                if (text.Contains("Gracious madam, I that do bring the news made not the match. He’s married, madam."))
                {
                    throw new InvalidOperationException($"Rogue, thou hast lived too long");
                }
            }
        }

        [Fact]
        public void Test_Configuration_Topology()
        {
            using var bus = serviceProvider.GetService<IMessaging>();

            Assert.Throws<EnsureTopologyException>(() =>
            {
                bus.EnsureTopology("Undefined");
            });

            try
            {
                bus.EnsureTopology("SimpleQueueWithRetry");
            }
            catch (Exception ex)
            {
                Assert.False(true, "Expected no exception, but got: " + ex.Message);
            }
        }

        [Fact]
        public void Test_Publish_Missing_Topic()
        {
            using var bus = serviceProvider.GetService<IMessaging>();

            Assert.Throws<PublishException>(() =>
            {
                bus.CreatePublisher().Publish("do-not-exist", Encoding.UTF8.GetBytes("Testing"));
            });

            Assert.Throws<MissingRouteException>(() =>
            {
                bus.CreatePublisher().Publish("test-missing-route", Encoding.UTF8.GetBytes("Testing"));
            });
        }

        [Fact]
        public void Test_Publish_Missing_Route()
        {
            using var bus = serviceProvider.GetService<IMessaging>();

            var pub = bus.CreatePublisher();

            Assert.Throws<MissingRouteException>(() =>
            {
                pub.Publish("test-missing-route", Encoding.UTF8.GetBytes("Testing"));
            });

            Assert.Throws<PublishException>(() =>
            {
                pub.Publish("do-not-exist", Encoding.UTF8.GetBytes("Testing"));
            });
        }

        [Fact]
        public void Test_Publish_Disposed()
        {
            var bus = serviceProvider.GetService<IMessaging>();

            bus.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                bus.CreatePublisher().Publish("test-01", Encoding.UTF8.GetBytes("Testing"));
            });
        }

        [Fact]
        public void Test_Subscription_Disposed()
        {
            var bus = serviceProvider.GetService<IMessaging>();

            var handler = new MessageHandler { OutputHelper = OutputHelper };
            var sub = bus.Subscribe(message => { handler.Handle(message); }, "test-01");

            bus.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                sub.Start();
            });
        }

        [Fact]
        public void Test_Subscribe_Missing_Queue()
        {
            using var bus = serviceProvider.GetService<IMessaging>();

            var handler = new MessageHandler { OutputHelper = OutputHelper };
            var subscription = bus.Subscribe(message => { handler.Handle(message); }, "do-not-exist");

            Assert.Throws<OperationInterruptedException>(() =>
            {
                subscription.Start();
            });
        }

        [Fact]
        public void Test_Publish_And_Consume()
        {
            var msg = $"Message # {DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

            using var bus = serviceProvider.GetService<IMessaging>();

            bus.CreatePublisher().Publish("test-02", Encoding.UTF8.GetBytes(msg));

            var handler = new MessageHandler { OutputHelper = OutputHelper };
            var sub = bus.Subscribe(message => { handler.Handle(message); }, "test-02");

            sub.Start();
            Thread.Sleep(500);
            sub.Stop();

            Assert.Single(handler.Messages);
            Assert.Equal(msg, handler.Messages.First());
        }

        [Fact]
        public void Test_Publish_And_Consume_Checking_Content()
        {
            using var bus = serviceProvider.GetService<IMessaging>();

            var pub = bus.CreatePublisher();

            var msgA = $"Message A # {DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
            pub.Publish("test-03", Encoding.UTF8.GetBytes(msgA));

            var msgB = $"Message B # {DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
            pub.Publish("test-04", Encoding.UTF8.GetBytes(msgB));

            var handler = new MessageHandler { OutputHelper = OutputHelper };
            var sub = bus.Subscribe(message => { handler.Handle(message); }, "test-03", "test-04");

            sub.Start();
            Thread.Sleep(500);
            sub.Stop();

            Assert.True(handler.Messages.Contains(msgA));
            Assert.True(handler.Messages.Contains(msgB));
        }

        [Fact]
        public void Test_Publish_And_Consume_Load()
        {
            using var bus = serviceProvider.GetService<IMessaging>();

            string[] queues = { "test-05", "test-06", "test-07" };

            var msg = $"Message # {DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

            Stopwatch StopWatch = new Stopwatch();
            StopWatch.Start();

            var tasks = new List<Task>();
            var publishCount = 200;

            for (int i = 0; i < publishCount; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    using var pub = bus.CreatePublisher();
                    foreach (var queue in queues)
                    {
                        pub.Publish(queue, Encoding.UTF8.GetBytes($"{msg} ({i}) from {queue}"));
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            StopWatch.Stop();

            var handler = new MessageHandler
            {
                OutputHelper = OutputHelper,
                StopCount = 600
            };
            var sub = bus.Subscribe(message => { handler.Handle(message); }, queues);

            sub.Start();
            Thread.Sleep(1000);
            sub.Stop();

            Assert.Equal(handler.StopCount, handler.Messages.Count);

            OutputHelper.WriteLine("Publish time: " + StopWatch.Elapsed.ToString());
            OutputHelper.WriteLine("Consume time: " + handler.StopWatch.Elapsed.ToString());
        }

        [Fact]
        public void Test_Subscription_Start_Stop_Start_Stop()
        {
            var msg = $"Message # {DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

            using var bus = serviceProvider.GetService<IMessaging>();

            var pub = bus.CreatePublisher();
            pub.Publish("test-08", Encoding.UTF8.GetBytes(msg));

            var handler = new MessageHandler { OutputHelper = OutputHelper };
            var sub = bus.Subscribe(message => { handler.Handle(message); }, "test-08");

            sub.Start();
            Thread.Sleep(500);
            sub.Stop();

            Assert.Single(handler.Messages);
            Assert.Equal(msg, handler.Messages.First());

            handler.Messages.Clear();

            pub.Publish("test-08", Encoding.UTF8.GetBytes(msg));

            sub.Start();
            Thread.Sleep(500);
            sub.Stop();

            Assert.Single(handler.Messages);
            Assert.Equal(msg, handler.Messages.First());
        }

        [Fact]
        public void Test_Consuming_Exceptional_Message_Rejected_On_First_Delivery()
        {
            var msg = $"Gracious madam, I that do bring the news made not the match. He’s married, madam.";

            using var bus = new RabbitMQMessaging(new RabbitMQConfiguration
            {
                Host = "localhost",
                Port = 5672,
                User = "guest",
                Password = "guest",
                MessageHandlerErrorBehavior = MessageHandlerErrorBehavior.RejectOnFistDelivery,
                PerQueuePrefetchCount = 10
            }, serviceProvider.GetService<ILoggerFactory>());

            bus.CreatePublisher().Publish("test-09", Encoding.UTF8.GetBytes(msg));

            var handler = new MessageHandler { OutputHelper = OutputHelper };
            var sub = bus.Subscribe(message => { handler.Handle(message); }, "test-09");

            sub.Start();
            Thread.Sleep(500);
            sub.Stop();

            // only first attempt to handle the message will be in the list
            Assert.Single(handler.Messages);
        }

        [Fact]
        public void Test_Consuming_Exceptional_Message_Rejected_On_Redelivery()
        {
            var msg = $"Gracious madam, I that do bring the news made not the match. He’s married, madam.";

            using var bus = new RabbitMQMessaging(new RabbitMQConfiguration
            {
                Host = "localhost",
                Port = 5672,
                User = "guest",
                Password = "guest",
                MessageHandlerErrorBehavior = MessageHandlerErrorBehavior.RejectOnRedelivery,
                PerQueuePrefetchCount = 10
            }, serviceProvider.GetService<ILoggerFactory>());

            bus.CreatePublisher().Publish("test-09", Encoding.UTF8.GetBytes(msg));

            var handler = new MessageHandler { OutputHelper = OutputHelper };
            var sub = bus.Subscribe(message => { handler.Handle(message); }, "test-09");

            sub.Start();
            Thread.Sleep(500);
            sub.Stop();

            // 2 attempts to handle the message will be in the list
            Assert.Equal(2, handler.Messages.Count);
        }
    }
}