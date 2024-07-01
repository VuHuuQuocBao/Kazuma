/*using Confluent.Kafka;
using Newtonsoft.Json;
using Kazuma.Common.Kafka;
using Kazuma.Domain.Kafka.Models;

namespace Kazuma.IngestService.Consumers
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