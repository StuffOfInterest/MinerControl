using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using MinerControl.PriceEntries;

namespace MinerControl.Services
{
    public class YaampService : ServiceBase<YaampPriceEntry>
    {
        public YaampService()
        {
            ServiceEnum = ServiceEnum.YAAMP;
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
            LaunchChecker("http://yaamp.com/api/status", DownloadStringCompletedPrice);
            LaunchChecker(string.Format("http://yaamp.com/api/wallet?address={0}", _account), DownloadStringCompletedBalance);
        }

        private void DownloadStringCompletedPrice(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                var pageString = e.Result;
                if (pageString == null) return;
                var serializer = new JavaScriptSerializer();
                var data = serializer.DeserializeObject(pageString) as Dictionary<string, object>;
                lock (MiningEngine)
                {
                    foreach (var key in data.Keys)
                    {
                        var rawitem = data[key];
                        var item = rawitem as Dictionary<string, object>;
                        var algo = key.ToLower();

                        var entry = PriceEntries.FirstOrDefault(o => o.AlgoName == algo);
                        if (entry == null) continue;

                        entry.Price = item["estimate_current"].ExtractDecimal() * 1000;
                        entry.FeePercent = item["fees"].ExtractDecimal();
                    }

                    MiningEngine.PricesUpdated = true;
                    MiningEngine.HasPrices = true;

                    LastUpdated = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
            }
        }

        private void DownloadStringCompletedBalance(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                var pageString = e.Result;
                if (pageString == null) return;
                var serializer = new JavaScriptSerializer();
                var data = serializer.DeserializeObject(pageString) as Dictionary<string, object>;

                lock (MiningEngine)
                {
                    Balance = data["unpaid"].ExtractDecimal();

                    MiningEngine.BalancesUpdated = true;

                    foreach (var entry in PriceEntries)
                        entry.AcceptSpeed = 0;

                    if (!data.ContainsKey("miners")) return;
                    var miners = data["miners"] as Dictionary<string, object>;
                    foreach (var key in miners.Keys)
                    {
                        var entry = PriceEntries.Where(o => o.AlgoName == key).FirstOrDefault();
                        if (entry == null) continue;
                        var item = miners[key] as Dictionary<string, object>;
                        entry.AcceptSpeed = item["hashrate"].ExtractDecimal() / 1000000;
                    }

                    MiningEngine.PricesUpdated = true;
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
            }
        }
    }
}
