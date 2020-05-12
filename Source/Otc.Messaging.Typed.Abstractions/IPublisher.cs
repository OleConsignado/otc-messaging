using Otc.Messaging.Abstractions;
using System;

namespace Otc.Messaging.Typed.Abstractions
{
    /// <inheritdoc cref="IPublisher"/>
    public interface IPublisher<in T> : IDisposable
    {
        /// <inheritdoc cref="IPublisher.Publish(byte[], string, string, string)"/>
        void Publish(T message, string topic, string queue = null, string messageId = null);
    }
}
