using System.Collections.Generic;

namespace Lykke.Job.TradesConverter.Core.IncomingMessages
{
    public class LimitOrders
    {
        public List<LimitOrderWithTrades> Orders { get; set; }
    }
}
