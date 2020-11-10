using System.CommandLine;

namespace Otc.Messaging.RabbitMQ.Cli
{
    public static class RootCommandExtensions
    {
        public static void AddZeroOrOneOption<T>(this RootCommand cmd, Option<T> option)
        {
            option.Argument.Arity = ArgumentArity.ZeroOrOne;
            cmd.AddGlobalOption(option);
        }
    }
}
