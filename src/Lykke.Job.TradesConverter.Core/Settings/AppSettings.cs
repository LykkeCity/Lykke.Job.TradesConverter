namespace Lykke.Job.TradesConverter.Core.Settings
{
    public class AppSettings
    {
        public TradesConverterSettings TradesConverterJob { get; set; }

        public SlackNotificationsSettings SlackNotifications { get; set; }

        public ClientAccountClientSettings ClientAccountClient { get; set; }
    }

    public class SlackNotificationsSettings
    {
        public AzureQueuePublicationSettings AzureQueue { get; set; }
    }

    public class ClientAccountClientSettings
    {
        public string ServiceUrl { get; set; }
    }
}
