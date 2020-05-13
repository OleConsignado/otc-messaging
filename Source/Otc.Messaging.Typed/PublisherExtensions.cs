using Otc.Messaging.Abstractions;

namespace Otc.Messaging.Typed
{
    public static class PublisherExtensions
    {
        /// <inheritdoc cref="IPublisher"/>
        /// <typeparam name="T">The message object type</typeparam>
        public static void Publish<T>(this IPublisher publisher, T message, string topic, string queue = null, string messageId = null)
        {
            var messageBytes = MessagingExtensions.Serializer.Serialize(message);
            publisher.Publish(messageBytes, topic, queue, messageId);
        }
    }
}
