using Microsoft.Extensions.Logging;
using Otc.Messaging.Abstractions;
using Otc.Messaging.RabbitMQ.Configurations;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Otc.Messaging.RabbitMQ
{
    /// <summary>
    /// Provides a factory for RabbitMQ <see cref="IPublisher"/> and <see cref="ISubscription"/>.
    /// </summary>
    /// <remarks>
    /// It holds a connection with the broker that is supposed to be long-lived, i.e., this object
    /// instance may usually be registered as a singleton in your application life-cycle, and
    /// provide scoped instances of <see cref="IPublisher"/> and/or <see cref="ISubscription"/>.
    /// </remarks>
    public class RabbitMQMessaging : IMessaging
    {
        private readonly RabbitMQConfiguration configuration;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger logger;
        private readonly ICollection<RabbitMQPublisher> publishers;
        private readonly ICollection<RabbitMQSubscription> subscriptions;

        private IConnection connection;
        private IConnection Connection
        {
            get
            {
                if (connection?.IsOpen ?? false)
                {
                    return connection;
                }

                var factory = new ConnectionFactory()
                {
                    HostName = configuration.Host,
                    Port = configuration.Port,
                    UserName = configuration.User,
                    Password = configuration.Password,
                    ClientProvidedName = nameof(RabbitMQ)
                };

                factory.AutomaticRecoveryEnabled = true;
                factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);

                logger.LogInformation($"{nameof(Connection)}: Connecting to " +
                    $"{configuration.Host}:{configuration.Port} with user {configuration.User}");

                connection = factory.CreateConnection();
                return connection;
            }
        }

        public RabbitMQMessaging(
            RabbitMQConfiguration configuration,
            ILoggerFactory loggerFactory)
        {
            this.configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));

            this.loggerFactory = loggerFactory ??
                throw new ArgumentNullException(nameof(loggerFactory));

            logger = loggerFactory.CreateLogger<RabbitMQMessaging>();

            publishers = new List<RabbitMQPublisher>();

            subscriptions = new List<RabbitMQSubscription>();
        }

        /// <inheritdoc/>>
        public IPublisher CreatePublisher()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(RabbitMQMessaging));
            }

            var (channel, channelEvents) = CreateChannel();

            var publisher = new RabbitMQPublisher(channel, channelEvents,
                configuration.PublishConfirmationTimeoutMilliseconds, this, loggerFactory);

            publishers.Add(publisher);

            logger.LogInformation($"{nameof(CreatePublisher)}: Publisher created");

            return publisher;
        }

        /// <inheritdoc/>
        public ISubscription Subscribe(Action<IMessage> handler, params string[] queues)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(RabbitMQMessaging));
            }

            var (channel, channelEvents) = CreateChannel();

            var subscription = new RabbitMQSubscription(channel, channelEvents, handler,
                configuration, this, loggerFactory, queues);

            subscriptions.Add(subscription);

            logger.LogInformation($"{nameof(Subscribe)}: Subscription created");

            return subscription;
        }

        /// <summary>
        /// Applies a given topology to the broker.
        /// </summary>
        /// <param name="name">Topology name loaded in <see cref="Topologies"/>.</param>
        /// <remarks>
        /// All Exchanges and it's queues and bindings will be declared via 
        /// ExchangeDeclare, QueueDeclare e QueueBind.
        /// It is not necessary to apply theses configurations all the time if
        /// your topology defines exchanges and queues as durables.
        /// </remarks>
        /// <exception cref="EnsureTopologyException">
        /// Thrown if any error of configuration, connection or permissions occurs while
        /// applying given topology.
        /// </exception>
        public void EnsureTopology(string name)
        {
            using (var channel = Connection.CreateModel())
            {
                configuration.EnsureTopology(name, channel);
                channel.Close();
            }

            logger.LogInformation($"{nameof(EnsureTopology)}: Topology " +
                "{Topology} applied successfully!", name);
        }

        private (IModel, RabbitMQChannelEventsHandler) CreateChannel()
        {
            var channel = Connection.CreateModel();

            logger.LogInformation($"{nameof(CreateChannel)}: Channel " +
                $"{channel.ChannelNumber} created");

            channel.BasicQos(prefetchSize: 0, prefetchCount: configuration.PerQueuePrefetchCount,
                global: false);

            logger.LogDebug($"{nameof(CreateChannel)}: " +
                $"{nameof(configuration.PerQueuePrefetchCount)} set to " +
                $"{configuration.PerQueuePrefetchCount}");

            return (channel, new RabbitMQChannelEventsHandler(channel, loggerFactory));
        }

        internal void RemovePublisher(RabbitMQPublisher item)
        {
            publishers.Remove(item);
        }

        internal void RemoveSubscription(RabbitMQSubscription item)
        {
            subscriptions.Remove(item);
        }

        private bool disposed = false;

        /// <summary>
        /// Disposing this object will dispose all subscriptions and publishers created by it.
        /// Connection will be closed too.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                logger.LogDebug($"{nameof(RabbitMQMessaging)}: Disposing ...");

                foreach (var publisher in publishers.ToList())
                {
                    publisher?.Dispose();
                }

                foreach (var subscription in subscriptions.ToList())
                {
                    subscription?.Dispose();
                }

                if (connection != null)
                {
                    if (connection.IsOpen)
                    {
                        connection.Close();
                    }

                    connection.Dispose();
                }

                logger.LogDebug($"{nameof(RabbitMQMessaging)}: Disposed.");
            }

            disposed = true;
        }
    }
}