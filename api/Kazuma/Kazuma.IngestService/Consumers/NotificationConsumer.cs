/*using Confluent.Kafka;
using Newtonsoft.Json;
using Spaghetti.Common.Kafka;
using Spaghetti.Domain.Kafka.Models;

namespace Spaghetti.IngestService.Consumers
{
    public class NotificationConsumer : TopicConsumer<string, KafkaMessage>
    {
        public NotificationConsumer(ILogger<TestConsumer> logger) : base(logger, "Test-Notification")
        {
        }

        protected override Task OnMessageAsync(ConsumeResult<string, KafkaMessage> consumeResult, CancellationToken stoppingToken)
        {
           
        }
    }
}
*/