using Otc.Messaging.RabbitMQ.Configurations;
using System;
using System.Collections.Generic;

namespace Otc.Messaging.RabbitMQ.PredefinedTopologies
{
    public static class RabbitMQConfigurationExtensions
    {
        /// <summary>
        /// Add topology created by a specific <see cref="ITopologyFactory"/> implementation.
        /// </summary>
        /// <typeparam name="TTopologyFactory">An <see cref="ITopologyFactory"/> implementation.</typeparam>
        /// <param name="rabbitMQconfiguration">The <see cref="RabbitMQConfiguration"/> object.</param>
        /// <param name="mainExchangeName">The exchange name.</param>
        /// <param name="args">
        ///     Custom topology args (see the specific <see cref="ITopologyFactory"/> 
        ///     implementation for details).</param>
        /// <returns>The provided <see cref="RabbitMQConfiguration"/> object.</returns>
        public static RabbitMQConfiguration AddTopology<TTopologyFactory>(this RabbitMQConfiguration rabbitMQconfiguration,
            string mainExchangeName, params object[] args)
            where TTopologyFactory : ITopologyFactory, new()
        {
            if (rabbitMQconfiguration is null)
            {
                throw new ArgumentNullException(nameof(rabbitMQconfiguration));
            }

            if (mainExchangeName is null)
            {
                throw new ArgumentNullException(nameof(mainExchangeName));
            }

            var topologyFactory = new TTopologyFactory();

            if (rabbitMQconfiguration.Topologies is null)
            {
                rabbitMQconfiguration.Topologies = new Dictionary<string, Topology>();
            }

            rabbitMQconfiguration.Topologies.Add(mainExchangeName, topologyFactory.Create(mainExchangeName, args));

            return rabbitMQconfiguration;
        }
    }
}
