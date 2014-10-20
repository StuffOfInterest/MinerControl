using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinerControl.PriceEntries;
using MinerControl.Utility;

namespace MinerControl.Services
{
    public class LtcRabbitService : ServiceBase<LtcRabbitPriceEntry>
    {
        // https://www.ltcrabbit.com/index.php?page=api&action=public
        // {"hashrate":2394110,"hashrate_scrypt":2394110,"hashrate_x11":3971072,"workers":1765,"ltc_mh_scrypt":"0.0257857","ltc_mh_x11":"0.0328172",
        //  "profitability":
        //    {"current":{
        //        "scrypt":{"ltc_mh":"0.0257857","btc_mh":"0.0002651","vs_ltc":"93.60"},
        //        "x11":{"ltc_mh":"0.0328172","btc_mh":"0.0003374","vs_ltc":"476.49"}},
        //     "history":{
        //       "scrypt":[{"date":"2014-10-20","ltc_mh":"0.0171152","btc_mh":"0.0001757","vs_ltc":"62.08"},<snip>],
        //       "x11":[{"date":"2014-10-20","ltc_mh":"0.0303198","btc_mh":"0.0003113","vs_ltc":"439.88"},<snip>]}
        //  }}

        // https://www.ltcrabbit.com/index.php?page=api&action=getappdata&appname=general&appversion=1&api_key=<apikey>
        // {"getappdata":{
        //    "general":{"message":""},
        //    "pool":{"hashrate_scrypt":2271520,"hashrate_x11":3859754,"workers":1783,"ltc_mh_scrypt":"0.0254868","ltc_mh_x11":"0.0323970","hashrate":2271520},
        //    "profitability": <same as public api above>,
        //    "ltc_exchange_rates":{"USD":"3.88","EUR":"3.104"},
        //    "btc_exchange_rates":{"USD":"379.736","EUR":"301.00000"},
        //    "user":{"username":"MinerControl","balance":0,"hashrate_scrypt":0,"hashrate_x11":0,"invalid_shares_scrypt":0,"invalid_shares_x11":0,"sharerate":0,"invalid_share_rate":0,"hashrate":0},
        //    "worker":[],
        //    "earnings":{"basis":[],"alt":[],"24h_total":0,"24h_basis":0,"24h_alt":0,"24h_affiliate":0,"48h_total":0,"48h_basis":0,"48h_alt":0,"48h_affiliate":0}}}

        private string _apikey;

        public LtcRabbitService()
        {
            ServiceEnum = ServiceEnum.LTCRabbit;
            DonationAccount = "MinerControl";
            DonationWorker = "1";
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
            WebUtil.DownloadJson("https://www.ltcrabbit.com/index.php?page=api&action=public", ProcessPrices);
            WebUtil.DownloadJson(string.Format("https://www.ltcrabbit.com/index.php?page=api&action=getappdata&appname=general&appversion=1&api_key={0}", _apikey), ProcessBalances);
        }

        private void ProcessPrices(object jsonData)
        {
            var data = jsonData as Dictionary<string, object>;
            var profitability = data["profitability"] as Dictionary<string, object>;
            var current = profitability["current"] as Dictionary<string, object>;

            lock (MiningEngine)
            {
                foreach (var key in current.Keys)
                {
                    var rawitem = current[key];
                    var item = rawitem as Dictionary<string, object>;
                    var algo = key.ToLower();

                    var entry = PriceEntries.FirstOrDefault(o => o.AlgoName == algo);
                    if (entry == null) continue;

                    entry.Price = item["btc_mh"].ExtractDecimal() * 1000;
                }

                MiningEngine.PricesUpdated = true;
                MiningEngine.HasPrices = true;

                LastUpdated = DateTime.Now;
            }
        }

        private void ProcessBalances(object jsonData)
        {
            var data = jsonData as Dictionary<string, object>;
            var getappdata = data["getappdata"] as Dictionary<string, object>;
            var ltc_exchange_rates = getappdata["ltc_exchange_rates"] as Dictionary<string, object>;
            var btc_exchange_rates = getappdata["btc_exchange_rates"] as Dictionary<string, object>;
            var user = getappdata["user"] as Dictionary<string, object>;
            var ltc_usd = ltc_exchange_rates["USD"].ExtractDecimal();
            var btc_usd = btc_exchange_rates["USD"].ExtractDecimal();
            var balance = user["balance"].ExtractDecimal();

            var exchange_rate = ltc_usd / btc_usd;

            Balance = balance * exchange_rate;

            var entry = PriceEntries.FirstOrDefault(o => o.AlgoName == "x11");
            if (entry != null)
            {
                var hashrate = user["hashrate_x11"].ExtractDecimal();
                entry.AcceptSpeed = hashrate / 1000;
            }

            entry = PriceEntries.FirstOrDefault(o => o.AlgoName == "scrypt");
            if (entry != null)
            {
                var hashrate = user["hashrate_scrypt"].ExtractDecimal();
                entry.AcceptSpeed = hashrate / 1000;
            }
        }
    }
}
