using Otc.Messaging.RabbitMQ.Configurations;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Otc.Messaging.RabbitMQ.PredefinedTopologies
{
    /// <summary>
    /// Creates a new queue with a pack of exchanges and queues implementing 
    /// [(wait -> retry) -> deadletter] sequence and then associates this queue
    /// with a given exchage.
    /// </summary>
    internal class QueueRetryPackBuilder
    {
        /// <summary>
        /// Create topology
        /// </summary>
        /// <param name="exchange">
        ///     The exchange to add main queue with retries.
        /// </param>
        /// <param name="queueName">
        ///     The main queue name.
        /// </param>
        /// <param name="args">
        ///     Wait time in millisesonds for each retry level. 
        ///     The number of retry levels is the args array length.</param>
        /// <returns>List of exchanges and queues used for retries</returns>
        public IEnumerable<Exchange> Create(
            Exchange exchange, string queueName, params object[] args)
        {
            if (exchange is null)
            {
                throw new ArgumentNullException(nameof(exchange));
            }

            if (queueName is null)
            {
                throw new ArgumentNullException(nameof(queueName));
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

            return BuildRetries(exchange, queueName, retryDelaysMilliseconds);
        }

        private IEnumerable<Exchange> BuildRetries(
            Exchange exchange, string queueName, int[] retryWaitsMilliseconds)
        {
            if (retryWaitsMilliseconds is null)
            {
                throw new ArgumentNullException(nameof(retryWaitsMilliseconds));
            }

            var exchanges = new List<Exchange>();

            if (retryWaitsMilliseconds.Length > 0)
            {
                int i = 0;

                exchange.Queues.Add(
                    BuildQueue(queueName, $"{queueName}-wait-{i}"));

                for (; i < retryWaitsMilliseconds.Length; i++)
                {
                    exchanges.Add(
                        BuildExchangeAndQueue(
                            $"{queueName}-wait-{i}",
                            $"{queueName}-retry-{i}",
                            retryWaitsMilliseconds[i]));

                    if (i + 1 < retryWaitsMilliseconds.Length)
                    {
                        exchanges.Add(
                            BuildExchangeAndQueue(
                                $"{queueName}-retry-{i}",
                                $"{queueName}-wait-{i + 1}"));
                    }
                }

                exchanges.Add(
                    BuildExchangeAndQueue(
                        $"{queueName}-retry-{i - 1}",
                        $"{queueName}-dead"));
            }
            else
            {
                // If the retryWaitsMilliseconds is empty
                // there will be no wait/retry exchanges, 
                // failed messages goes direct to deadletter exchange.
                exchange.Queues.Add(
                    BuildQueue(queueName, $"{queueName}-dead"));

            }

            exchanges.Add(
                BuildExchangeAndQueue($"{queueName}-dead"));

            return exchanges;
        }

        private Exchange BuildExchangeAndQueue(
            string name, string dlxName = null, int? messageTtlMilliseconds = null)
        {
            return new Exchange()
            {
                Type = ExchangeType.Direct,
                Name = name,
                Queues = new Queue[]
                {
                    BuildQueue(name, dlxName, messageTtlMilliseconds)
                }
            };
        }

        private Queue BuildQueue(
            string name, string dlxName = null, int? messageTtlMilliseconds = null)
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

            return new Queue()
            {
                Name = name,
                Arguments = queueArguments
            };
        }
    }
}
