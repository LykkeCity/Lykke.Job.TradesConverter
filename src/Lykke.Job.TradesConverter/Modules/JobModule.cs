using Autofac;
using Common.Log;
using Lykke.Common;
using Lykke.Service.ClientAccount.Client;
using Lykke.Job.TradesConverter.Core.Services;
using Lykke.Job.TradesConverter.Services;
using Lykke.Job.TradesConverter.RabbitSubscribers;
using Lykke.Job.TradesConverter.RabbitPublishers;
using Lykke.Job.TradesConverter.Settings;

namespace Lykke.Job.TradesConverter.Modules
{
    public class JobModule : Module
    {
        private readonly AppSettings _settings;
        private readonly ILog _log;
        private readonly IConsole _console;

        public JobModule(AppSettings settings, ILog log, IConsole console)
        {
            _settings = settings;
            _log = log;
            _console = console;
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

            builder.RegisterLykkeServiceClient(_settings.ClientAccountServiceClient.ServiceUrl);

            builder.RegisterResourcesMonitoring(_log);

            builder.RegisterType<OrdersConverter>()
                .As<IOrdersConverter>();

            RegisterRabbitMqSubscribers(builder);

            RegisterRabbitMqPublishers(builder);
        }

        private void RegisterRabbitMqSubscribers(ContainerBuilder builder)
        {
            builder.RegisterType<MarketOrdersSubscriber>()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance()
                .WithParameter("connectionString", _settings.TradesConverterJob.Rabbit.InputConnectionString)
                .WithParameter("exchangeName", _settings.TradesConverterJob.MarketOrdersTradesExchangeName);

            builder.RegisterType<LimitOrdersSubscriber>()
                .As<IStartable>()
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
                .AutoActivate()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.TradesConverterJob.Rabbit.OutputConnectionString));
        }
    }
}
