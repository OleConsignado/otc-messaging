using System;
using System.CommandLine;

namespace Otc.Messaging.RabbitMQ.Cli.Options
{
    public static class TopicOption
    {
        public static void AddIn(Command cmd)
        {
            var option = new Option<string>(new[] { "--topic", "-t" });
            option.Argument.Arity = ArgumentArity.ExactlyOne;
            option.Description = "Main topic name.";
            cmd.AddOption(option);
        }

        public static void Validate(string topic)
        {
            if (string.IsNullOrEmpty(topic))
            {
                throw new ArgumentException($"Option --{nameof(topic)} is required.");
            }
        }
    }
}
