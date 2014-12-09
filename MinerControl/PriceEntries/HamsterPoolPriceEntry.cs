using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinerControl.PriceEntries
{
    public class HamsterPoolPriceEntry : PriceEntryBase
    {
        public override decimal Fees
        {
            get { return Earn * (0.015m + Donation); }
            set { base.Fees = value; }
        }

        public decimal Donation { get; set; }
    }
}
