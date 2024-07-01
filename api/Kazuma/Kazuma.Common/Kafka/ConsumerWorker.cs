using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spaghetti.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spaghetti.Common.Kafka
{
    public class ConsumerWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly KafkaConsumerConfiguration _kafkaConsumerConfig;
        private readonly ITopicConsumer[] _topicConsumers;

        public ConsumerWorker(IServiceProvider services, ILogger<ConsumerWorker> logger, IOptions<KafkaConsumerConfiguration> kafkaConsumerConfigOptions)
        {
            _logger = logger;
            _kafkaConsumerConfig = kafkaConsumerConfigOptions.Value;
            // all consumer defined to consume topic in worker;
            _topicConsumers = services.GetServices<ITopicConsumer>().Where(t => !t.Disabled).ToArray();
        }

        public override void Dispose()
        {
            foreach (var topicConsumer in _topicConsumers)
                topicConsumer.Dispose();
            base.Dispose();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            // start subscribe all 
            //var index = 1;
            foreach (var topicConsumer in _topicConsumers)
            {
                //_kafkaConsumerConfig.GroupId += index;
                topicConsumer.Start(_kafkaConsumerConfig);
                //index++;
            }
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            foreach (var topicConsumer in _topicConsumers)
                topicConsumer.Stop();
        }

        protected sealed override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 
            return _topicConsumers.Length <= 0 ? Task.CompletedTask :
                Task.WhenAll(_topicConsumers.Select(tc => Task.Run(() => tc.Execute(stoppingToken), stoppingToken)));
        }
    }
}
