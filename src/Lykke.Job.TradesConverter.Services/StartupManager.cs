using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Autofac;
using Lykke.Job.TradesConverter.Core.Services;

namespace Lykke.Job.TradesConverter.Services
{
    [UsedImplicitly]
    public class StartupManager : IStartupManager
    {
        private readonly List<IStartable> _startables = new List<IStartable>();

        public void Register(IStartable startable)
        {
            _startables.Add(startable);
        }

        public Task StartAsync()
        {
            foreach (var item in _startables)
            {
                item.Start();
            }

            return Task.CompletedTask;
        }
    }
}
