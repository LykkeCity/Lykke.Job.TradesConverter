﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Job.TradesConverter.Contract;
using Lykke.Job.TradesConverter.Core.IncomingMessages;
using Lykke.Job.TradesConverter.Core.Services;

namespace Lykke.Job.TradesConverter.RabbitSubscribers
{
    public class LimitOrdersSubscriber : IStartable, IStopable
    {
        private readonly ITradeLogPublisher _publisher;
        private readonly IOrdersConverter _tradesConverter;
        private readonly ILog _log;
        private readonly IConsole _console;
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private RabbitMqSubscriber<LimitOrders> _subscriber;

        public LimitOrdersSubscriber(
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

            _subscriber = new RabbitMqSubscriber<LimitOrders>(
                    settings,
                    new ResilientErrorHandlingStrategy(_log, settings,
                        retryTimeout: TimeSpan.FromSeconds(10),
                        next: new DeadQueueErrorHandlingStrategy(_log, settings)))
                .SetMessageDeserializer(new JsonMessageDeserializer<LimitOrders>())
                .SetMessageReadStrategy(new MessageReadQueueStrategy())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .SetLogger(_log)
                .SetConsole(_console)
                .Start();
        }

        private async Task ProcessMessageAsync(LimitOrders arg)
        {
            try
            {
                var start = DateTime.UtcNow;
                var allTrades = new List<TradeLogItem>();
                foreach (var order in arg.Orders)
                {
                    var trades = await _tradesConverter.ConvertAsync(order);
                    allTrades.AddRange(trades);
                }
                if (allTrades.Count > 0)
                    await _publisher.PublishAsync(allTrades);
                if (DateTime.UtcNow.Subtract(start) > TimeSpan.FromMinutes(2))
                    await _log.WriteInfoAsync(nameof(LimitOrdersSubscriber), nameof(ProcessMessageAsync), $"Long processing: {arg.ToJson()}");
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync("LimitOrdersSubscriber.ProcessMessageAsync", arg.ToJson(), ex);
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
