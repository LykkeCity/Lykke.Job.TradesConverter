using System;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Job.TradesConverter.Core.IncomingMessages;
using Lykke.Job.TradesConverter.Core.Services;

namespace Lykke.Job.TradesConverter.RabbitSubscribers
{
    public class MarketOrdersSubscriber : IStartable, IStopable
    {
        private readonly ITradeLogPublisher _publisher;
        private readonly IOrdersConverter _tradesConverter;
        private readonly ILog _log;
        private readonly IConsole _console;
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private RabbitMqSubscriber<MarketOrderWithTrades> _subscriber;

        public MarketOrdersSubscriber(
            ITradeLogPublisher publisher,
            IOrdersConverter tradesConverter,
            ILog log,
            IConsole console,
            string connectionString,
            string exchangeName)
        {
            _publisher = publisher;
            _tradesConverter = tradesConverter;
            _log = log;
            _console = console;
            _connectionString = connectionString;
            _exchangeName = exchangeName;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .CreateForSubscriber(_connectionString, _exchangeName, "tradesconverter")
                .MakeDurable();

            _subscriber = new RabbitMqSubscriber<MarketOrderWithTrades>(
                    settings,
                    new ResilientErrorHandlingStrategy(_log, settings,
                        retryTimeout: TimeSpan.FromSeconds(10),
                        next: new DeadQueueErrorHandlingStrategy(_log, settings)))
                .SetMessageDeserializer(new JsonMessageDeserializer<MarketOrderWithTrades>())
                .SetMessageReadStrategy(new MessageReadQueueStrategy())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .SetLogger(_log)
                .SetConsole(_console)
                .Start();
        }

        private async Task ProcessMessageAsync(MarketOrderWithTrades arg)
        {
            try
            {
                var start = DateTime.UtcNow;
                var trades = await _tradesConverter.ConvertAsync(arg);
                if (trades.Count > 0)
                    await _publisher.PublishAsync(trades);
                if (DateTime.UtcNow.Subtract(start) > TimeSpan.FromMinutes(2))
                    await _log.WriteInfoAsync(nameof(MarketOrdersSubscriber), nameof(ProcessMessageAsync), $"Long processing: {arg.ToJson()}");
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync("MarketOrdersSubscriber.ProcessMessageAsync", arg.ToJson(), ex);
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
