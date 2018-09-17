using System.Threading.Tasks;
using System.Collections.Generic;
using Lykke.Job.TradesConverter.Contract;

namespace Lykke.Job.TradesConverter.Core.Services
{
    public interface ITradeLogPublisher : IStartStop
    {
        Task PublishAsync(List<TradeLogItem> message);
    }
}
