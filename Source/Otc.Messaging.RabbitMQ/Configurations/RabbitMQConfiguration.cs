using Otc.ComponentModel.DataAnnotations;
using Otc.Messaging.Abstractions.Exceptions;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Otc.Messaging.RabbitMQ.Configurations
{
    /// <summary>
    /// Holds the configurations used by this client.
    /// </summary>
    public class RabbitMQConfiguration
    {
        /// <summary>
        /// List of hosts names or ip addresses to connect to.
        /// Enter multiple options if you have a cluster configured.
        /// </summary>
        [Required]
        public IList<string> Hosts { set; get; }
        
        /// <summary>
        /// Port number to connect to.
        /// </summary>
        /// <remarks>
        /// RabbitMQ.Client uses a conventional value of -1 to indicate that this connection 
        /// will use protocol default ports, which are 5672 for AMQP and 5671 for AMQPS (TLS/SSL)
        /// Set a different number if your broker customises this values
        /// </remarks>
        public int Port { set; get; } = AmqpTcpEndpoint.UseDefaultPort;

        /// <summary>
        /// Virtual host to connect to.
        /// </summary>
        public string VirtualHost { set; get; } = ConnectionFactory.DefaultVHost;

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
        /// Client provided name to be used for connection identification.
        /// </summary>
        public string ClientProvidedName { get; set; } = $"otc-messaging";

        /// <summary>
        /// Timeout in milliseconds. Throws an exception if confirmation wait time is exceded or
        /// if broker sends back an nack. Default is 15 seconds.
        /// </summary>
        public uint PublishConfirmationTimeoutMilliseconds { get; set; } = 15000;

        /// <summary>
        /// Subscription can consume one or more queues, this sets the number of messages
        /// prefetched per queue. Default is 1.
        /// </summary>
        public ushort PerQueuePrefetchCount { get; set; } = 1;

        /// <summary>
        /// Define action when a MessageHandler throws an exception.
        /// </summary>
        /// <remarks>
        /// It's highly recommended that your queue have a good dlx configuration defined.
        /// </remarks>
        public MessageHandlerErrorBehavior MessageHandlerErrorBehavior { get; set; } =
            MessageHandlerErrorBehavior.RejectOnRedelivery;

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
        /// <param name="name">Topology name loaded in <see cref="Topologies"/>.</param>
        /// <param name="channel">The channel opened by the <see cref="RabbitMQMessaging"/>
        /// for this purpose.</param>
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
        internal void EnsureTopology(string name, IModel channel)
        {
            try
            {
                var topology = Topologies[name];

                foreach (var e in topology.Exchanges)
                {
                    channel.ExchangeDeclare(exchange: e.Name, type: e.Type, durable: e.Durable,
                        autoDelete: e.AutoDelete, arguments: e.Arguments);

                    foreach (var q in e.Queues)
                    {
                        q.Arguments = ArgumentsConverter(q.Arguments);

                        channel.QueueDeclare(queue: q.Name, durable: q.Durable, exclusive: q.Exclusive,
                            autoDelete: q.AutoDelete, arguments: q.Arguments);

                        channel.QueueBind(queue: q.Name, exchange: e.Name, routingKey: q.RoutingKey);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new EnsureTopologyException(name, ex);
            }
        }

        internal string Hostnames
        {
            get
            {
                return string.Join(", ", Hosts.ToArray());
            }
        }

        private IDictionary<string, object> ArgumentsConverter(
            IDictionary<string, object> arguments)
        {
            if (arguments == null)
            {
                return arguments;
            }

            try
            {
                if (arguments.ContainsKey("x-message-ttl"))
                {
                    arguments["x-message-ttl"] = Convert.ToInt32(arguments["x-message-ttl"]);
                }

                return arguments;
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException)
            {
                throw new ArgumentException("Could not convert to appropriate type, see " +
                    "innerException.", nameof(arguments), ex);
            }
        }
    }
}