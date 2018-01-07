using MessagePack;

namespace Lykke.Job.TradesConverter.Contract
{
    [MessagePackObject]
    public class TradeLogItemFee
    {
        [Key(0)]
        public string Type { get; set; }

        [Key(1)]
        public string SourceClientId { get; set; }

        [Key(2)]
        public string TargetClientId { get; set; }

        [Key(3)]
        public string SizeType { get; set; }

        [Key(4)]
        public double? Size { get; set; }
    }
}
