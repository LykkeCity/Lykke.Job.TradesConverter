﻿using Autofac;
using Common;
using Common.Log;
using Lykke.Service.ClientAccount.Client;
using Lykke.Job.TradesConverter.Core.Services;
using Lykke.Job.TradesConverter.Core.Settings;
using Lykke.Job.TradesConverter.Services;
using Lykke.Job.TradesConverter.RabbitSubscribers;
using Lykke.Job.TradesConverter.RabbitPublishers;

namespace Lykke.Job.TradesConverter.Modules
{
    public class JobModule : Module
    {
        private readonly AppSettings _settings;
        private readonly ILog _log;
        private readonly IConsole _console = new LogToConsole();

        public JobModule(AppSettings settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterInstance(_console)
                .As<IConsole>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>();

            builder.RegisterLykkeServiceClient(_settings.ClientAccountClient.ServiceUrl);

            builder.RegisterType<OrdersConverter>()
                .As<IOrdersConverter>();

            RegisterRabbitMqPublishers(builder);

            RegisterRabbitMqSubscribers(builder);
        }

        private void RegisterRabbitMqSubscribers(ContainerBuilder builder)
        {
            builder.RegisterType<MarketOrdersSubscriber>()
                .As<IStartable>()
                .As<IStopable>()
                .AutoActivate()
                .SingleInstance()
                .WithParameter("connectionString", _settings.TradesConverterJob.Rabbit.InputConnectionString)
                .WithParameter("exchangeName", _settings.TradesConverterJob.MarketOrdersTradesExchangeName);

            builder.RegisterType<LimitOrdersSubscriber>()
                .As<IStartable>()
                .As<IStopable>()
                .AutoActivate()
                .SingleInstance()
                .WithParameter("connectionString", _settings.TradesConverterJob.Rabbit.InputConnectionString)
                .WithParameter("exchangeName", _settings.TradesConverterJob.LimitOrdersTradesExchangeName);
        }

        private void RegisterRabbitMqPublishers(ContainerBuilder builder)
        {
            builder.RegisterType<TradesPublisher>()
                .As<ITradeLogPublisher>()
                .As<IStartable>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.TradesConverterJob.Rabbit.OutputConnectionString));
        }
    }
}