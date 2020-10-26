using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Otc.Messaging.RabbitMQ.Cli.Exceptions;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Otc.Messaging.RabbitMQ.Cli.Commands
{
    public static class ApplyMirrorPolicy
    {
        private static readonly string DefaultErrorMessage = "Error applying mirror policy. ";

        public static Command Setup()
        {
            var cmd = new Command(nameof(ApplyMirrorPolicy));

            var option = new Option<string>(new[] { "--name", "-n" });
            option.Argument.Arity = ArgumentArity.ExactlyOne;
            option.Description = "Name of Policy to be created.";
            cmd.AddOption(option);

            option = new Option<string>(new[] { "--pattern" }, () => ".*");
            option.Argument.Arity = ArgumentArity.ZeroOrOne;
            option.Description = "Pattern matching queues names.";
            cmd.AddOption(option);

            cmd.Handler = CommandHandler.Create<string, string>(Execute);

            return cmd;
        }

        public static async Task Execute(string name, string pattern)
        {
            try
            {
                Console.WriteLine("Executing...");

                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException($"Option --{nameof(name)} is required.");
                }

                if (string.IsNullOrEmpty(pattern))
                {
                    throw new ArgumentException($"Option --{nameof(pattern)} is required.");
                }

                var provider = ServiceProvider.GetInstance();
                var api = provider.GetService<IRabbitMQApi>();

                var body = await PrepareBody(api, pattern);
                var response = await api.PutPolicy(Broker.VirtualHost, name, body);
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new CliException(DefaultErrorMessage +
                            $"VirtualHost {Broker.VirtualHost} not found.");
                    }

                    throw new CliException(DefaultErrorMessage +
                        $"{ response.StatusCode } - { response.ReasonPhrase}.");
                }

                Console.WriteLine($"Mirror policy {name} applied successfully to queues matching ({pattern})!");
            }
            catch (Exception e)
            {
                ExceptionHandler.Handle(e);
            }
        }

        private static async Task<string> PrepareBody(IRabbitMQApi api, string pattern)
        {
            var nodes = await FetchNumberOfNodes(api);
            var mirrors = CalculateNumberOfMirrors(nodes);
            return BuildPayload(mirrors, pattern);
        }

        private static async Task<int> FetchNumberOfNodes(IRabbitMQApi api)
        {
            var response = await api.GetOverview();
            if (!response.IsSuccessStatusCode)
            {
                throw new CliException(DefaultErrorMessage +
                    $"{ response.StatusCode } - { response.ReasonPhrase}.");
            }

            JObject json = JObject.Parse(response.Content);
            var nodes = json.SelectTokens("$.listeners[?(@.protocol == 'amqp')]");

            var numberOfNodes = nodes?.Count();

            if (numberOfNodes == null)
            {
                throw new CliException(DefaultErrorMessage + 
                    "Could not fetch number of nodes in this cluster.");
            }

            if (numberOfNodes == 0)
            {
                throw new CliException(DefaultErrorMessage +
                    "No node is listening to amqp comunication in this cluster.");
            }

            return numberOfNodes.Value;
        }

        /// <summary>
        /// Calculates the recommended number of mirrors for a queue, based on 
        /// https://www.rabbitmq.com/ha.html#replication-factor.
        /// </summary>
        /// <param name="nodes">Actual number of nodes in the cluster</param>
        /// <returns>Number of mirrors to be used.</returns>
        private static int CalculateNumberOfMirrors(int nodes)
        {
            return (nodes > 3)
                ? nodes / 2 + 1
                : nodes;
        }

        private static string BuildPayload(int numberOfMirrors, string pattern)
        {
            var body = $@"
            {{
                ""pattern"":""{pattern}"",
                ""apply-to"":""queues"",
                ""priority"":0,
                ""definition"":{{
                    ""ha-mode"":""exactly"",
                    ""ha-params"":{numberOfMirrors},
                    ""ha-promote-on-failure"":""when-synced"",
                    ""ha-promote-on-shutdown"":""when-synced"",
                    ""ha-sync-mode"":""automatic""
                }}
            }}";

            return body;
        }
    }
}
