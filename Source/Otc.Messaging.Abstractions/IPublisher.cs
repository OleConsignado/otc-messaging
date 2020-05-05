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
        void Publish(string topic, byte[] message, string queue = null, string messageId = null);
    }
}
