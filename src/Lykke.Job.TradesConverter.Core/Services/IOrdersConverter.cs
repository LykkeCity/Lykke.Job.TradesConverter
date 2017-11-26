using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Job.TradesConverter.Core.IncomingMessages;
using Lykke.Job.TradesConverter.Contract;

namespace Lykke.Job.TradesConverter.Core.Services
{
    public interface IOrdersConverter
    {
        Task<List<TradeLogItem>> ConvertAsync(LimitOrderWithTrades model);

        Task<List<TradeLogItem>> ConvertAsync(MarketOrderWithTrades model);
    }
}
