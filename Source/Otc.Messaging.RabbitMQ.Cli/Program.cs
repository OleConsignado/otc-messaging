using Otc.Messaging.RabbitMQ.Cli.Commands;
using System.CommandLine;
using System.Threading.Tasks;

namespace Otc.Messaging.RabbitMQ.Cli
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            var cli = new RootCommand("Otc.Messaging.RabbitMQ.Cli");

            SetupGlobalOptions(cli);

            // Setup available commands
            cli.AddCommand(CreateSimpleQueue.Setup());
            cli.AddCommand(CreateMultipleQueues.Setup());
            cli.AddCommand(CreateVirtualHost.Setup());
            cli.AddCommand(ApplyMirrorPolicy.Setup());

            // Parse command line arguments
            var parsed = cli.Parse(args);

            // Sets global options
            Broker.Host = parsed.CommandResult.ValueForOption<string>("--host");
            Broker.Port = parsed.CommandResult.ValueForOption<int>("--port");
            Broker.ApiPort = parsed.CommandResult.ValueForOption<int>("--apiport");
            Broker.User = parsed.CommandResult.ValueForOption<string>("--user");
            Broker.Password = parsed.CommandResult.ValueForOption<string>("--pass");
            Broker.VirtualHost = parsed.CommandResult.ValueForOption<string>("--vhost");

            // Invoke execution
            await cli.InvokeAsync(args);
        }

        static void SetupGlobalOptions(RootCommand cli)
        {
            cli.AddZeroOrOneOption(new Option<string>(
                new[] { "--host", "-h" }, () => "localhost"));

            cli.AddZeroOrOneOption(new Option<int>(
                new[] { "--port", "-p" }, () => -1));

            cli.AddZeroOrOneOption(new Option<int>(
                new[] { "--apiport", "-a" }, () => Broker.ApiPort));

            cli.AddZeroOrOneOption(new Option<string>(
                new[] { "--user", "-u" }, () => "guest"));

            cli.AddZeroOrOneOption(new Option<string>(
                new[] { "--pass", "-s" }, () => "guest"));

            cli.AddZeroOrOneOption(new Option<string>(
                new[] { "--vhost", "-v" }, () => "/"));
        }
    }
}
