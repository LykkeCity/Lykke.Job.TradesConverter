using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.TradesConverter.Contract;
using Lykke.Job.TradesConverter.Core.Services;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.Job.TradesConverter.RabbitPublishers
{
    [UsedImplicitly]
    public class TradesPublisher : ITradeLogPublisher
    {
        private readonly ILog _log;
        private readonly string _connectionString;
        private readonly TimeSpan _timeThreshold = TimeSpan.FromMinutes(1);
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        private RabbitMqPublisher<List<TradeLogItem>> _publisher;

        public TradesPublisher(ILog log, string connectionString)
        {
            _log = log;
            _connectionString = connectionString;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .CreateForPublisher(_connectionString, "tradelog")
                .MakeDurable();

            _publisher = new RabbitMqPublisher<List<TradeLogItem>>(settings)
                .SetSerializer(new MessagePackMessageSerializer<List<TradeLogItem>>())
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(settings))
                .PublishSynchronously()
                .SetLogger(_log)
                .Start();
        }

        public void Dispose()
        {
            _publisher?.Dispose();
        }

        public void Stop()
        {
            _publisher?.Stop();
        }

        public async Task PublishAsync(List<TradeLogItem> message)
        {
            await _lock.WaitAsync();
            try
            {
                var publishStart = DateTime.UtcNow;
                await _publisher.ProduceAsync(message);
                var publishTime = DateTime.UtcNow.Subtract(publishStart);
                if (publishTime > _timeThreshold)
                {
                    _log.WriteWarning(nameof(TradesPublisher), nameof(PublishAsync), $"Long publish ({publishTime}): {message.ToJson()}");
                    _publisher.Stop();
                    _publisher.Start();
                }
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
