using RabbitMQ.Client;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Otc.Messaging.RabbitMQ.Configurations
{
    /// <summary>
    /// Holds the configurations used by this client.
    /// </summary>
    public class RabbitMQConfiguration
    {
        /// <summary>
        /// Host name or ip address to connect to.
        /// </summary>
        [Required]
        public string Host { set; get; }

        /// <summary>
        /// Port number to connect to.
        /// </summary>
        [Required]
        public int Port { set; get; }

        /// <summary>
        /// User for connection.
        /// </summary>
        [Required]
        public string User { set; get; }

        /// <summary>
        /// User's password for connection.
        /// </summary>
        [Required]
        public string Password { set; get; }

        /// <summary>
        /// Timeout in milliseconds. Throws an exception if confirmation wait time is exceded or
        /// if broker sends back an nack. Default is 15 seconds.
        /// </summary>
        [Required]
        public uint PublishConfirmationTimeout { get; set; } = 15000;

        /// <summary>
        /// Subscription can consume one or more queues, this sets the number of messages
        /// prefetched per queue. Default is 1.
        /// </summary>
        [Required]
        public byte PerQueuePrefetchCount { get; set; } = 1;

        /// <summary>
        /// Define action when a MessageHandler throws an exception.
        /// </summary>
        /// <remarks>
        /// I's highly recommended that your queue have a good dlx configuration defined.
        /// </remarks>
        [Required]
        public OnMessageHandlerError OnMessageHandlerError { get; set; } =
            OnMessageHandlerError.RejectOnRedelivery;

        /// <summary>
        /// List of Topologies used by this client.
        /// </summary>
        /// <remarks>
        /// Topology is a set of exchanges, queues and its bindings that can be applyied
        /// using <see cref="EnsureTopology(string, IModel)"/> if user has permissions.
        /// </remarks>
        public IDictionary<string, Topology> Topologies { get; set; }

        /// <summary>
        /// Applies a given topology to the broker.
        /// </summary>
        /// <param name="name">One topology name loaded in <see cref="Topologies"/>.</param>
        /// <param name="channel">The channel opened by the <see cref="RabbitMQMessaging"/>
        /// for this purpose.</param>
        public void EnsureTopology(string name, IModel channel)
        {
            var topology = Topologies[name];

            foreach (var e in topology.Exchanges)
            {
                channel.ExchangeDeclare(exchange: e.Name, type: e.Type, durable: e.Durable,
                    autoDelete: e.AutoDelete, arguments: e.Arguments);

                foreach (var q in e.Queues)
                {
                    q.Arguments = this.QueueArgumentsConverter(q.Arguments);

                    channel.QueueDeclare(queue: q.Name, durable: q.Durable, exclusive: q.Exclusive,
                        autoDelete: q.AutoDelete, arguments: q.Arguments);

                    channel.QueueBind(queue: q.Name, exchange: e.Name, routingKey: q.RoutingKey);
                }
            }
        }
    }

    /// <summary>
    /// Sets the <see cref="RabbitMQSubscription"/> behavior when message handling throws 
    /// an exception.
    /// </summary>
    public enum OnMessageHandlerError
    {
        RejectOnFistDelivery = 0,
        RejectOnRedelivery = 1,
    }
}