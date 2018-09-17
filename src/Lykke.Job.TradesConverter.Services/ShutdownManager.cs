using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Common;
using Common.Log;
using Lykke.Job.TradesConverter.Core.Services;

namespace Lykke.Job.TradesConverter.Services
{
    [UsedImplicitly]
    public class ShutdownManager : IShutdownManager
    {
        private readonly ILog _log;
        private readonly List<IStopable> _items = new List<IStopable>();
        private readonly List<IStartStop> _stopables = new List<IStartStop>();

        public ShutdownManager(
            ILog log,
            IEnumerable<IStopable> items,
            IEnumerable<IStartStop> stopables)
        {
            _log = log;
            _items.AddRange(items);
            _stopables.AddRange(stopables);
        }

        public Task StopAsync()
        {
            Parallel.ForEach(_stopables, i =>
            {
                try
                {
                    i.Stop();
                }
                catch (Exception ex)
                {
                    _log.WriteWarning(nameof(StopAsync), null, $"Unable to stop {i.GetType().Name}", ex);
                }
            });

            Parallel.ForEach(_items, i =>
            {
                try
                {
                    i.Stop();
                }
                catch (Exception ex)
                {
                    _log.WriteWarning(nameof(StopAsync), null, $"Unable to stop {i.GetType().Name}", ex);
                }
            });

            return Task.CompletedTask;
        }
    }
}
