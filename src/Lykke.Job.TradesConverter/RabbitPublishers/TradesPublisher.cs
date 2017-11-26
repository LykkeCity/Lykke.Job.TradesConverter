using System.Threading.Tasks;
using Common.Log;
using Lykke.Job.TradesConverter.Contract;
using Lykke.Job.TradesConverter.Core.Services;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.Job.TradesConverter.RabbitPublishers
{
    public class TradesPublisher : ITradeLogPublisher
    {
        private readonly ILog _log;
        private readonly IConsole _console;
        private readonly string _connectionString;
        private RabbitMqPublisher<TradeLogItem> _publisher;

        public TradesPublisher(
            ILog log,
            IConsole console,
            string connectionString)
        {
            _log = log;
            _console = console;
            _connectionString = connectionString;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .CreateForPublisher(_connectionString, "tradesconverter")
                .MakeDurable();

            _publisher = new RabbitMqPublisher<TradeLogItem>(settings)
                .SetSerializer(new JsonMessageSerializer<TradeLogItem>())
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(settings))
                .PublishSynchronously()
                .SetLogger(_log)
                .SetConsole(_console)
                .Start();
        }

        public void Dispose()
        {
            _publisher?.Dispose();
        }

        public void Stop()
        {
            _publisher?.Stop();
        }

        public async Task PublishAsync(TradeLogItem message)
        {
            await _publisher.ProduceAsync(message);
        }
    }
}
