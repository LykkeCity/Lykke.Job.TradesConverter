using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.TradesConverter.Core.Services;
using Lykke.MatchingEngine.Connector.Models.Events;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.Job.TradesConverter.RabbitSubscribers
{
    [UsedImplicitly]
    public class OrdersSubscriber : IStartStop
    {
        private readonly ITradeLogPublisher _publisher;
        private readonly IOrdersConverter _tradesConverter;
        private readonly ILog _log;
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly TimeSpan _processTimeThreshold = TimeSpan.FromMinutes(1);

        private RabbitMqSubscriber<ExecutionEvent> _subscriber;

        public OrdersSubscriber(
            ITradeLogPublisher publisher,
            IOrdersConverter tradesConverter,
            ILog log,
            string connectionString,
            string exchangeName)
        {
            _publisher = publisher;
            _tradesConverter = tradesConverter;
            _log = log;
            _connectionString = connectionString;
            _exchangeName = exchangeName;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .CreateForSubscriber(_connectionString, _exchangeName, "tradesconverter")
                .MakeDurable();

            _subscriber = new RabbitMqSubscriber<ExecutionEvent>(
                    settings,
                    new ResilientErrorHandlingStrategy(_log, settings,
                        retryTimeout: TimeSpan.FromSeconds(10),
                        next: new DeadQueueErrorHandlingStrategy(_log, settings)))
                .SetMessageDeserializer(new ProtobufMessageDeserializer<ExecutionEvent>())
                .SetMessageReadStrategy(new MessageReadQueueStrategy())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .SetLogger(_log)
                .Start();
        }

        private async Task ProcessMessageAsync(ExecutionEvent arg)
        {
            try
            {
                var start = DateTime.UtcNow;
                var convertStart = DateTime.UtcNow;
                var allTrades = await _tradesConverter.ConvertAsync(arg);
                var convertTime = DateTime.UtcNow.Subtract(convertStart);
                if (convertTime > _processTimeThreshold)
                    _log.WriteWarning(nameof(OrdersSubscriber), nameof(ProcessMessageAsync), $"Long convert ({convertTime}): from {arg.ToJson()} to {allTrades.ToJson()}");

                if (allTrades.Count > 0)
                    await _publisher.PublishAsync(allTrades);

                var elapsed = DateTime.UtcNow.Subtract(start);
                if (elapsed > _processTimeThreshold)
                    _log.WriteWarning(nameof(OrdersSubscriber), nameof(ProcessMessageAsync), $"Long processing ({elapsed}): {arg.ToJson()}");
            }
            catch (Exception ex)
            {
                _log.WriteError("OrdersSubscriber.ProcessMessageAsync", arg.ToJson(), ex);
                throw;
            }
        }

        public void Dispose()
        {
            _subscriber?.Dispose();
        }

        public void Stop()
        {
            _subscriber.Stop();
        }
    }
}
