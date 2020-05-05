using Otc.Messaging.Abstractions;
using RabbitMQ.Client.Events;

namespace Otc.Messaging.RabbitMQ
{
    /// <summary>
    /// Provides a message received from broker.
    /// It is constructed right after a message arrives and is passed 
    /// to the registered handler. All properties are readonly.
    /// </summary>
    public class RabbitMQMessage : IMessage
    {
        /// <summary>
        /// Creates a new <see cref="RabbitMQMessage"/>.
        /// </summary>
        /// <param name="ea">Message's metadata sent by the broker.</param>
        /// <param name="queue">The queue it came from.</param>
        public RabbitMQMessage(BasicDeliverEventArgs ea, string queue)
        {
            Id = ea.BasicProperties.MessageId;
            Timestamp = ea.BasicProperties.Timestamp.UnixTime;
            Topic = ea.Exchange;
            Queue = queue;
            Redelivered = ea.Redelivered;
            Body = ea.Body.ToArray();
        }

        /// <inheritdoc/>
        public string Id { get; }

        /// <inheritdoc/>
        public long Timestamp { get; }

        /// <inheritdoc/>
        public string Topic { get; }

        /// <inheritdoc/>
        public string Queue { get; }

        /// <inheritdoc/>
        public bool Redelivered { get; }

        /// <inheritdoc/>
        public byte[] Body { get; }
    }
}
