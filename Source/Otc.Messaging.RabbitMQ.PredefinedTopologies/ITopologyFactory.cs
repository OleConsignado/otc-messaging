using Otc.Messaging.RabbitMQ.Configurations;
using System.Collections.Generic;

namespace Otc.Messaging.RabbitMQ.PredefinedTopologies
{
    public interface ITopologyFactory
    {
        /// <summary>
        /// Create topology (see specific implementation for details).
        /// </summary>
        /// <param name="mainExchangeName">The exchange name.</param>
        /// <param name="args">Custom topology args (see specific implementation for details).</param>
        /// <returns>The created topology</returns>
        Topology Create(string mainExchangeName, params object[] args);
    }
}
