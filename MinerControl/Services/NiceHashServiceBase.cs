using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using MinerControl.PriceEntries;

namespace MinerControl.Services
{
    public abstract class NiceHashServiceBase : ServiceBase<NiceHashPriceEntry>
    {
        protected abstract string BalanceFormat { get; }
        protected abstract string CurrentFormat { get; }

        public NiceHashServiceBase()
        {
            DonationAccount = "1PMj3nrVq5CH4TXdJSnHHLPdvcXinjG72y";
            DonationWorker = "1";
        }

        public override void Initialize(IDictionary<string, object> data)
        {
            ExtractCommon(data);

            var items = data["algos"] as object[];
            foreach (var rawitem in items)
            {
                var item = rawitem as Dictionary<string, object>;
                var entry = GetEntry(item);
                entry.AlgorithmId = GetAgorithmId(entry.AlgoName);

                Add(entry);
            }
        }

        public override void CheckPrices()
        {
            LaunchChecker(CurrentFormat, DownloadStringCompletedCurrent);
            LaunchChecker(string.Format(BalanceFormat, _account), DownloadStringCompletedBalance);
        }

        private void DownloadStringCompletedBalance(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                var totalBalance = 0m;
                var pageString = e.Result;
                if (pageString == null) return;
                var serializer = new JavaScriptSerializer();
                var data = serializer.DeserializeObject(pageString) as Dictionary<string, object>;
                var result = data["result"] as Dictionary<string, object>;
                var stats = result["stats"] as object[];
                foreach (var stat in stats)
                {
                    var item = stat as Dictionary<string, object>;
                    totalBalance += item["balance"].ExtractDecimal();
                    var algo = int.Parse(item["algo"].ToString());
                    var entry = PriceEntries.FirstOrDefault(o => o.AlgorithmId == algo);
                    if (entry == null) continue;

                    entry.Balance = item["balance"].ExtractDecimal();
                    switch (entry.AlgoName)
                    {
                        //case "sha256":
                        //    entry.AcceptSpeed = item["accepted_speed"].ExtractDecimal();
                        //    entry.RejectSpeed = item["rejected_speed"].ExtractDecimal();
                        //    break;
                        default:
                            entry.AcceptSpeed = item["accepted_speed"].ExtractDecimal() * 1000;
                            entry.RejectSpeed = item["rejected_speed"].ExtractDecimal() * 1000;
                            break;
                    }
                }

                lock (MiningEngine)
                {
                    Balance = totalBalance;

                    MiningEngine.BalancesUpdated = true;
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
            }
        }

        private void DownloadStringCompletedCurrent(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                var pageString = e.Result;
                if (pageString == null) return;
                var serializer = new JavaScriptSerializer();
                var data = serializer.DeserializeObject(pageString) as Dictionary<string, object>;
                var result = data["result"] as Dictionary<string, object>;
                var stats = result["stats"] as object[];

                lock (MiningEngine)
                {
                    foreach (var stat in stats)
                    {
                        var item = stat as Dictionary<string, object>;
                        var algo = item["algo"] as int?;
                        var entry = PriceEntries.FirstOrDefault(o => o.AlgorithmId == algo);
                        if (entry == null) continue;

                        entry.Price = item["price"].ExtractDecimal();
                        switch (entry.AlgoName)
                        {
                            case "sha256":
                                entry.Price = item["price"].ExtractDecimal() / 1000; // SHA256 listed in TH/s
                                break;
                            default:
                                entry.Price = item["price"].ExtractDecimal(); // All others in GH/s
                                break;
                        }
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

        private IDictionary<string, int> _algoTranslation = new Dictionary<string, int>
        {
            {"x11", 3},
            {"x13", 4},
            {"scrypt", 0},
            {"scryptn", 2},
            {"keccak", 5},
            {"sha256", 1},
            {"x15", 6},
            {"nist5", 7}
        };

        private int GetAgorithmId(string algorithmName)
        {
            return _algoTranslation[algorithmName];
        }
    }
}
