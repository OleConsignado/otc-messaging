using Microsoft.Extensions.DependencyInjection;
using Otc.Messaging.Abstractions;
using Otc.Messaging.RabbitMQ.Configurations;
using System;
using System.Collections.Generic;

namespace Otc.Messaging.RabbitMQ.Cli
{
    public static class Broker
    {
        public static string Host { get; set; }
        public static int Port { get; set; }
        public static string User { get; set; }
        public static string Password { get; set; }
        public static string VirtualHost { get; set; }
        public static int ApiPort { get; set; } = 15672;
        public static string ApiBaseUrl
        {
            get => $"http://{Host}:{ApiPort}/api";
        }

        private static RabbitMQConfiguration configuration;
        public static RabbitMQConfiguration Configuration
        {
            get
            {
                if (configuration == null)
                {
                    configuration = new RabbitMQConfiguration
                    {
                        Hosts = new List<string> { Host },
                        Port = Port,
                        User = User,
                        Password = Password,
                        VirtualHost = VirtualHost
                    };
                }

                return configuration;
            }

            set => configuration = value;
        }

        private static IMessaging broker;
        public static IMessaging GetInstance()
        {
            if (broker == null)
            {
                var provider = ServiceProvider.GetInstance();
                broker = provider.GetService<IMessaging>();
            }

            return broker;
        }
    }
}
