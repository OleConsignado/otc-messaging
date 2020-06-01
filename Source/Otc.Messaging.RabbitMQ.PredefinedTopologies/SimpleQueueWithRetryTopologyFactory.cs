using Otc.Messaging.RabbitMQ.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;

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

            int[] retryDelaysMilliseconds;

            try
            {
                retryDelaysMilliseconds = args.Select(x => (int)x).ToArray();
            }
            catch(InvalidCastException e)
            {
                throw new ArgumentException("Every arg must be of type int.", nameof(args), e);
            }

            return BuildTopology(mainExchangeName, retryDelaysMilliseconds);
        }

        private Topology BuildTopology(string baseName, int[] retryWaitsMilliseconds)
        {
            if (baseName is null)
            {
                throw new ArgumentNullException(nameof(baseName));
            }

            if (retryWaitsMilliseconds is null)
            {
                throw new ArgumentNullException(nameof(retryWaitsMilliseconds));
            }

            var exchanges = new List<Exchange>();

            if (retryWaitsMilliseconds.Length > 0)
            {
                int i = 0;

                exchanges.Add(
                    BuildExchange(baseName, $"{baseName}-wait-{i}"));

                for (; i < retryWaitsMilliseconds.Length; i++)
                {
                    exchanges.Add(
                        BuildExchange(
                            $"{baseName}-wait-{i}",
                            $"{baseName}-retry-{i}",
                            retryWaitsMilliseconds[i]));

                    if (i + 1 < retryWaitsMilliseconds.Length)
                    {
                        exchanges.Add(
                            BuildExchange(
                                $"{baseName}-retry-{i}",
                                $"{baseName}-wait-{i + 1}"));
                    }
                }

                exchanges.Add(
                    BuildExchange(
                        $"{baseName}-retry-{i - 1}",
                        $"{baseName}-dead"));
            }
            else
            {
                // If the retryWaitsMilliseconds is empty
                // there will be no wait/retry exchanges, 
                // failed messages goes direct to deadletter exchange.
                exchanges.Add(
                    BuildExchange(baseName, $"{baseName}-dead"));

            }

            exchanges.Add(
                BuildExchange($"{baseName}-dead"));

            return new Topology()
            {
                Exchanges = exchanges
            };
        }

        private Exchange BuildExchange(string name, string dlxName = null, int? messageTtlMilliseconds = null)
        {
            var queueArguments = new Dictionary<string, object>();

            if (dlxName != null)
            {
                queueArguments.Add("x-dead-letter-exchange", dlxName);
            }

            if (messageTtlMilliseconds.HasValue)
            {
                queueArguments.Add("x-message-ttl", messageTtlMilliseconds.Value);
            }

            return new Exchange()
            {
                Type = "direct",
                Name = name,
                Queues = new Queue[]
                {
                    new Queue()
                    {
                        Name = name,
                        Arguments = queueArguments
                    }
                }
            };
        }
    }
}
