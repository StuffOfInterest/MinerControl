using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinerControl.PriceEntries;

namespace MinerControl.Services
{
    public class WestHashService : NiceHashServiceBase
    {
        public WestHashService()
        {
            ServiceEnum = ServiceEnum.WestHash;
        }

        protected override string BalanceFormat { get { return "https://www.westhash.com/api?method=stats.provider&addr={0}"; } }
        protected override string CurrentFormat { get { return "https://www.westhash.com/api?method=stats.global.current"; } }
    }
}
