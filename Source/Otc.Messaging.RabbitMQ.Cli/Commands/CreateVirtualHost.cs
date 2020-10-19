using Microsoft.Extensions.DependencyInjection;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Otc.Messaging.RabbitMQ.Cli.Commands
{
    public static class CreateVirtualHost
    {
        public static Command Setup()
        {
            var cmd = new Command(nameof(CreateVirtualHost));

            var option = new Option<string>(new[] { "--name", "-n" });
            option.Argument.Arity = ArgumentArity.ExactlyOne;
            option.Description = "Name of VirtualHost to be created.";
            cmd.AddOption(option);
            cmd.Handler = CommandHandler.Create<string>(Execute);

            return cmd;
        }

        public static async Task Execute(string name)
        {
            try
            {
                Console.WriteLine("Executing...");

                var provider = ServiceProvider.GetInstance();
                var api = provider.GetService<IRabbitMQApi>();

                var response = await api.GetVHost(name);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    throw new InvalidOperationException(
                        $"VirtualHost {name} already exists.");
                }

                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    throw new HttpRequestException($"Response status not expected: " +
                        $"{response.StatusCode} - {response.ReasonPhrase}.");
                }

                response = await api.PutVHost(name);
                if (response.StatusCode != HttpStatusCode.Created)
                {
                    throw new HttpRequestException($"Response status not expected: " +
                        $"{response.StatusCode} - {response.ReasonPhrase}.");
                }

                Console.WriteLine($"VirtualHost {name} created!");
            }
            catch (Exception e)
            {
                ExceptionHandler.Handle(e);
            }
        }
    }
}
