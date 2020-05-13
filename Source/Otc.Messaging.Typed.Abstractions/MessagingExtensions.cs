using Otc.Messaging.Typed.Abstractions;
using System;

namespace Otc.Messaging.Abstractions
{
    /// <summary>
    /// Extends IMessaging to allow strong typed message objects
    /// </summary>
    public static class MessagingExtensions
    {
        public static ISerializer Serializer { get; set; }

        /// <summary>
        /// Creates a subscription for messages of type T
        /// </summary>
        /// <typeparam name="T">The type of message object.</typeparam>
        /// <param name="messaging">The Messaging instance being extendend.</param>
        /// <param name="handler">The message handler.</param>
        /// <param name="queues">The queues to be consumed.</param>
        /// <returns><see cref="ISubscription"/></returns>
        public static ISubscription Subscribe<T>(this IMessaging messaging,
            Action<T, IMessageContext> handler, params string[] queues)
        {
            return messaging.Subscribe((message, messageContext) =>
            {
                var typedMessage = Serializer.Deserialize<T>(message);
                handler.Invoke(typedMessage, messageContext);
            }, queues);
        }
    }
}