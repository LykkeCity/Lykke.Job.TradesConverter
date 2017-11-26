using System.Threading.Tasks;
using Autofac;
using Common;
using Lykke.Job.TradesConverter.Contract;

namespace Lykke.Job.TradesConverter.Core.Services
{
    public interface ITradeLogPublisher : IStartable, IStopable
    {
        Task PublishAsync(TradeLogItem message);
    }
}
