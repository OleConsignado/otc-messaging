using Otc.Messaging.Abstractions;
using Otc.Messaging.RabbitMQ;
using Otc.Messaging.RabbitMQ.Configurations;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OtcMessagingRabbitMQServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMQ(this IServiceCollection services,
            RabbitMQConfiguration configuration)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.AddSingleton(configuration);
            services.AddScoped<IMessaging, RabbitMQMessaging>();

            return services;
        }
    }
}
