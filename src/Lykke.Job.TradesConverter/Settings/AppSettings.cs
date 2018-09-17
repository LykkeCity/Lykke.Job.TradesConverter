using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.TradesConverter.Settings
{
    public class AppSettings
    {
        public TradesConverterSettings TradesConverterJob { get; set; }

        public SlackNotificationsSettings SlackNotifications { get; set; }

        public ClientAccountClientSettings ClientAccountServiceClient { get; set; }

        public MonitoringServiceClientSettings MonitoringServiceClient { get; set; }
    }

    public class SlackNotificationsSettings
    {
        public AzureQueuePublicationSettings AzureQueue { get; set; }
    }

    public class ClientAccountClientSettings
    {
        [HttpCheck("api/isalive")]
        public string ServiceUrl { get; set; }
    }

    public class MonitoringServiceClientSettings
    {
        [HttpCheck("api/isalive")]
        public string MonitoringServiceUrl { get; set; }
    }

    public class AzureQueuePublicationSettings
    {
        public string ConnectionString { get; set; }

        public string QueueName { get; set; }
    }

    public class DbSettings
    {
        [AzureTableCheck]
        public string LogsConnString { get; set; }
    }

    public class RabbitMqSettings
    {
        [AmqpCheck]
        public string InputConnectionString { get; set; }

        [AmqpCheck]
        public string OutputConnectionString { get; set; }
    }

    public class TradesConverterSettings
    {
        public DbSettings Db { get; set; }

        public RabbitMqSettings Rabbit { get; set; }

        public string EventsExchangeName { get; set; }
    }
}
