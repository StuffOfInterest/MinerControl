using System;
using System.Collections.Generic;
using System.Linq;
using MinerControl.PriceEntries;
using MinerControl.Utility;

namespace MinerControl.Services
{
    public class TradeMyBitService : ServiceBase<TradeMyBitPriceEntry>
    {
        // https://pool.trademybit.com/api/bestalgo?key=[key]
        // [{"algo":"x11","score":"0.00136681","actual":"0.000248511"},{"algo":"nscrypt","score":"0.00111675","actual":"0.00237607"},{"algo":"scrypt","score":"0.000443541","actual":"0.000443541"},{"algo":"nist5","score":"0.0000663135","actual":"0.000004019"},{"algo":"x13","score":"0.0000659915","actual":"0.0000178355"}]
        
        // https://pool.trademybit.com/api/balance?key=[key]
        // {"autoexchange":{"est_total":"0.00130840","unexchanged":"0.00000000","exchanged":"0.00130840","alltime":"0.00000000"}}

        private string _apikey;

        public TradeMyBitService()
        {
            ServiceEnum = ServiceEnum.TradeMyBit;
            DonationAccount = "MinerControl";
            DonationWorker = "1";

            AlgoTranslations = new Dictionary<string, string>
                {
                    {"nscrypt", "scryptn"}
                };
        }

        public override void Initialize(IDictionary<string, object> data)
        {
            ExtractCommon(data);
            _apikey = data.GetString("apikey");

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
            WebUtil.DownloadJson(string.Format("https://pool.trademybit.com/api/bestalgo?key={0}", _apikey), ProcessPrices);
            WebUtil.DownloadJson(string.Format("https://pool.trademybit.com/api/balance?key={0}", _apikey), ProcessBalances);
        }

        private void ProcessPrices(object jsonData)
        {
            var data = jsonData as object[];
            lock (MiningEngine)
            {
                foreach (var rawitem in data)
                {
                    var item = rawitem as Dictionary<string, object>;
                    var algo = item.GetString("algo");
                    var entry = PriceEntries.FirstOrDefault(o => o.AlgoName == GetAlgoName(algo));
                    if (entry == null) continue;

                    entry.Price = item["actual"].ExtractDecimal() * 1000;
                }

                MiningEngine.PricesUpdated = true;
                MiningEngine.HasPrices = true;

                LastUpdated = DateTime.Now;
            }
        }

        private void ProcessBalances(object jsonData)
        {
            var data = jsonData as Dictionary<string, object>;
            var balances = data["autoexchange"] as Dictionary<string, object>;

            lock (MiningEngine)
            {
                Balance = balances["est_total"].ExtractDecimal();
            }
        }
    }
}
