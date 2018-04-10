using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Job.TradesConverter.Core.Services;

namespace Lykke.Job.TradesConverter.Services
{
    [UsedImplicitly]
    public class ShutdownManager : IShutdownManager
    {
        public async Task StopAsync()
        {
            // TODO: Implement your shutdown logic here. Good idea is to log every step

            await Task.CompletedTask;
        }
    }
}
