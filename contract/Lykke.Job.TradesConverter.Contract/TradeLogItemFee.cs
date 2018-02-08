using System;
using MessagePack;

namespace Lykke.Job.TradesConverter.Contract
{
    [MessagePackObject(keyAsPropertyName:true)]
    public class TradeLogItemFee
    {
        public string FromClientId { get; set; }

        public string ToClientId { get; set; }

        public DateTime DateTime { get; set; }

        public double Volume { get; set; }

        public string Asset { get; set; }

        public string Type { get; set; }

        public string SizeType { get; set; }

        public double? Size { get; set; }
    }
}
