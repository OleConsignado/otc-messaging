using Microsoft.Extensions.DependencyInjection;
using Otc.Messaging.RabbitMQ.Cli.Exceptions;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net;
using System.Threading.Tasks;

namespace Otc.Messaging.RabbitMQ.Cli.Commands
{
    public static class CreateVirtualHost
    {
        private static readonly string DefaultErrorMessage = "Error creating virtual host. ";

        public static Command Setup()
        {
            var cmd = new Command(nameof(CreateVirtualHost));

            cmd.Handler = CommandHandler.Create(Execute);

            return cmd;
        }

        public static async Task Execute()
        {
            try
            {
                Console.WriteLine("Executing...");

                var name = Broker.VirtualHost;
                var provider = ServiceProvider.GetInstance();
                var api = provider.GetService<IRabbitMQApi>();

                var response = await api.GetVHost(name);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    throw new CliException(
                        $"VirtualHost {name} already exists.");
                }

                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    throw new CliException(DefaultErrorMessage +
                        $"{response.StatusCode} - {response.ReasonPhrase}.");
                }

                response = await api.PutVHost(name);
                if (response.StatusCode != HttpStatusCode.Created)
                {
                    throw new CliException(DefaultErrorMessage +
                        $"{response.StatusCode} - {response.ReasonPhrase}.");
                }

                Console.WriteLine($"VirtualHost {name} created successfully!");

                Console.WriteLine($"Applying default mirror policy");
                await ApplyMirrorPolicy.Execute("default-mirror-policy", ".*");
            }
            catch (Exception e)
            {
                ExceptionHandler.Handle(e);
            }
        }
    }
}
