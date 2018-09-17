using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Job.TradesConverter.Contract;
using Lykke.MatchingEngine.Connector.Models.Events;

namespace Lykke.Job.TradesConverter.Core.Services
{
    public interface IOrdersConverter
    {
        Task<List<TradeLogItem>> ConvertAsync(ExecutionEvent model);
    }
}
