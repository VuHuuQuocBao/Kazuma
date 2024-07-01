using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Spaghetti.Common.Configuration;
using Spaghetti.Core.Utilities.Kafka;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using static Confluent.Kafka.ConfigPropertyNames;


namespace Spaghetti.Common.Kafka
{
    public sealed class KafkaProducer : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IProducer<byte[], byte[]> _producer;

        public KafkaProducer(ILogger<KafkaProducer> logger, IOptions<KafkaProducerConfiguration> kafkaProducerConfigOptions)
        {
            _logger = logger;
            _producer = KafkaUtility.CreateRawProducerWithKey(kafkaProducerConfigOptions.Value);
            var adminClient = new AdminClientBuilder(kafkaProducerConfigOptions.Value).Build();
            var metadata = adminClient.GetMetadata("Manga-Generic", TimeSpan.FromSeconds(10000));
            Console.WriteLine($"Topic: {metadata.Topics[0].Topic} has {metadata.Topics[0].Partitions.Count} partitions.");
        }

        public KafkaProducer(ILogger<KafkaProducer> logger, KafkaProducerConfiguration kafkaProducerConfigOptions)
        {
            _logger = logger;
            _producer = KafkaUtility.CreateRawProducerWithKey(kafkaProducerConfigOptions);
        }

        public void Dispose()
        {
            _producer.Dispose();
            GC.SuppressFinalize(this);
        }

        public Task<KafkaDeliveryResult<Null, TValue>> ProduceAsync<TValue>(string topic, TValue value, CancellationToken cancellationToken = default)
        {
            return ProduceAsync(topic, new Message<Null, TValue>() { Value = value }, cancellationToken);
        }

        public Task<KafkaDeliveryResult<Null, TValue>> ProduceAsync<TValue>(TopicPartition topicPartition, TValue value, CancellationToken cancellationToken = default)
        {
            return  ProduceAsync(topicPartition, new Message<Null, TValue>() { Value = value }, cancellationToken);
        }

        /*        public Task<KafkaDeliveryResult<Null, TValue>> ProduceAsyncIncludeNullValue<TValue>(string topic, TValue value, CancellationToken cancellationToken = default)
                {
                    return ProduceAsyncIncludeNullValue(topic, new Message<Null, TValue>() { Value = value }, cancellationToken);
                }

                public Task<KafkaDeliveryResult<Null, TValue>> ProduceAsync<TValue>(TopicPartition topicPartition, TValue value, CancellationToken cancellationToken = default)
                {
                    return ProduceAsync(topicPartition, new Message<Null, TValue>() { Value = value }, cancellationToken);
                }

                public Task<KafkaDeliveryResult<TKey, TValue>> ProduceAsync<TKey, TValue>(string topic, TValue value, TKey key, CancellationToken cancellationToken = default)
                {
                    return ProduceAsync(topic, new Message<TKey, TValue>() { Key = key, Value = value }, cancellationToken);
                }

                public Task<KafkaDeliveryResult<TKey, TValue>> ProduceAsync<TKey, TValue>(TopicPartition topicPartition, TValue value, TKey key, CancellationToken cancellationToken = default)
                {
                    return ProduceAsync(topicPartition, new Message<TKey, TValue>() { Key = key, Value = value }, cancellationToken);
                }*/

        public async Task<KafkaDeliveryResult<TKey, TValue>> ProduceAsync<TKey, TValue>(string topic, Message<TKey, TValue> message, CancellationToken cancellationToken = default)
        {
            // setting

            var keyJson = message.Key == null ? null : JsonConvert.SerializeObject(message.Key);
            var valueJson = message.Value == null ? null : JsonConvert.SerializeObject(message.Value);
            var producingMessage = new Message<byte[], byte[]>()
            {
                Headers = message.Headers,
                Timestamp = message.Timestamp,
                Key = keyJson == null ? default : Encoding.UTF8.GetBytes(keyJson),
                Value = valueJson == null ? default : Encoding.UTF8.GetBytes(valueJson)
            };
            try
            {
                return new KafkaDeliveryResult<TKey, TValue>(
                    message,
                    await _producer.ProduceAsync(
                        topic,
                        producingMessage,
                        cancellationToken
                    )
                );
            }
            catch (ProduceException<byte[], byte[]> ex)
            {
                if (ex.Error.Code == ErrorCode.MsgSizeTooLarge)
                    throw new Exception($"KAFKA-Error Produce to {topic} {ex.Error.Code}: KEY[{keyJson?.Length ?? 0}]={(keyJson == null || keyJson.Length < 64 ? keyJson : (keyJson.Substring(0, 64) + ".."))} | VALUE[{valueJson?.Length ?? 0}]={(valueJson == null || valueJson.Length < 1024 ? valueJson : (valueJson.Substring(0, 1024) + ".."))}");
                throw;
            }
        }

        /*public async Task<KafkaDeliveryResult<TKey, TValue>> ProduceAsyncIncludeNullValue<TKey, TValue>(string topic, Message<TKey, TValue> message, CancellationToken cancellationToken = default)
        {
            // setting 
            var keyJson = message.Key == null ? null : Json.SerializeIncludeNullValue(message.Key);
            var valueJson = message.Value == null ? null : Json.SerializeIncludeNullValue(message.Value);
            var producingMessage = new Message<byte[], byte[]>()
            {
                Headers = message.Headers,
                Timestamp = message.Timestamp,
                Key = keyJson == null ? default : Encoding.UTF8.GetBytes(keyJson),
                Value = valueJson == null ? default : Encoding.UTF8.GetBytes(valueJson)
            };
            try
            {
                return new KafkaDeliveryResult<TKey, TValue>(
                    message,
                    await _producer.ProduceAsync(
                        topic,
                        producingMessage,
                        cancellationToken
                    )
                );
            }
            catch (ProduceException<byte[], byte[]> ex)
            {
                if (ex.Error.Code == ErrorCode.MsgSizeTooLarge)
                    throw new Exception($"KAFKA-Error Produce to {topic} {ex.Error.Code}: KEY[{keyJson?.Length ?? 0}]={(keyJson == null || keyJson.Length < 64 ? keyJson : (keyJson.Substring(0, 64) + ".."))} | VALUE[{valueJson?.Length ?? 0}]={(valueJson == null || valueJson.Length < 1024 ? valueJson : (valueJson.Substring(0, 1024) + ".."))}");
                throw;
            }
        }*/
        public async Task<KafkaDeliveryResult<TKey, TValue>> ProduceAsync<TKey, TValue>(TopicPartition topicPartition, Message<TKey, TValue> message, CancellationToken cancellationToken = default)
        {
            // setting ?
            var keyJson = message.Key == null ? null : JsonConvert.SerializeObject(message.Key);
            var valueJson = message.Value == null ? null : JsonConvert.SerializeObject(message.Value);
            var producingMessage = new Message<byte[], byte[]>()
            {
                Headers = message.Headers,
                Timestamp = message.Timestamp,
                Key = keyJson == null ? default : Encoding.UTF8.GetBytes(keyJson),
                Value = valueJson == null ? default : Encoding.UTF8.GetBytes(valueJson)
            };
            try
            {
                return new KafkaDeliveryResult<TKey, TValue>(
                    message,
                    await _producer.ProduceAsync(
                        topicPartition,
                        producingMessage,
                        cancellationToken
                    )
                );
            }
            catch (ProduceException<byte[], byte[]> ex)
            {
                if (ex.Error.Code == ErrorCode.MsgSizeTooLarge)
                    throw new Exception($"KAFKA-Error Produce to {topicPartition.Topic}[{topicPartition.Partition.Value}] {ex.Error.Code}: KEY[{keyJson?.Length ?? 0}]={(keyJson == null || keyJson.Length < 64 ? keyJson : (keyJson.Substring(0, 64) + ".."))} | VALUE[{valueJson?.Length ?? 0}]={(valueJson == null || valueJson.Length < 1024 ? valueJson : (valueJson.Substring(0, 1024) + ".."))}");
                throw;
            }
        }
    }

    public class KafkaDeliveryResult<TKey, TValue>
    {
        private readonly DeliveryResult<byte[], byte[]> _deliveryResult;
        private Message<TKey, TValue> _message;

        internal KafkaDeliveryResult(Message<TKey, TValue> message, DeliveryResult<byte[], byte[]> deliveryResult)
        {
            _deliveryResult = deliveryResult;
            _message = message;
        }

        public string Topic { get { return _deliveryResult.Topic; } set { _deliveryResult.Topic = value; } }
        public Partition Partition { get { return _deliveryResult.Partition; } set { _deliveryResult.Partition = value; } }
        public Offset Offset { get { return _deliveryResult.Offset; } set { _deliveryResult.Offset = value; } }
        public TopicPartition TopicPartition { get { return _deliveryResult.TopicPartition; } }
        public TopicPartitionOffset TopicPartitionOffset { get { return _deliveryResult.TopicPartitionOffset; } set { _deliveryResult.TopicPartitionOffset = value; } }
        public PersistenceStatus Status { get { return _deliveryResult.Status; } set { _deliveryResult.Status = value; } }
        public Message<TKey, TValue> Message { get { return _message; } set { _message = value; } }
        public TKey Key { get { return _message.Key; } set { _message.Key = value; } }
        public TValue Value { get { return _message.Value; } set { _message.Value = value; } }
        public Timestamp Timestamp { get { return _deliveryResult.Timestamp; } set { _deliveryResult.Timestamp = value; } }
        public Headers Headers { get { return _deliveryResult.Headers; } set { _deliveryResult.Headers = value; } }
    }
}
