using System;
using System.Collections.Generic;
using System.Linq;
using MinerControl.PriceEntries;
using MinerControl.Utility;

namespace MinerControl.Services
{
    public class YaampService : ServiceBase<YaampPriceEntry>
    {
        // http://yaamp.com/api/status
        // {
        //   "scrypt": {"name": "scrypt", "port": 3433, "coins": 21, "fees": 0.5, "hashrate": 1585708947, "estimate_current": 0.00017441, "estimate_last24h": 0.00018214, "actual_last24h": 0.00019935}, 
        //   "scryptn": {"name": "scryptn", "port": 4333, "coins": 3, "fees": 3.2, "hashrate": 88775354, "estimate_current": 0.00042183, "estimate_last24h": 0.00045701, "actual_last24h": 0.00039583}, 
        //   "neoscrypt": {"name": "neoscrypt", "port": 4233, "coins": 4, "fees": 2.8, "hashrate": 359254951, "estimate_current": 0.00217158, "estimate_last24h": 0.00352892, "actual_last24h": 0.00320471}, 
        //   "quark": {"name": "quark", "port": 4033, "coins": 3, "fees": 0.5, "hashrate": 85473584, "estimate_current": 0.00000078, "estimate_last24h": 0.00000087, "actual_last24h": 0.00020362}, 
        //   "lyra2": {"name": "lyra2", "port": 4433, "coins": 1, "fees": 0.6, "hashrate": 16864909, "estimate_current": 0.00062095, "estimate_last24h": 0.00063257, "actual_last24h": 0.00090811}, 
        //   "x11": {"name": "x11", "port": 3533, "coins": 10, "fees": 1.3, "hashrate": 7793602368, "estimate_current": 0.00019716, "estimate_last24h": 0.00015537, "actual_last24h": 0.00016760}, 
        //   "x13": {"name": "x13", "port": 3633, "coins": 2, "fees": 3.7, "hashrate": 2500273917, "estimate_current": 0.00019537, "estimate_last24h": 0.00016803, "actual_last24h": 0.00017710}, 
        //   "x14": {"name": "x14", "port": 3933, "coins": 1, "fees": 3.5, "hashrate": 54660672, "estimate_current": 0.00032863, "estimate_last24h": 0.00025539, "actual_last24h": 0.00025089}, 
        //   "x15": {"name": "x15", "port": 3733, "coins": 3, "fees": 5.4, "hashrate": 5870269795, "estimate_current": 0.00126368, "estimate_last24h": 0.00025544, "actual_last24h": 0.00082305}
        // }

        private int _priceMode;

        public YaampService()
        {
            ServiceEnum = ServiceEnum.YAAMP;
            DonationAccount = "1PMj3nrVq5CH4TXdJSnHHLPdvcXinjG72y";
        }
        
        public override void Initialize(IDictionary<string, object> data)
        {
            ExtractCommon(data);

            if (data.ContainsKey("pricemode"))
                _priceMode = int.Parse(data["pricemode"].ToString());

            var items = data["algos"] as object[];
            foreach (var rawitem in items)
            {
                var item = rawitem as Dictionary<string, object>;
                var entry = CreateEntry(item);

                Add(entry);
            }
        }

        public override void CheckPrices()
        {
            ClearStalePrices();
            WebUtil.DownloadJson("http://yaamp.com/api/status", ProcessPrices);
            WebUtil.DownloadJson(string.Format("http://yaamp.com/api/wallet?address={0}", _account), ProcessBalances);
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

                    var entry = GetEntry(algo);
                    if (entry == null) continue;

                    switch (_priceMode)
                    {
                        case 1:
                            entry.Price = item["estimate_last24h"].ExtractDecimal() * 1000;
                            break;
                        case 2:
                            entry.Price = item["actual_last24h"].ExtractDecimal() * 1000;
                            break;
                        default:
                            entry.Price = item["estimate_current"].ExtractDecimal() * 1000;
                            break;
                    }
                    entry.FeePercent = item["fees"].ExtractDecimal();
                }

                MiningEngine.PricesUpdated = true;
                MiningEngine.HasPrices = true;

                LastUpdated = DateTime.Now;
            }
        }

        private void ProcessBalances(object jsonData)
        {
            var data = jsonData as Dictionary<string, object>;

            lock (MiningEngine)
            {
                Balance = data["unpaid"].ExtractDecimal();

                foreach (var entry in PriceEntries)
                    entry.AcceptSpeed = 0;

                if (!data.ContainsKey("miners")) return;
                var miners = data["miners"] as Dictionary<string, object>;
                foreach (var key in miners.Keys)
                {
                    var entry = GetEntry(key.ToLower());
                    if (entry == null) continue;
                    var item = miners[key] as Dictionary<string, object>;
                    entry.AcceptSpeed = item["hashrate"].ExtractDecimal() / 1000000;
                }

                MiningEngine.PricesUpdated = true;
            }
        }
    }
}
