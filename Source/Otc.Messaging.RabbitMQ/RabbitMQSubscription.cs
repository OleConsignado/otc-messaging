using Microsoft.Extensions.Logging;
using Otc.Messaging.Abstractions;
using Otc.Messaging.RabbitMQ.Configurations;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Otc.Messaging.RabbitMQ
{
    /// <summary>
    /// Provides implementation of <see cref="ISubscription"/> for RabbitMQ
    /// </summary>
    /// <remarks>
    /// This is not thread safe. When concurrent subscriptions are needed you should create as many
    /// subscriptions as the number of concurrent processings will exist.
    /// As each subscription has its own channel internally, you should consider the max number of
    /// channels allowed for your connection.
    /// The same applies for your handlers, if you register a single handler instance within
    /// 2+ subscriptions and start concurrent consuming them, be aware that you'll have to manage
    /// concurrent access to your handler's resources and states.
    /// </remarks>
    public class RabbitMQSubscription : ISubscription
    {
        private readonly IModel channel;
        private readonly Action<byte[], IMessageContext> handler;
        private readonly RabbitMQConfiguration configuration;
        private readonly RabbitMQMessaging messaging;
        private readonly string[] queues;
        private readonly ILogger logger;
        private readonly IDictionary<string, string> consumersToQueues;

        // holds per channel instance of events listener
        [EditorBrowsable(EditorBrowsableState.Never)]
        public readonly RabbitMQChannelEventsHandler channelEvents;

        private CancellationToken cancellationToken;

        public RabbitMQSubscription(
            IModel channel,
            RabbitMQChannelEventsHandler channelEvents,
            Action<byte[], IMessageContext> handler,
            RabbitMQConfiguration configuration,
            RabbitMQMessaging messaging,
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

            this.messaging = messaging ??
                throw new ArgumentNullException(nameof(messaging));

            this.queues = queues ??
                throw new ArgumentNullException(nameof(queues));

            logger = loggerFactory?.CreateLogger<RabbitMQSubscription>() ??
                throw new ArgumentNullException(nameof(loggerFactory));

            consumersToQueues = new Dictionary<string, string>();
        }

        /// <inheritdoc/>
        /// <inheritdoc cref="Start"/>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;

            StartConsuming();

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(50);
            }

            logger.LogInformation($"{nameof(StartAsync)}: Cancellation requested.");

            Stop();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// The consumer of messages runs on a thread of its own.
        /// All messages from all subscribed queues will be passed to the handler 
        /// that will run in this one thread too.
        /// <para></para>
        /// <inheritdoc cref="RabbitMQSubscription"/>
        /// </remarks>
        public void Start() => StartConsuming();

        private void StartConsuming()
        {
            logger.LogInformation($"{nameof(StartConsuming)}: Subscription starting ...");

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

            lock (consumersToQueues)
            {

                foreach (var queue in queues)
                {
                    var tag = channel.BasicConsume(queue: queue, autoAck: false, consumer: consumer);
                    consumersToQueues.Add(tag, queue);

                    logger.LogInformation($"{nameof(StartConsuming)}: Consumer {tag} of queue " +
                        "{Queue} started", queue);
                }
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Stop operation will wait until current message being handled finishes, if any.
        /// Then it will ignore all other messages already sent 
        /// (<see cref="RabbitMQConfiguration.PerQueuePrefetchCount"/>) which will be 
        /// automatically requeued by the broker.
        /// <para></para>
        /// Internal channel remains opened to allow for a new start.
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

            // This lock prevents from stopping when there is a message being handled.
            // When handling finishes and releases the lock, this process grabs it 
            // preventing other messages from entering the handling chain.
            // Without this lock management, the stop operation would execute and 
            // return execution to the caller that would then dispose this subscription
            // instance, causing failure on the ack/nack operations of still handling 
            // message.
            lock (consumersToQueues)
            {
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
        }

        private void MessageReceived(object sender, BasicDeliverEventArgs ea)
        {
            logger.LogDebug($"{nameof(MessageReceived)}: Message " +
                "{MessageId} received by " + $"{ea.ConsumerTag} " +
                $"with DeliveryTag {ea.DeliveryTag}", ea.BasicProperties.MessageId);

            lock (consumersToQueues)
            {
                if (consumersToQueues.TryGetValue(ea.ConsumerTag, out string queue))
                {
                    MessageHandling(queue, ea);
                }
                else
                {
                    logger.LogDebug($"{nameof(MessageReceived)}: Message " +
                        "{MessageId} sent to " + $"{ea.ConsumerTag} " +
                        $"with DeliveryTag {ea.DeliveryTag} was returned because " +
                        $"subscription was stopped", ea.BasicProperties.MessageId);
                }
            }
        }

        private void MessageHandling(string queue, BasicDeliverEventArgs ea)
        {
            var message = ea.Body.ToArray();
            var messageContext = new RabbitMQMessageContext(ea, queue, cancellationToken);

            try
            {
                logger.LogInformation($"{nameof(MessageHandling)}: Message " +
                    "{MessageId} received from queue {Queue} " +
                    $"with DeliveryTag {ea.DeliveryTag}", messageContext.Id, queue);

                handler.Invoke(message, messageContext);

                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

                logger.LogInformation($"{nameof(MessageHandling)}: Message " +
                    "{MessageId} handle succeeded!", messageContext.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"{nameof(MessageHandling)}: Message " +
                    "{MessageId} handle failed!", messageContext.Id);

                var requeue = false;

                if (configuration.MessageHandlerErrorBehavior ==
                    MessageHandlerErrorBehavior.RejectOnRedelivery && !ea.Redelivered)
                {
                    requeue = true;
                }

                channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: requeue);

                logger.LogWarning($"{nameof(MessageReceived)}: Message " +
                    "{MessageId} rejected with requeue set to {Requeue}",
                    messageContext.Id, requeue);
            }
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
                logger.LogDebug($"{nameof(RabbitMQSubscription)}: Disposing ...");

                if (channel.IsOpen)
                {
                    channel.Close();
                }

                channel.Dispose();

                messaging.RemoveSubscription(this);

                logger.LogDebug($"{nameof(RabbitMQSubscription)}: Disposed.");
            }

            disposed = true;
        }
    }
}