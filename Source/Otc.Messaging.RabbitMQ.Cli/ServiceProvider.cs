using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refit;
using System;
using System.Net.Http.Headers;
using System.Text;

namespace Otc.Messaging.RabbitMQ.Cli
{
    public static class ServiceProvider
    {
        private static IServiceProvider provider;

        public static IServiceProvider GetInstance()
        {
            if (provider == null)
            {
                var services = new ServiceCollection();

                var credentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{Broker.User}:{Broker.Password}"));

                services.AddRefitClient<IRabbitMQApi>()
                    .ConfigureHttpClient(c =>
                    {
                        c.BaseAddress = new Uri(Broker.ApiBaseUrl);
                        c.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Basic", credentials);
                    });

                services
                    .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
                    .AddRabbitMQ(Broker.Configuration);

                provider = services.BuildServiceProvider();
            }

            return provider;
        }
    }
}
