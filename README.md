# Otc.Messaging
Otc.Messaging goal is to abstract complex aspects of working with Messaging Systems by providing a simplified and easy to use API for .NET Standards 2.0. 

Currently it supports only [RabbitMQ](https://rabbitmq.com/) as backend. `Otc.Messaging.RabbitMQ` was built on top of [RabbitMQ.Client](https://github.com/rabbitmq/rabbitmq-dotnet-client) and provides opinionated implementation of some common patterns.

## Quickstart

### Installation

Recommended to install it from NuGet. It's spplited into two packages:

* [Otc.Messaging.Abstractions](https://www.nuget.org/packages/Otc.Messaging.Abstractions) - Interfaces, exception types, all you need to use it, except implementation;
* [Otc.Messaging.RabbitMQ](https://www.nuget.org/packages/Otc.Messaging.RabbitMQ) - RabbitMQ implementation.

** *Curretly only pre-release packages are available*

### Configuration

At startup, add `IMessaging` to your service collection by calling `AddRabbitMQ` extension method for `IServiceCollection` (`AddRabbitMQ` lives at `Otc.Messaging.RabbitMQ` assembly):

```cs
services.AddRabbitMQ(new RabbitMQConfiguration
{ 
    Host = "localhost",
    Port = 5672,
    User = "guest",
    Password = "guest"
});

```

`AddRabbitMQ` will register `RabbitMQMessaging` implementation (from `Otc.Messaging.RabbitMQ` assembly) for `IMessaging` interface (from `Otc.Messaging.Abstractions` assembly) as scoped lifetime.

### Basic Usage

#### Publish to a topic (exchange)

```cs
IMessaging bus = ... // get messaging bus from service provider (using dependency injection)

string message = "Hello world!";
byte[] messageBytes = Encoding.UTF8.GetBytes(message);

// Publish "Hello world!" string to a topic named "TopicName"
bus.Publish("TopicName", messageBytes);
```
#### Subscribe to queue(s)

Subscribe to queue(s) and start consuming:

```cs
IMessaging bus = ... // taken from service provider

// Subscribe to "QueueName1" and "QueueName2" queues
ISubscription subscription = bus.Subscribe(message =>
{
    // do something useful with the message
}
, "QueueName1", "QueueName2", ...);

// Start consuming messages
subscription.Start();

// To stop consuming messages
subscription.Stop();
```
