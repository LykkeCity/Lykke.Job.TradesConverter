﻿namespace Lykke.Job.TradesConverter.Settings
{
    public class AppSettings
    {
        public TradesConverterSettings TradesConverterJob { get; set; }

        public SlackNotificationsSettings SlackNotifications { get; set; }

        public ClientAccountClientSettings ClientAccountServiceClient { get; set; }
    }

    public class SlackNotificationsSettings
    {
        public AzureQueuePublicationSettings AzureQueue { get; set; }
    }

    public class ClientAccountClientSettings
    {
        public string ServiceUrl { get; set; }
    }

    public class AzureQueuePublicationSettings
    {
        public string ConnectionString { get; set; }

        public string QueueName { get; set; }
    }

    public class DbSettings
    {
        public string LogsConnString { get; set; }
    }

    public class RabbitMqSettings
    {
        public string InputConnectionString { get; set; }

        public string OutputConnectionString { get; set; }
    }

    public class TradesConverterSettings
    {
        public DbSettings Db { get; set; }

        public RabbitMqSettings Rabbit { get; set; }

        public string MarketOrdersTradesExchangeName { get; set; }

        public string LimitOrdersTradesExchangeName { get; set; }
    }
}