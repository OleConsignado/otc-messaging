using Otc.Messaging.RabbitMQ.Configurations;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Otc.Messaging.RabbitMQ.PredefinedTopologies
{
    /// <summary>
    /// Create wait -> retry -> deadletter topology
    /// </summary>
    public class MultipleQueuesWithRetryTopologyFactory : ITopologyFactory
    {
        /// <summary>
        /// Create topology
        /// </summary>
        /// <param name="mainExchangeName">
        ///     The main exchange name.
        /// </param>
        /// <param name="args">
        ///     Names of queues (string) to send/funout published messages.
        ///     Wait times in millisesonds (int) for each retry level.
        ///     The number of retry levels is the number of int args.
        /// </param>
        /// <returns>The created topology</returns>
        public Topology Create(string mainExchangeName, params object[] args)
        {
            if (mainExchangeName is null)
            {
                throw new ArgumentNullException(nameof(mainExchangeName));
            }

            var (queueNames, retryDelaysMilliseconds) = ReadArguments(args);

            var exchange = new Exchange()
            {
                Name = mainExchangeName,
                Type = ExchangeType.Fanout
            };

            var exchanges = new List<Exchange>
            {
                exchange
            };

            var queueRetryPackBuilder = new QueueRetryPackBuilder();

            foreach (var queue in queueNames)
            {
                exchanges.AddRange(
                    queueRetryPackBuilder.Create(exchange, queue,
                    retryDelaysMilliseconds.ToArray()));
            }

            return new Topology()
            {
                Exchanges = exchanges
            };
        }

        private (IEnumerable<string>, IEnumerable<object>) ReadArguments(
            params object[] args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var queueNames = new List<string>();
            var retryDelaysMilliseconds = new List<object>();

            foreach (var item in args)
            {
                if (item is string queueName)
                {
                    queueNames.Add(queueName);
                }
                else if (item is int delay)
                {
                    retryDelaysMilliseconds.Add(delay);
                }
                else
                {
                    throw new ArgumentException(
                        "Argument must be of type string or int.", nameof(args));
                }
            }

            if (queueNames.Count < 2)
            {
                throw new ArgumentException(
                    "A minimum of 2 queues must be provided " +
                    "to use this predefined topology.", nameof(args));
            }

            return (queueNames, retryDelaysMilliseconds);
        }
    }
}
