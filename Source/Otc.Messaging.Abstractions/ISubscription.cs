using System;

namespace Otc.Messaging.Abstractions
{
    /// <summary>
    /// Subscription for consuming messages from queues.
    /// </summary>
    public interface ISubscription : IDisposable
    {
        /// <summary>
        /// Starts consuming messages from all subscribed queues.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops consuming all subscribed queues.
        /// </summary>
        void Stop();
    }
}