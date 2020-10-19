using Otc.Messaging.RabbitMQ.Cli.Options;
using Otc.Messaging.RabbitMQ.PredefinedTopologies;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Otc.Messaging.RabbitMQ.Cli.Commands
{
    public static class CreateSimpleQueue
    {
        public static Command Setup()
        {
            var cmd = new Command(nameof(CreateSimpleQueue));

            TopicOption.AddIn(cmd);
            RetriesOption.AddIn(cmd);
            cmd.Handler = CommandHandler.Create<string, string[]>(Execute);

            return cmd;
        }

        public static void Execute(string topic, string[] retries)
        {
            try
            {
                Console.WriteLine("Executing...");

                var args = new List<object>();
                TopicOption.Validate(topic);
                RetriesOption.Validate(retries, args);

                Broker.Configuration
                    .AddTopology<SimpleQueueWithRetryTopologyFactory>(
                        topic, args.ToArray());

                var broker = Broker.GetInstance();
                broker.EnsureTopology(topic);

                Console.WriteLine($"Simple queue {topic} created successfully!");
            }
            catch (Exception e)
            {
                ExceptionHandler.Handle(e);
            }
        }
    }
}
