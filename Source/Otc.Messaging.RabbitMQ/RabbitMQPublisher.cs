using Microsoft.Extensions.Logging;
using Otc.Messaging.Abstractions;
using Otc.Messaging.Abstractions.Exceptions;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;

namespace Otc.Messaging.RabbitMQ
{
    /// <summary>
    /// Provides <see cref="IPublisher"/> for RabbitMQ.
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
        private readonly RabbitMQChannelEventsHandler channelEvents;
        private readonly TimeSpan timeout;
        private readonly ILogger logger;

        public RabbitMQPublisher(
            IModel channel,
            RabbitMQChannelEventsHandler channelEvents,
            uint timeout,
            ILoggerFactory loggerFactory)
        {
            this.channel = channel ?? throw new ArgumentNullException(nameof(channel));

            this.channelEvents = channelEvents ??
                throw new ArgumentNullException(nameof(channelEvents));

            this.timeout = TimeSpan.FromMilliseconds(timeout);

            logger = loggerFactory?.CreateLogger<RabbitMQPublisher>() ??
                throw new ArgumentNullException(nameof(loggerFactory));

            channel.ConfirmSelect();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Will throw <see cref="PublishException"/> when broker do not send confirmation which 
        /// may be due to timeout, topic does not exist or communication problems. InnerException
        /// will provide details.
        /// <para>
        /// Will throw <see cref="MissingRouteException"/> if broker receives and ackowledge the 
        /// message but do not find a route to a queue for that message.
        /// </para>
        /// </remarks>
        public void Publish(string topic, byte[] message, string queue = null
            , string messageId = null)
        {
            logger.LogInformation($"{nameof(Publish)}: Publish starting ...");

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
                channel.WaitForConfirmsOrDie(timeout);
            }
            catch (OperationInterruptedException ex) 
            {
                logger.LogError(ex, $"{nameof(Publish)}: Message " +
                    "{MessageId} to topic {Topic} and queue '{Queue}' failed!",
                    properties.MessageId, topic, queue);

                throw new PublishException(topic, queue, message, ex);
            }

            if (channelEvents.IsRouteMissing)
            {
                logger.LogError($"{nameof(Publish)}: Message " +
                    "{MessageId} to topic {Topic} and queue '{Queue}' failed. Route is missing!",
                    properties.MessageId, topic, queue);

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
            if (!disposed)
            {
                logger.LogDebug($"{nameof(RabbitMQPublisher)}: Disposing ...");

                if (channel.IsOpen)
                {
                    channel.Close();
                }

                channel.Dispose();

                logger.LogDebug($"{nameof(RabbitMQPublisher)}: Disposed.");

                disposed = true;
            }
        }
    }

    /// <summary>
    /// Provides named Delivery Modes for message publishing
    /// </summary>
    public static class DeliveryMode
    {
        public static byte NonPersistent = 1;
        public static byte Persistent = 2;
    }
}