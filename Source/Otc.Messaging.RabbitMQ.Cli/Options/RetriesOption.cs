using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

namespace Otc.Messaging.RabbitMQ.Cli.Options
{
    public static class RetriesOption
    {
        public static void AddIn(Command cmd)
        {
            var option = new Option<string>(new[] { "--retries", "-r" });
            option.Argument.Arity = ArgumentArity.ZeroOrMore;
            option.Description = "Retry wait in milliseconds.";
            cmd.AddOption(option);
        }

        public static void Validate(string[] retries, List<object> args)
        {
            if (retries != null)
            {
                args.AddRange(retries.AsEnumerable().Select(i =>
                {
                    if (!int.TryParse(i, out int retry))
                    {
                        throw new InvalidCastException($"Value {i} is not an integer.");
                    }
                    return (object)retry;
                }));
            }
        }
    }
}
