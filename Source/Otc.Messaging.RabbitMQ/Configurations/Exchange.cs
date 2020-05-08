using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Otc.Messaging.RabbitMQ.Configurations
{
    public class Exchange
    {
        [Required]
        public string Name { get; set; }
        public string Type { get; set; } = ExchangeType.Direct;
        public bool Durable { get; set; } = true;
        public bool AutoDelete { get; set; } = false;
        public IDictionary<string, object> Arguments { get; set; } =
            new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

        public ICollection<Queue> Queues { get; set; } = new List<Queue>();
    }
}


