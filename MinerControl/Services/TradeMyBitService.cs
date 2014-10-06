using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using MinerControl.PriceEntries;

namespace MinerControl.Services
{
    public class TradeMyBitService : ServiceBase<TradeMyBitPriceEntry>
    {
        // https://pool.trademybit.com/api/bestalgo?key=[key]
        // [{"algo":"x11","score":"0.00136681","actual":"0.000248511"},{"algo":"nscrypt","score":"0.00111675","actual":"0.00237607"},{"algo":"scrypt","score":"0.000443541","actual":"0.000443541"},{"algo":"nist5","score":"0.0000663135","actual":"0.000004019"},{"algo":"x13","score":"0.0000659915","actual":"0.0000178355"}]
        
        // https://pool.trademybit.com/api/balance?key=[key]
        // {"autoexchange":{"est_total":"0.00130840","unexchanged":"0.00000000","exchanged":"0.00130840","alltime":"0.00000000"}}

        public TradeMyBitService()
        {
            ServiceEnum = ServiceEnum.TradeMyBit;
            DonationAccount = "MinerControl";
            DonationWorker = "1";
        }

        public string _apikey;

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
            LaunchChecker(string.Format("https://pool.trademybit.com/api/bestalgo?key={0}", _apikey), DownloadStringAlgoCompleted);
            LaunchChecker(string.Format("https://pool.trademybit.com/api/balance?key={0}", _apikey), DownloadStringBalanceCompleted);
        }

        private void DownloadStringAlgoCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                var pageString = e.Result;
                if (pageString == null) return;
                var serializer = new JavaScriptSerializer();
                var data = serializer.DeserializeObject(pageString) as object[];
                lock (MiningEngine)
                {
                    foreach (var rawitem in data)
                    {
                        var item = rawitem as Dictionary<string, object>;
                        var algo = item.GetString("algo").ToLower();
                        var translatedName = GetAlgoName(algo);
                        if (translatedName == null) continue;

                        var entry = PriceEntries.FirstOrDefault(o => o.AlgoName == translatedName);
                        if (entry == null) continue;

                        entry.Price = item["actual"].ExtractDecimal() * 1000;
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

        private void DownloadStringBalanceCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                var pageString = e.Result;
                if (pageString == null) return;
                var serializer = new JavaScriptSerializer();
                var data = serializer.DeserializeObject(pageString) as Dictionary<string, object>;
                var balances = data["autoexchange"] as Dictionary<string, object>;

                lock (MiningEngine)
                {
                    Balance = balances["est_total"].ExtractDecimal();

                    MiningEngine.BalancesUpdated = true;
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
            }
        }

        private IDictionary<string, string> _algoTranslations = new Dictionary<string, string>
        {
            {"x11", "x11"},
            {"x13", "x13"},
            {"nscrypt", "scryptn"},
            {"scrypt", "scrypt"},
            {"nist5", "nist5"},
            {"x15", "x15"}
        };

        private string GetAlgoName(string name)
        {
            if (!_algoTranslations.ContainsKey(name)) return null;
            return _algoTranslations[name];
        }
    }
}
