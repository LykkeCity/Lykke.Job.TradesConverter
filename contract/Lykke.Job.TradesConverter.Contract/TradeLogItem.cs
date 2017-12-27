using System;
using MessagePack;

namespace Lykke.Job.TradesConverter.Contract
{
    [MessagePackObject]
    public class TradeLogItem
    {
        [Key(0)]
        public long Id { get; set; }

        [Key(1)]
        public string TradeId { get; set; }

        [Key(2)]
        public string UserId { get; set; }

        [Key(3)]
        public string WalletId { get; set; }

        [Key(4)]
        public string OrderId { get; set; }

        [Key(5)]
        public string OrderType { get; set; }

        [Key(6)]
        public string Direction { get; set; }

        [Key(7)]
        public string Asset { get; set; }

        [Key(8)]
        public decimal Volume { get; set; }

        [Key(9)]
        public decimal Price { get; set; }

        [Key(10)]
        public DateTime DateTime { get; set; }

        [Key(11)]
        public string OppositeOrderId { get; set; }

        [Key(12)]
        public string OppositeAsset { get; set; }

        [Key(13)]
        public decimal? OppositeVolume { get; set; }

        [Key(14)]
        public bool? IsHidden { get; set; }
    }
}
