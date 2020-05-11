using System;

namespace Otc.Messaging.Abstractions
{
    /// <summary>
    /// Creates <see cref="IPublisher"/> and <see cref="ISubscription"/>.
    /// Allow ensuring a given topology of topics and queues.
    /// </summary>
    public interface IMessaging : IDisposable
    {
        /// <summary>
        /// Creates a new instance of <see cref="IPublisher"/>.
        /// </summary>
        /// <returns>Instance of <see cref="IPublisher"/> ready to publish messages.</returns>
        IPublisher CreatePublisher();

        /// <summary>
        /// Creates a new instance of <see cref="ISubscription"/>.
        /// </summary>
        /// <param name="handler">The delegate to be invoked when a new message arrives.</param>
        /// <param name="queues">The list of queues to consume from.</param>
        /// <returns>Instance of <see cref="ISubscription"/> ready to start
        /// consuming messages.</returns>
        ISubscription Subscribe(Action<byte[], IMessageContext> handler, params string[] queues);

        /// <summary>
        /// Ensures that a given topology is configure by the broker.
        /// </summary>
        /// <param name="name">The topology name to look for.</param>
        /// <exception cref="EnsureTopologyException">
        /// Thrown if any error of configuration, connection or permissions occurs while
        /// applying given topology.
        /// </exception>
        void EnsureTopology(string name);
    }
}