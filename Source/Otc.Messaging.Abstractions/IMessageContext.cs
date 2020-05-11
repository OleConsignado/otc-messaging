using System;

namespace Otc.Messaging.Abstractions
{
    /// <summary>
    /// Context metadata of a message received by a consumer.
    /// </summary>
    public interface IMessageContext
    {
        /// <summary>
        /// Application generated Message Id.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Application generated Unix timestamp in milliseconds.
        /// </summary>
        DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Topic this message was published to.
        /// </summary>
        string Topic { get; }

        /// <summary>
        /// Queue this message was received from.
        /// </summary>
        string Queue { get; }

        /// <summary>
        /// If true indicates this message was already sent
        /// to a consumer before and was not acknowledged.
        /// </summary>
        bool Redelivered { get; }
    }
}