using System.Collections.Generic;

namespace Lykke.Job.TradesConverter.Core.IncomingMessages
{
    public class LimitOrderWithTrades
    {
        public LimitOrder Order { get; set; }

        public List<LimitTradeInfo> Trades { get; set; }
    }
}
