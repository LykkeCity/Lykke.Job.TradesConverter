namespace Lykke.Job.TradesConverter.Core.Settings
{
    public class TradesConverterSettings
    {
        public DbSettings Db { get; set; }

        public RabbitMqSettings Rabbit { get; set; }

        public string MarketOrdersTradesExchangeName { get; set; }

        public string LimitOrdersTradesExchangeName { get; set; }
    }
}
