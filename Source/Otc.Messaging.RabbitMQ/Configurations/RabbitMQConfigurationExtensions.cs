using System;
using System.Collections.Generic;

namespace Otc.Messaging.RabbitMQ.Configurations
{
    internal static class RabbitMQConfigurationExtensions
    {
        /// <summary>
        /// Converts datatypes of queues' arguments for use within
        /// <see cref="RabbitMQConfiguration"/>. Only types that are not strings need conversion.
        /// </summary>
        /// <remarks>
        /// Not a attempt to map all possible arguments, just a subset as necessity arrives.
        /// </remarks>
        /// <param name="configuration">Configuration being extended.</param>
        /// <param name="arguments">Dictionary of queue's arguments for type conversion.</param>
        /// <returns>Dictionary of arguments with conversions applied.</returns>
        public static IDictionary<string, object> QueueArgumentsConverter(
            this RabbitMQConfiguration configuration,
            IDictionary<string, object> arguments)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (arguments == null)
            {
                return arguments;
            }

            try
            {
                if (arguments.ContainsKey("x-message-ttl"))
                {
                    arguments["x-message-ttl"] = Convert.ToInt32(arguments["x-message-ttl"]);
                }

                return arguments;
            }
            catch (Exception e) when (e is FormatException || e is OverflowException)
            {
                throw new ArgumentException("Could not convert to appropriate type, see " +
                    "innerException.", nameof(arguments), e);
            }
        }
    }
}