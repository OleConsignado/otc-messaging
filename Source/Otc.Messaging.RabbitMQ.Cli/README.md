# Otc.Messaging.RabbitMQ.Cli

This is a simple commandline utility allowing to apply predefined topologies found in [Otc.Messaging.RabbitMQ.PredefinedTopologies](../Otc.Messaging.RabbitMQ.PredefinedTopologies).

It may grow to acomodate more commands as need appears, check [here](./Commands) to get a list of available commands.

## Compiling

Compile using dotnet cli. Here is an example of linux publishing:

`dotnet publish -r linux-x64 -p:PublishSingleFile=true --self-contained true -o ./linux-x64`

## Basic Usage

Run `otcrabbitmq -h` to get a list of global arguments needed to connect to your RabbitMQ installation, like host, port, user and password.

### Creating a new virtual host named cli-tests.

`./otcrabbitmq CreateVirtualHost -v cli-tests`

### Creating a simple queue topology in a virtual host.

`./otcrabbitmq CreateSimpleQueue -v cli-tests -t my-simple-queue`

### Creating a simple queue topology with 2 retries, one after 10 seconds, and another after 20 seconds.

`./otcrabbitmq CreateSimpleQueue -v cli-tests -t my-topic-with-retries -r 10000 20000`

### Creating a fanout queue topology with 2 queues and 2 retries each.

`./otcrabbitmq CreateMultipleQueues -v cli-tests -t my-topic-with-2-queues-and-2-retries-each -q my-queue-1 my-queue-2 -r 10000 20000`

### Applying mirror policy to all queues starting with 'my-queue'.

`./otcrabbitmq ApplyMirrorPolicy -v cli-tests -n my-policy --pattern "^my-queue.*"`

*Note:* All VirtualHosts are created with a default mirror policy that applies to all of its queues.