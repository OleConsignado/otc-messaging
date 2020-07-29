using Otc.Messaging.RabbitMQ.Configurations;
using System;
using System.Collections.Generic;

namespace Otc.Messaging.RabbitMQ.PredefinedTopologies
{
    /// <summary>
    /// Create wait -> retry -> deadletter topology
    /// </summary>
    public class SimpleQueueWithRetryTopologyFactory : ITopologyFactory
    {
        /// <summary>
        /// Create topology
        /// </summary>
        /// <param name="mainExchangeName">
        ///     The main exchange name.
        /// </param>
        /// <param name="args">
        ///     Wait time in millisesonds for each retry level. 
        ///     The number of retry levels is the args array length.</param>
        /// <returns>The created topology</returns>
        public Topology Create(string mainExchangeName, params object[] args)
        {
            if (mainExchangeName is null)
            {
                throw new ArgumentNullException(nameof(mainExchangeName));
            }

            var exchange = new Exchange()
            {
                Name = mainExchangeName
            };

            var exchanges = new List<Exchange>
            {
                exchange
            };

            var queueRetryPackBuilder = new QueueRetryPackBuilder();

            exchanges.AddRange(
                queueRetryPackBuilder.Create(exchange, mainExchangeName, args));

            return new Topology()
            {
                Exchanges = exchanges
            };
        }
    }
}
