using Microsoft.Extensions.Logging;
using Otc.Messaging.Abstractions;
using Otc.Messaging.RabbitMQ.Configurations;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Otc.Messaging.RabbitMQ
{
    /// <summary>
    /// Provides <see cref="ISubscription"/> for RabbitMQ
    /// </summary>
    /// <remarks>
    /// This is not thread safe. When concurrent subscriptions are needed you should create as many 
    /// subscriptions as the number of concurrent processings will exist. 
    /// As each subscription has its own channel internally, you should consider the max number of
    /// channels allowed for your connection.
    /// <para>
    /// The same applies for your handlers, if you register a single handler instance within
    /// 2+ subscriptions and start concurrent consuming, be aware that you'll have to manage 
    /// concurrent access to your handler's resources and states.
    /// </para>
    /// </remarks>
    public class RabbitMQSubscription : ISubscription
    {
        private readonly IModel channel;
        private readonly RabbitMQChannelEventsHandler channelEvents;
        private readonly Action<IMessage> handler;
        private readonly RabbitMQConfiguration configuration;
        private readonly string[] queues;
        private readonly ILogger logger;
        private readonly IDictionary<string, string> consumersToQueues;

        public RabbitMQSubscription(
            IModel channel,
            RabbitMQChannelEventsHandler channelEvents,
            Action<IMessage> handler,
            RabbitMQConfiguration configuration,
            ILoggerFactory loggerFactory,
            params string[] queues)
        {
            this.channel = channel ??
                throw new ArgumentNullException(nameof(channel));

            this.channelEvents = channelEvents ??
                throw new ArgumentNullException(nameof(channelEvents));

            this.handler = handler ??
                throw new ArgumentNullException(nameof(handler));

            this.configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));

            this.queues = queues ??
                throw new ArgumentNullException(nameof(queues));

            logger = loggerFactory?.CreateLogger<RabbitMQSubscription>() ??
                throw new ArgumentNullException(nameof(loggerFactory));

            consumersToQueues = new Dictionary<string, string>();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// The consumer of messages runs on a thread of its own.
        /// All messages from all subscribed queues will be passed to the handler 
        /// that will run in this one thread too.
        /// </remarks>
        public void Start()
        {
            logger.LogInformation($"{nameof(Start)}: Subscription starting ...");

            if (disposed)
            {
                throw new ObjectDisposedException(nameof(RabbitMQSubscription));
            }

            if (consumersToQueues.Count > 0)
            {
                throw new InvalidOperationException("Subscription already started!");
            }

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += MessageReceived;

            foreach (var queue in queues)
            {
                var tag = "";
                lock(consumersToQueues)
                {
                    tag = channel.BasicConsume(queue: queue, autoAck: false, consumer: consumer);
                    consumersToQueues.Add(tag, queue);
                }

                logger.LogInformation($"{nameof(Start)}: Consumer {tag} of queue " +
                    "{Queue} started", queue);

                logger.LogDebug($"{nameof(Start)}: Consumer {tag} of queue " +
                    "{Queue} declared on thread " + $"{Thread.CurrentThread.ManagedThreadId}",
                    queue);
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Stops queues consumption, but internal channel remains opened to allow for a new start.
        /// </remarks>
        public void Stop()
        {
            logger.LogInformation($"{nameof(Stop)}: Subscription stopping ...");

            if (disposed)
            {
                throw new ObjectDisposedException(nameof(RabbitMQSubscription));
            }

            if (consumersToQueues.Count == 0)
            {
                return;
            }

            if (channel?.IsOpen ?? false)
            {
                foreach (var tag in consumersToQueues)
                {
                    channel.BasicCancel(tag.Key);

                    logger.LogInformation($"{nameof(Stop)}: Consumer {tag.Key} " +
                        $"of queue " + "{Queue} stopped", tag.Value);
                }
            }

            consumersToQueues.Clear();
        }

        private void MessageReceived(object sender, BasicDeliverEventArgs ea)
        {
            logger.LogDebug($"{nameof(MessageReceived)}: Message " +
                "{MessageId} received by " + $"{ea.ConsumerTag} " +
                $"with DeliveryTag {ea.DeliveryTag}", ea.BasicProperties.MessageId);

            var queue = "";
            lock (consumersToQueues)
            {
                queue = consumersToQueues[ea.ConsumerTag];
            }
            var message = new RabbitMQMessage(ea, queue);

            logger.LogDebug($"{nameof(Start)}: Consumer {ea.ConsumerTag} of queue " +
                "{Queue} running on thread " + $"{Thread.CurrentThread.ManagedThreadId}",
                queue);

            try
            {
                logger.LogInformation($"{nameof(MessageReceived)}: Message " +
                    "{MessageId} received from queue {Queue} " +
                    $"with DeliveryTag {ea.DeliveryTag}", message.Id, queue);

                handler.Invoke(message);

                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

                logger.LogInformation($"{nameof(MessageReceived)}: Message " +
                    "{MessageId} handle succeeded!", message.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"{nameof(MessageReceived)}: Message " +
                    "{MessageId} handle failed!", message.Id);

                var requeue = false;

                if (configuration.OnMessageHandlerError ==
                    OnMessageHandlerError.RejectOnRedelivery)
                {
                    if (ea.Redelivered == false)
                    {
                        requeue = true;
                    }
                }

                channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: requeue);

                logger.LogInformation($"{nameof(MessageReceived)}: Message " +
                    "{MessageId} rejected with requeue set to " + $"{requeue}", message.Id);

                return;
            }
        }

        private bool disposed = false;

        public void Dispose()
        {
            if (!disposed)
            {
                logger.LogDebug($"{nameof(RabbitMQSubscription)}: Disposing ...");

                if (channel.IsOpen)
                {
                    channel.Close();
                }

                channel.Dispose();

                logger.LogDebug($"{nameof(RabbitMQSubscription)}: Disposed.");

                disposed = true;
            }
        }
    }
}