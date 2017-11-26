using System.Threading.Tasks;

namespace Lykke.Job.TradesConverter.Core.Services
{
    public interface IShutdownManager
    {
        Task StopAsync();
    }
}