﻿namespace Lykke.Job.TradesConverter.Core.IncomingMessages
{
    public class Fee
    {
        public FeeInstruction Instruction { get; set; }

        public FeeTransfer Transfer { get; set; }
    }
}
