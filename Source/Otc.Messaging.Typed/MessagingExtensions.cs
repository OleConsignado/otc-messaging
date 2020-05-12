using Otc.Messaging.Typed;
using Otc.Messaging.Typed.Abstractions;
using System;

namespace Otc.Messaging.Abstractions
{
    /// <summary>
    /// Extends IMessaging to allow strong typed message objects
    /// </summary>
    public static class MessagingExtensions
    {
        /// <summary>
        /// Singleton instance of <see cref="ISerializer"/> used for message serialization
        /// and deserialization
        /// </summary>
        internal static ISerializer Serializer { get; set; }

        /// <summary>
        /// Creates a publisher for messages of type T
        /// </summary>
        /// <typeparam name="T">The type of message object.</typeparam>
        /// <param name="messaging">The Messaging instance being extended.</param>
        /// <returns><see cref="Publisher{T}"/> default implementation of 
        /// <see cref="IPublisher{T}"/></returns>
        public static IPublisher<T> CreatePublisher<T>(this IMessaging messaging)
        {
            return new Publisher<T>(messaging, Serializer);
        }

        /// <summary>
        /// Creates a subscription for messages of type T
        /// </summary>
        /// <typeparam name="T">The type of message object.</typeparam>
        /// <param name="messaging">The Messaging instance being extendend.</param>
        /// <param name="handler">The message handler.</param>
        /// <param name="queues">The queues to be consumed.</param>
        /// <returns><see cref="Subscription{T}"/> default implementation of 
        /// <see cref="ISubscription{T}"/></returns>
        public static ISubscription<T> Subscribe<T>(this IMessaging messaging,
            Action<T, IMessageContext> handler, params string[] queues)
        {
            return new Subscription<T>(messaging, Serializer, handler, queues);
        }
    }
}