using System;
using System.Collections.Generic;

namespace Otc.Messaging.RabbitMQ.Configurations
{
    public class Queue
    {
        public string Name { get; set; } = "";
        public bool Durable { get; set; } = true;
        public bool Exclusive { get; set; } = false;
        public bool AutoDelete { get; set; } = false;
        public string RoutingKey { get; set; } = "";
        public IDictionary<string, object> Arguments { get; set; } =
            new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
    }
}