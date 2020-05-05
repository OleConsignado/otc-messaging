using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Otc.Messaging.RabbitMQ.Configurations
{
    public class Topology
    {
        [Required]
        public ICollection<Exchange> Exchanges { get; set; }
    }
}