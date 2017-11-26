using System.Collections.Generic;

namespace Lykke.Job.TradesConverter.Core.IncomingMessages
{
    public class MarketOrderWithTrades
    {
        public MarketOrder Order { get; set; }

        public List<TradeInfo> Trades { get; set; }
    }
}
