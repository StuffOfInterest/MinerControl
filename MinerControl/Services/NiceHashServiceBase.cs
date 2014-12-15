using System;
using System.Collections.Generic;
using System.Linq;
using MinerControl.PriceEntries;
using MinerControl.Utility;

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
                var entry = CreateEntry(item);
                if (string.IsNullOrWhiteSpace(entry.PriceId))
                    entry.PriceId = GetAgorithmId(entry.AlgoName).ToString();

                Add(entry);
            }
        }

        public override void CheckPrices()
        {
            ClearStalePrices();
            WebUtil.DownloadJson(CurrentFormat, ProcessPrices);
            WebUtil.DownloadJson(string.Format(BalanceFormat, _account), ProcessBalances);
        }

        private void ProcessPrices(object jsonData)
        {
            var data = jsonData as Dictionary<string, object>;
            var result = data["result"] as Dictionary<string, object>;
            var stats = result["stats"] as object[];

            lock (MiningEngine)
            {
                foreach (var stat in stats)
                {
                    var item = stat as Dictionary<string, object>;
                    var algo = item["algo"].ToString();
                    var entry = GetEntry(algo);
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

        private void ProcessBalances(object jsonData)
        {
            var totalBalance = 0m;
            var data = jsonData as Dictionary<string, object>;
            var result = data["result"] as Dictionary<string, object>;
            var stats = result["stats"] as object[];
            foreach (var stat in stats)
            {
                var item = stat as Dictionary<string, object>;
                totalBalance += item["balance"].ExtractDecimal();
                var algo = item.GetString("algo");
                var entry = GetEntry(algo);
                if (entry == null) continue;

                entry.Balance = item["balance"].ExtractDecimal();
                switch (entry.AlgoName)
                {
                    case "sha256":
                        entry.AcceptSpeed = item["accepted_speed"].ExtractDecimal();
                        entry.RejectSpeed = item["rejected_speed"].ExtractDecimal();
                        break;
                    default:
                        entry.AcceptSpeed = item["accepted_speed"].ExtractDecimal() * 1000;
                        entry.RejectSpeed = item["rejected_speed"].ExtractDecimal() * 1000;
                        break;
                }
            }

            lock (MiningEngine)
            {
                Balance = totalBalance;
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
            {"nist5", 7},
            {"neoscrypt", 8}
        };

        private int GetAgorithmId(string algorithmName)
        {
            return _algoTranslation[algorithmName];
        }
    }
}
