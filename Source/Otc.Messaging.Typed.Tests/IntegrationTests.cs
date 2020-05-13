using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Otc.Extensions.Configuration;
using Otc.Messaging.Abstractions;
using Otc.Messaging.RabbitMQ.Configurations;
using System;
using System.IO;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Otc.Messaging.Typed.Tests
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
                .AddTypedMessaging()
                .BuildServiceProvider();

            var bus = serviceProvider.GetService<IMessaging>();
            bus.EnsureTopology("IntegrationTests");
        }

        public class MessageType
        {
            public string Text { get; set; }
        }

        [Fact]
        public void Test_Publish_And_Subscription()
        {
            using (var bus = serviceProvider.GetService<IMessaging>())
            {
                var pub = bus.CreatePublisher();

                var sentMessage = new MessageType
                {
                    Text = $"Message # {DateTimeOffset.Now.ToUnixTimeMilliseconds()}"
                };

                pub.Publish(sentMessage, "test-typed-messages");

                MessageType receivedMessage = new MessageType();

                void handler(MessageType message, IMessageContext messageContext)
                {
                    receivedMessage = message;
                }

                var sub = bus.Subscribe<MessageType>(handler, "test-typed-messages");

                sub.Start();
                Thread.Sleep(500);
                sub.Stop();

                Assert.Equal(receivedMessage.Text, sentMessage.Text);
            }
        }
    }
}
