using Otc.Messaging.RabbitMQ.Cli.Options;
using Otc.Messaging.RabbitMQ.PredefinedTopologies;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Otc.Messaging.RabbitMQ.Cli.Commands
{
    public static class CreateMultipleQueues
    {
        public static Command Setup()
        {
            var cmd = new Command(nameof(CreateMultipleQueues));

            TopicOption.AddIn(cmd);
            QueuesOption.AddIn(cmd);
            RetriesOption.AddIn(cmd);
            cmd.Handler = CommandHandler.Create<string, string[], string[]>(Execute);

            return cmd;
        }

        public static void Execute(string topic, string[] queues, string[] retries)
        {
            try
            {
                Console.WriteLine("Executing...");

                var args = new List<object>();
                TopicOption.Validate(topic);
                QueuesOption.Validate(queues, args);
                RetriesOption.Validate(retries, args);

                Broker.Configuration
                    .AddTopology<MultipleQueuesWithRetryTopologyFactory>(
                        topic, args.ToArray());

                var broker = Broker.GetInstance();
                broker.EnsureTopology(topic);

                Console.WriteLine($"Topic {topic} with multiple queues created successfully!");
            }
            catch (Exception e)
            {
                ExceptionHandler.Handle(e);
            }
        }
    }
}
