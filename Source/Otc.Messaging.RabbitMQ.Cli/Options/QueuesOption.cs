using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

namespace Otc.Messaging.RabbitMQ.Cli.Options
{
    public static class QueuesOption
    {
        public static void AddIn(Command cmd)
        {
            var option = new Option<string>(new[] { "--queues", "-q" });
            option.Argument.Arity = ArgumentArity.OneOrMore;
            option.Description = "Queues names.";
            cmd.AddOption(option);
        }

        public static void Validate(string[] queues, List<object> args)
        {
            if (queues == null)
            {
                throw new ArgumentException($"Option --{nameof(queues)} is required.");
            }

            args.AddRange(queues.AsEnumerable());
        }
    }
}
