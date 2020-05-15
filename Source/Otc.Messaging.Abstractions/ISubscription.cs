using System;
using System.Threading;
using System.Threading.Tasks;

namespace Otc.Messaging.Abstractions
{
    /// <summary>
    /// Subscription for consuming messages from queues.
    /// </summary>
    public interface ISubscription : IDisposable
    {
        /// <summary>
        /// Starts consuming messages from all subscribed queues.
        /// The cancellation token must be used to stop consuming.
        /// </summary>
        /// <param name="cancellationToken">Token for async cancellation.</param>
        /// <returns></returns>
        Task StartAsync(CancellationToken cancellationToken);

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