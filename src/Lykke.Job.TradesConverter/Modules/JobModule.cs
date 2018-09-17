using Autofac;
using Common;
using Common.Log;
using Lykke.Common;
using Lykke.Job.TradesConverter.Core.Services;
using Lykke.Job.TradesConverter.RabbitSubscribers;
using Lykke.Job.TradesConverter.RabbitPublishers;
using Lykke.Job.TradesConverter.Services;
using Lykke.Job.TradesConverter.Settings;
using Lykke.Service.ClientAccount.Client;

namespace Lykke.Job.TradesConverter.Modules
{
    public class JobModule : Module
    {
        private readonly AppSettings _settings;
        private readonly ILog _log;

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

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>()
                .SingleInstance();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>()
                .AutoActivate()
                .SingleInstance();

            builder.RegisterLykkeServiceClient(_settings.ClientAccountServiceClient.ServiceUrl);

            builder.RegisterResourcesMonitoring(_log);

            builder.RegisterType<OrdersConverter>()
                .As<IOrdersConverter>();

            RegisterRabbitMqSubscribers(builder);

            RegisterRabbitMqPublishers(builder);
        }

        private void RegisterRabbitMqSubscribers(ContainerBuilder builder)
        {
            builder.RegisterType<OrdersSubscriber>()
                .As<IStartStop>()
                .SingleInstance()
                .AutoActivate()
                .WithParameter("connectionString", _settings.TradesConverterJob.Rabbit.InputConnectionString)
                .WithParameter("exchangeName", _settings.TradesConverterJob.EventsExchangeName);
        }

        private void RegisterRabbitMqPublishers(ContainerBuilder builder)
        {
            builder.RegisterType<TradesPublisher>()
                .As<ITradeLogPublisher>()
                .As<IStartable>()
                .As<IStopable>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.TradesConverterJob.Rabbit.OutputConnectionString));
        }
    }
}
