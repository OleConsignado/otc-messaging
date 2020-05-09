using Otc.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Otc.Messaging.RabbitMQ.Configurations
{
    public class Topology
    {
        [Required]
        public IEnumerable<Exchange> Exchanges { get; set; }
    }
}