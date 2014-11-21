using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinerControl.PriceEntries;
using MinerControl.Utility;

namespace MinerControl.Services
{
    public class WePayBtcService : ServiceBase<WePayBtcPriceEntry>
    {
        // http://wepaybtc.com/payouts.json

        //{
        //    "reference": 0.001,
        //    "x11": 0.0002066,
        //    "x13": 0.0002985,
        //    "x15": 0.0003717,
        //    "nist5": 0.00006863
        //}

        public WePayBtcService()
        {
            ServiceEnum = ServiceEnum.WePayBTC;
            DonationAccount = "1PMj3nrVq5CH4TXdJSnHHLPdvcXinjG72y";
        }

        public override void Initialize(IDictionary<string, object> data)
        {
            ExtractCommon(data);

            var items = data["algos"] as object[];
            foreach (var rawitem in items)
            {
                var item = rawitem as Dictionary<string, object>;
                var entry = GetEntry(item);

                Add(entry);
            }
        }

        public override void CheckPrices()
        {
            ClearStalePrices();
            WebUtil.DownloadJson("http://wepaybtc.com/payouts.json", ProcessPrices);
        }
        
        private void ProcessPrices(object jsonData)
        {
            var data = jsonData as Dictionary<string, object>;

            lock (MiningEngine)
            {
                foreach (var key in data.Keys)
                {
                    var rawitem = data[key];
                    var item = rawitem as Dictionary<string, object>;
                    var algo = key.ToLower();

                    var entry = PriceEntries.FirstOrDefault(o => o.AlgoName == algo);
                    if (entry == null) continue;

                    entry.Price = data[key].ExtractDecimal() * 1000;
                }

                MiningEngine.PricesUpdated = true;
                MiningEngine.HasPrices = true;

                LastUpdated = DateTime.Now;
            }
        }
    }
}
