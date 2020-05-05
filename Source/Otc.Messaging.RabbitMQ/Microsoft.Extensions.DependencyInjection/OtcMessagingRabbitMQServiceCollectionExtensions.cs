using Otc.Messaging.Abstractions;
using Otc.Messaging.RabbitMQ;
using Otc.Messaging.RabbitMQ.Configurations;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OtcMessagingRabbitMQServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMQ(this IServiceCollection services,
            RabbitMQConfiguration configuration)
        {
            services.AddSingleton(configuration);
            services.AddSingleton<IMessaging, RabbitMQMessaging>();

            return services;
        }
    }
}