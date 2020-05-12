using Otc.Messaging.Abstractions;
using Otc.Messaging.Typed;
using Otc.Messaging.Typed.Abstractions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OtcTypedMessagingServiceCollectionExtensions
    {
        public static IServiceCollection AddTypedMessaging(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddTypedMessaging(new DefaultSerializer());

            return services;
        }

        public static IServiceCollection AddTypedMessaging(
            this IServiceCollection services, ISerializer serializer)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            MessagingExtensions.Serializer = serializer;

            return services;
        }
    }
}