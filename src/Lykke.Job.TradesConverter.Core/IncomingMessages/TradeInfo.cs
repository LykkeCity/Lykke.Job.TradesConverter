﻿using System;

namespace Lykke.Job.TradesConverter.Core.IncomingMessages
{
    public class TradeInfo
    {
        public string MarketOrderId { get; set; }

        public string MarketClientId { get; set; }

        public double MarketVolume { get; set; }

        public string MarketAsset { get; set; }

        public double Price { get; set; }

        public string LimitClientId { get; set; }

        public double LimitVolume { get; set; }

        public string LimitAsset { get; set; }

        public string LimitOrderId { get; set; }

        public string LimitOrderExternalId { get; set; }

        public DateTime Timestamp { get; set; }

        public FeeInstruction FeeInstruction { get; set; }
    }
}
