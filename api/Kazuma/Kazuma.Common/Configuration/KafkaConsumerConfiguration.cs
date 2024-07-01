using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kazuma.Common.Configuration
{
    public class KafkaConsumerConfiguration : ConsumerConfig
    {
        public string? PayCheckoutOrderGroup { get; set; }
        public string? PayCheckoutOrderTopic { get; set; }
    }
}
