using System;

namespace Otc.Messaging.Abstractions
{
    /// <summary>
    /// Publishes messages to a topic.
    /// </summary>
    public interface IPublisher : IDisposable
    {
        /// <summary>
        /// Publishes a message to a specified topic.
        /// </summary>
        /// <param name="topic">The existing topic to publish to.</param>
        /// <param name="message">The message content to be published.</param>
        /// <param name="queue">Routes this message to a specific queue.
        /// Normally omitted, since brokers are responsible for 
        /// routing messages to queues.</param>
        /// <param name="messageId">The application generated Message Id.
        /// Normally omitted, since publishers generate them when not provided.</param>
        /// <exception cref="PublishException">
        /// Thrown if broker do not send confirmation of message received, which may be due
        /// to timeout, topic does not exist or communication problems. InnerException will 
        /// provide details.
        /// </exception>
        /// <exception cref="MissingRouteException">
        /// Thrown if broker receives and ackowledges the message but do not find a route 
        /// to a queue for that message.
        /// </exception>
        void Publish(string topic, byte[] message, string queue = null, string messageId = null);
    }
}
