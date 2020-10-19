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

            serviceProvider = services
                .AddLogging(b => b.AddXUnit(OutputHelper).SetMinimumLevel(LogLevel.Trace))
                .AddRabbitMQ(configurationBuilder.SafeGet<RabbitMQConfiguration>())
                .BuildServiceProvider();

            var bus = serviceProvider.GetService<IMessaging>();
            bus.EnsureTopology("IntegrationTests");
        }

        [Fact]
        public void Test_Configuration_Topology()
        {
            using (var bus = serviceProvider.GetService<IMessaging>()) 
            { 
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
        }

        [Fact]
        public void Test_Missing_Topic_Route_Queue()
        {
            using (var bus = serviceProvider.GetService<IMessaging>())
            {
                var pub = bus.CreatePublisher();

                Assert.Throws<MissingRouteException>(() =>
                {
                    pub.Publish(Encoding.UTF8.GetBytes("Testing"), "test-missing-route");
                });

                Assert.Throws<PublishException>(() =>
                {
                    pub.Publish(Encoding.UTF8.GetBytes("Testing"), "do-not-exist");
                });

                var handler = new MessageHandler { OutputHelper = OutputHelper };
                var subscription = bus.Subscribe(handler.Handle, "do-not-exist");

                Assert.Throws<OperationInterruptedException>(() =>
                {
                    subscription.Start();
                });
            }
        }

        [Fact]
        public void Test_Publish_And_Subscription()
        {
            using (var bus = serviceProvider.GetService<IMessaging>())
            {
                var pub = bus.CreatePublisher();

                var msgA = $"Message A # {DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
                pub.Publish(Encoding.UTF8.GetBytes(msgA), "test-03");

                var msgB = $"Message B # {DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
                pub.Publish(Encoding.UTF8.GetBytes(msgB), "test-04");

                var handler = new MessageHandler { OutputHelper = OutputHelper };
                var sub = bus.Subscribe(handler.Handle,"test-03", "test-04");

                sub.Start();
                Thread.Sleep(2000);
                sub.Stop();

                Assert.True(handler.Messages.Contains(msgA));
                Assert.True(handler.Messages.Contains(msgB));
            }
        }

        [Fact]
        public async Task Test_Publish_And_Subscription_Async()
        {
            using (var bus = serviceProvider.GetService<IMessaging>())
            {
                var pub = bus.CreatePublisher();

                var msg = $"Message # {DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
                pub.Publish(Encoding.UTF8.GetBytes(msg), "test-12");

                var handler = new MessageHandler { OutputHelper = OutputHelper };
                var sub = bus.Subscribe(handler.Handle, "test-12");

                var cts = new CancellationTokenSource();

                _ = sub.StartAsync(cts.Token);

                await Task.Delay(2000);

                cts.Cancel();

                Assert.True(handler.Messages.Contains(msg));
            }
        }

        [Fact]
        public async Task Test_Publish_And_Subscription_Async_Awaited()
        {
            using (var bus = serviceProvider.GetService<IMessaging>())
            {
                var pub = bus.CreatePublisher();

                var msg = $"Message # {DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
                pub.Publish(Encoding.UTF8.GetBytes(msg), "test-13");

                var handler = new MessageHandler { OutputHelper = OutputHelper };
                var sub = bus.Subscribe(handler.Handle, "test-13");

                var cts = new CancellationTokenSource();

                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    cts.Cancel();
                });

                await sub.StartAsync(cts.Token);

                Assert.True(handler.Messages.Contains(msg));
            }
        }

        [Fact]
        public void Test_Publish_And_Subscription_Load()
        {
            using (var bus = serviceProvider.GetService<IMessaging>())
            {
                string[] queues = { "test-05", "test-06", "test-07" };

                Stopwatch StopWatch = new Stopwatch();
                StopWatch.Start();

                FloodPublish(100, queues);

                StopWatch.Stop();

                var handler = new MessageHandler
                {
                    OutputHelper = OutputHelper,
                    StopCount = 300
                };
                var sub = bus.Subscribe(handler.Handle, queues);

                sub.Start();
                Thread.Sleep(1000);
                sub.Stop();

                Assert.Equal(handler.StopCount, handler.Messages.Count);

                OutputHelper.WriteLine("Publish time: " + StopWatch.Elapsed.ToString());
                OutputHelper.WriteLine("Consume time: " + handler.StopWatch.Elapsed.ToString());
            }
        }

        [Fact]
        public void Test_Publish_And_Subscription_Disposed()
        {
            var bus = serviceProvider.GetService<IMessaging>();

            var pub = bus.CreatePublisher();
            var handler = new MessageHandler { OutputHelper = OutputHelper };
            var sub = bus.Subscribe(handler.Handle, "test-01");

            bus.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                pub.Publish(Encoding.UTF8.GetBytes("Testing"), "test-01");
            });

            Assert.Throws<ObjectDisposedException>(() =>
            {
                sub.Start();
            });
        }

        [Fact]
        public void Test_Subscription_Starts_Stops()
        {
            var msg = $"Message # {DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

            using (var bus = serviceProvider.GetService<IMessaging>())
            {
                var pub = bus.CreatePublisher();
                var handler = new MessageHandler { OutputHelper = OutputHelper };
                var sub = bus.Subscribe(handler.Handle, "test-08");

                pub.Publish(Encoding.UTF8.GetBytes(msg), "test-08");

                sub.Start();
                Thread.Sleep(500);
                sub.Stop();

                Assert.Single(handler.Messages);
                Assert.Equal(msg, handler.Messages.First());

                handler.Messages.Clear();

                pub.Publish(Encoding.UTF8.GetBytes(msg), "test-08");

                sub.Start();
                Thread.Sleep(500);
                sub.Stop();

                Assert.Single(handler.Messages);
                Assert.Equal(msg, handler.Messages.First());
            }
        }

        private readonly RabbitMQConfiguration configuration = new RabbitMQConfiguration
        {
            Hosts = new List<string> { "localhost" },
            VirtualHost = "Tests",
            Port = 5672,
            User = "guest",
            Password = "guest",
            PerQueuePrefetchCount = 10
        };

        [Fact]
        public void Test_Bad_Message_Rejected_On_First_Delivery()
        {
            configuration.MessageHandlerErrorBehavior = 
                MessageHandlerErrorBehavior.RejectOnFistDelivery;

            using (var bus = new RabbitMQMessaging(configuration,
                serviceProvider.GetService<ILoggerFactory>()))
            {
                bus.CreatePublisher().Publish(Encoding.UTF8.GetBytes(BadMessage.Text), "test-09");

                var handler = new MessageHandler { OutputHelper = OutputHelper };
                var sub = bus.Subscribe(handler.Handle, "test-09");

                sub.Start();
                Thread.Sleep(500);
                sub.Stop();

                // only first attempt to handle the message will be in the list
                Assert.Single(handler.Messages);
            }
        }

        [Fact]
        public void Test_Bad_Message_Rejected_On_Redelivery()
        {
            configuration.MessageHandlerErrorBehavior =
                MessageHandlerErrorBehavior.RejectOnRedelivery;

            using (var bus = new RabbitMQMessaging(configuration,
                serviceProvider.GetService<ILoggerFactory>()))
            {
                bus.CreatePublisher().Publish(Encoding.UTF8.GetBytes(BadMessage.Text), "test-10");

                var handler = new MessageHandler { OutputHelper = OutputHelper };
                var sub = bus.Subscribe(handler.Handle, "test-10");

                sub.Start();
                Thread.Sleep(500);
                sub.Stop();

                // 2 attempts to handle the message will be in the list
                Assert.Equal(2, handler.Messages.Count);
            }
        }

        [Fact]
        public void Test_Concurrency_Shared_Handler_Poc()
        {
            int receivedMessages = 0;
            var handler = new MessageHandler { OutputHelper = OutputHelper };
            void RunConcurrentSubscriptions(bool useLock)
            {
                var bus = serviceProvider.GetService<IMessaging>();

                FloodPublish(2000, "test-02");

                void sharedHandler(byte[] message, IMessageContext messageContext)
                {
                    // lock the counter update so we don't loose any
                    lock (handler.Messages)
                    {
                        receivedMessages++;
                    }
                    Thread.Sleep(5);
                    handler.Handle(message, messageContext);
                }

                var subs = new List<ISubscription>();
                for (int i = 0; i < 10; i++)
                {
                    subs.Add(bus.Subscribe(sharedHandler, "test-02"));
                }

                receivedMessages = 0;
                handler.UseLock = useLock;
                handler.Messages.Clear();

                foreach (var sub in subs)
                {
                    sub.Start();
                }

                Thread.Sleep(1000);

                foreach (var sub in subs)
                {
                    sub.Stop();
                }
            }

            // when concurrent threads try to add items to the Messages list in
            // MessageHandler (see: MessageHandler) but no lock is acquired there
            // will certainlly be losses
            RunConcurrentSubscriptions(useLock: false);
            Assert.True(receivedMessages > handler.Messages.Count);

            // when lock is acquired each thread must wait for their time
            // to add an item to the list, so no message is lost
            RunConcurrentSubscriptions(useLock: true);
            Assert.Equal(receivedMessages, handler.Messages.Count);
        }

        [Fact]
        public void Test_Concurrency_Stop_When_Idle()
        {
            using (var bus = serviceProvider.GetService<IMessaging>())
            {
                FloodPublish(100, "test-11");

                var watch = new Stopwatch();

                var handler = new MessageHandler { OutputHelper = OutputHelper };
                var sub = bus.Subscribe((message, messageContext) =>
                {
                    lock (watch)
                    {
                        handler.Handle(message, messageContext);
                        Thread.Sleep(100);
                        // restart timer as the last thing
                        watch.Restart();
                    }
                }, "test-11");

                watch.Start();
                sub.Start();

                // stops after 1 second idle, the sequence of operations are relevant!
                var idle = 1000;
                while (true)
                {
                    // no need to check timer before idle time have actually passed
                    Thread.Sleep(idle);
                    lock (watch)
                    {
                        if (watch.ElapsedMilliseconds > idle)
                        {
                            break;
                        }
                    }
                }

                sub.Stop();

                Assert.Equal(100, handler.Messages.Count);
            }
        }

        [Fact]
        public void Test_Concurrency_Stop_While_Handling()
        {
            using (var bus = serviceProvider.GetService<IMessaging>())
            {
                var pub = bus.CreatePublisher();

                var msg = $"Message # {DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

                // sending 2 messages, but will consume only one
                pub.Publish(Encoding.UTF8.GetBytes(msg), "test-01");
                pub.Publish(Encoding.UTF8.GetBytes(msg), "test-01");

                var handler = new MessageHandler { OutputHelper = OutputHelper };
                var sub = bus.Subscribe((message, messageContext) => {
                    // handling takes 1 second
                    Thread.Sleep(1000);
                    handler.Handle(message, messageContext);
                }, "test-01");

                // subscription will be stopped when second message
                // gets to be processed
                sub.Start();
                Thread.Sleep(100);
                sub.Stop();

                Assert.Single(handler.Messages);
            }
        }

        private void FloodPublish(int publishCount, params string[] queues)
        {
            var bus = serviceProvider.GetService<IMessaging>();
            var msg = $"Message # {DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

            var tasks = new List<Task>();

            var tasksCount = publishCount / 10;

            foreach (var queue in queues)
            {
                for (int t = 0; t < tasksCount; t++)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using (var pub = bus.CreatePublisher())
                        {
                            for (int m = 0; m < 10; m++)
                            {
                                pub.Publish(Encoding.UTF8.GetBytes($"{msg} to {queue}"), queue);
                            }
                        }
                    }));
                }
            }

            Task.WaitAll(tasks.ToArray());
        }
     }
}