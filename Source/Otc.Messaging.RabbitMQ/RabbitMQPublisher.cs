using Microsoft.Extensions.Logging;
using Otc.Messaging.Abstractions;
using Otc.Messaging.Abstractions.Exceptions;
using RabbitMQ.Client;
using System;
using System.ComponentModel;

namespace Otc.Messaging.RabbitMQ
{
    /// <summary>
    /// Provides implementation of <see cref="IPublisher"/> for RabbitMQ.
    /// </summary>
    /// <remarks>
    /// This is not thread safe. When concurrent publishing is needed you should create as many
    /// publishers as the number of concurrent processings will exist.
    /// As each publisher has its own channel internally, you should consider the max number of
    /// channels allowed for your connection.
    /// </remarks>
    public class RabbitMQPublisher : IPublisher
    {
        private readonly IModel channel;
        private readonly TimeSpan timeout;
        private readonly RabbitMQMessaging messaging;
        private readonly ILogger logger;

        // holds per channel instance of events listener
        [EditorBrowsable(EditorBrowsableState.Never)]
        public readonly RabbitMQChannelEventsHandler channelEvents;

        public RabbitMQPublisher(
            IModel channel,
            RabbitMQChannelEventsHandler channelEvents,
            uint timeout,
            RabbitMQMessaging messaging,
            ILoggerFactory loggerFactory)
        {
            this.channel = channel ?? throw new ArgumentNullException(nameof(channel));

            this.channelEvents = channelEvents ??
                throw new ArgumentNullException(nameof(channelEvents));

            this.timeout = TimeSpan.FromMilliseconds(timeout);

            this.messaging = messaging ??
                throw new ArgumentNullException(nameof(messaging));

            logger = loggerFactory?.CreateLogger<RabbitMQPublisher>() ??
                throw new ArgumentNullException(nameof(loggerFactory));

            channel.ConfirmSelect();
        }

        /// <inheritdoc/>
        public void Publish(byte[] message, string topic, string queue = null
            , string messageId = null)
        {
            logger.LogDebug($"{nameof(Publish)}: Publish starting ...");

            if (disposed)
            {
                throw new ObjectDisposedException(nameof(RabbitMQPublisher));
            }

            if (topic is null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (queue is null)
            {
                queue = "";
            }

            var properties = SetupProperties(messageId);

            channel.BasicPublish(exchange: topic, routingKey: queue, mandatory: true,
                basicProperties: properties, body: message);

            try
            {
                // this operation is synchronous, so when this call returns, the 
                // corresponding channel events already happened and states were 
                // updated, like in the missing route checked bellow
                channel.WaitForConfirmsOrDie(timeout);
            }
            catch (Exception ex)
            {
                throw new PublishException(topic, queue, message, ex);
            }

            if (channelEvents.IsRouteMissing)
            {
                throw new MissingRouteException(topic, queue, message);
            }

            logger.LogInformation($"{nameof(Publish)}: Message " +
                "{MessageId} published to topic {Topic} and queue '{Queue}'",
                properties.MessageId, topic, queue);
        }

        private IBasicProperties SetupProperties(string messageId)
        {
            var properties = channel.CreateBasicProperties();

            properties.DeliveryMode = DeliveryMode.Persistent;
            properties.MessageId = messageId ?? Guid.NewGuid().ToString("N");
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            return properties;
        }

        private bool disposed = false;

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
                logger.LogDebug($"{nameof(RabbitMQPublisher)}: Disposing ...");

                if (channel.IsOpen)
                {
                    channel.Close();
                }

                channel.Dispose();

                messaging.RemovePublisher(this);

                logger.LogDebug($"{nameof(RabbitMQPublisher)}: Disposed.");
            }

            disposed = true;
        }

        /// <summary>
        /// Provides named Delivery Modes for message publishing
        /// </summary>
        public static class DeliveryMode
        {
            public const byte NonPersistent = 1;
            public const byte Persistent = 2;
        }
    }
}