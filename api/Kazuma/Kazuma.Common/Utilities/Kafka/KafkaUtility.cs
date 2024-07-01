using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spaghetti.Core.Utilities.Kafka
{
    public static class KafkaUtility
    {
        public static IProducer<byte[], byte[]> CreateRawProducerWithKey(ProducerConfig producerConfig)
        {
            var producerBuilder = new ProducerBuilder<byte[], byte[]>(producerConfig);
            producerBuilder.SetKeySerializer(RawSerializer.Instance);
            producerBuilder.SetValueSerializer(RawSerializer.Instance);
            return producerBuilder.Build();
        }
        sealed class RawSerializer : ISerializer<byte[]>
        {
            public static RawSerializer Instance = new RawSerializer();

            private RawSerializer() { }

            public byte[] Serialize(byte[] value, SerializationContext context)
            {
                return value;
            }
        }
    }
}
