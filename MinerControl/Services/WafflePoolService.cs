using System;
using System.Collections.Generic;
using System.Linq;
using MinerControl.PriceEntries;
using MinerControl.Utility;

namespace MinerControl.Services
{
    public class WafflePoolService : ServiceBase<WafflePoolPriceEntry>
    {
        // http://wafflepool.com/api/stats
        // {"scrypt":{
        //    "mining":"hidden",
        //    "hashrate":22229349799,
        //    "last_hour":{"aricoin":1.69,"feathercoin":20.61,"litecoin":31.07,"litecoindark":2.32,"megacoin":6.69,"monacoin":3.19,"nautiluscoin":5.7,"noblecoin":4.56,"potcoin":3.29,"tagcoin":3.44,"usde":3.79,"viacoin":13.66},
        //    "balances":{"sent":15957.24412827,"exchanged":5.5655921,"confirmed":2.3906955,"unconfirmed":1.49996499},
        //    "earnings":[
        //      {"date":"2014-10-06","earned":3.65661792,"hashrate":22652460350,"permhs":0.00036595,"vsltc":116.02},
        //      {"date":"2014-10-05","earned":7.3391845,"hashrate":22683413258,"permhs":0.00032355,"vsltc":100.25},
        //      {"date":"2014-10-04","earned":8.25865688,"hashrate":20843006208,"permhs":0.00039623,"vsltc":123.13},
        //      {"date":"2014-10-03","earned":6.38886382,"hashrate":16476333450,"permhs":0.00038776,"vsltc":121.41},
        //      {"date":"2014-10-02","earned":6.67626292,"hashrate":22812053995,"permhs":0.00029266,"vsltc":89.81},
        //      {"date":"2014-10-01","earned":7.99719669,"hashrate":11478782808,"permhs":0.00069669,"vsltc":212.72},
        //      {"date":"2014-09-30","earned":5.90335632,"hashrate":15462069191,"permhs":0.0003818,"vsltc":117.91}]},
        //  "nscrypt":{
        //    "mining":"murraycoin","hashrate":159737729,"last_hour":{"murraycoin":2.61,"spaincoin":3.52,"vertcoin":93.87},"balances":{"sent":156.60008804,"exchanged":0.15470803,"confirmed":0,"unconfirmed":0.04396441},"earnings":[{"date":"2014-10-06","earned":0.08699461,"hashrate":158068034,"permhs":0.0012477,"vsltc":185.92},{"date":"2014-10-05","earned":0.23894878,"hashrate":171170636,"permhs":0.00139597,"vsltc":203.28},{"date":"2014-10-04","earned":0.30048782,"hashrate":198166747,"permhs":0.00151634,"vsltc":221.46},{"date":"2014-10-03","earned":0.34351864,"hashrate":204521631,"permhs":0.00167962,"vsltc":247.16},{"date":"2014-10-02","earned":0.46233947,"hashrate":240756592,"permhs":0.00192036,"vsltc":276.99},{"date":"2014-10-01","earned":0.38214958,"hashrate":248980112,"permhs":0.00153486,"vsltc":220.26},{"date":"2014-09-30","earned":0.42550327,"hashrate":254810422,"permhs":0.00166988,"vsltc":242.38}]},
        //  "x11":{
        //    "mining":"startcoinv2","hashrate":18529509790,"last_hour":{"cannabiscoin":5.93,"cryptcoin":0.21,"darkcoin":8.47,"fractalcoin":0.13,"glyphcoin":0.19,"startcoinv2":81.72,"urocoin":3.19,"virtualcoin":0.15},"balances":{"sent":1058.00456076,"exchanged":5.22549376,"confirmed":0.02933664,"unconfirmed":0.08405081},"earnings":[{"date":"2014-10-06","earned":4.3002266,"hashrate":17715027860,"permhs":0.00055032,"vsltc":697.88},{"date":"2014-10-05","earned":8.16684463,"hashrate":16683302480,"permhs":0.00048952,"vsltc":606.68},{"date":"2014-10-04","earned":6.19759441,"hashrate":15926108606,"permhs":0.00038915,"vsltc":483.71},{"date":"2014-10-03","earned":5.38855032,"hashrate":15966248607,"permhs":0.0003375,"vsltc":422.67},{"date":"2014-10-02","earned":3.92883941,"hashrate":14658970986,"permhs":0.00026802,"vsltc":329},{"date":"2014-10-01","earned":2.82580133,"hashrate":12714976659,"permhs":0.00022224,"vsltc":271.43},{"date":"2014-09-30","earned":3.05441985,"hashrate":14278611823,"permhs":0.00021392,"vsltc":264.25}]},
        //  "x13":{
        //    "mining":null,"hashrate":10995107,"last_hour":[],"balances":{"sent":138.95795525,"exchanged":0.04238868,"confirmed":0,"unconfirmed":0},"earnings":[{"date":"2014-10-06","earned":0,"hashrate":0,"permhs":0,"vsltc":0},{"date":"2014-10-05","earned":0,"hashrate":0,"permhs":0,"vsltc":0},{"date":"2014-10-04","earned":0,"hashrate":0,"permhs":0,"vsltc":0},{"date":"2014-10-03","earned":0,"hashrate":0,"permhs":0,"vsltc":0},{"date":"2014-10-02","earned":0,"hashrate":0,"permhs":0,"vsltc":0},{"date":"2014-10-01","earned":0,"hashrate":0,"permhs":0,"vsltc":0},{"date":"2014-09-30","earned":0,"hashrate":0,"permhs":0,"vsltc":0}]}}

        // http://wafflepool.com/api/miner?address=1PMj3nrVq5CH4TXdJSnHHLPdvcXinjG72y
        // {"scrypt":{"hashrate":0,"hashrate_str":"0.00 kH\/s","stalerate":0,"stalerate_str":"0.00 kH\/s","workers":[],"balances":{"sent":0,"confirmed":0,"unconverted":0}},
        //  "nscrypt":{"hashrate":0,"hashrate_str":"0.00 kH\/s","stalerate":0,"stalerate_str":"0.00 kH\/s","workers":[],"balances":{"sent":0,"confirmed":0,"unconverted":0}},
        //  "x11":{
        //    "hashrate":1215199,"hashrate_str":"1.22 MH\/s","stalerate":0,"stalerate_str":"0.00 kH\/s",
        //    "workers":{"1PMj3nrVq5CH4TXdJSnHHLPdvcXinjG72y":{"hashrate":1215199,"stalerate":0,"str":"1.22 MH\/s","last_seen":1412633470}},
        //    "balances":{"sent":0,"confirmed":0,"unconverted":0}},
        //  "x13":{"hashrate":0,"hashrate_str":"0.00 kH\/s","stalerate":0,"stalerate_str":"0.00 kH\/s","workers":[],"balances":{"sent":0,"confirmed":0,"unconverted":0}}}

        public WafflePoolService()
        {
            ServiceEnum = ServiceEnum.WafflePool;
            DonationAccount = "1PMj3nrVq5CH4TXdJSnHHLPdvcXinjG72y";
            DonationWorker = "1";

            AlgoTranslations = new Dictionary<string, string>                
                {
                    {"nscrypt", "scryptn"}
                };
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
            WebUtil.DownloadJson("http://wafflepool.com/api/stats", ProcessPrices);
            WebUtil.DownloadJson(string.Format("http://wafflepool.com/api/miner?address={0}", _account), ProcessBalances);
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
                    var algo = GetAlgoName(key.ToLower());

                    var entry = PriceEntries.FirstOrDefault(o => o.AlgoName == algo);
                    if (entry == null) continue;

                    var earnings = item["earnings"] as object[];
                    var earning = earnings[0] as Dictionary<string, object>;

                    entry.Price = earning["permhs"].ExtractDecimal() * 1000;
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
                foreach (var entry in PriceEntries)
                {
                    entry.Balance = 0;
                    entry.AcceptSpeed = 0;
                    entry.RejectSpeed = 0;
                }

                foreach (var key in data.Keys)
                {
                    var rawitem = data[key];
                    var item = rawitem as Dictionary<string, object>;
                    var algo = GetAlgoName(key.ToLower());

                    var entry = PriceEntries.FirstOrDefault(o => o.AlgoName == algo);
                    if (entry == null) continue;

                    entry.AcceptSpeed = item["hashrate"].ExtractDecimal() / 1000000;
                    entry.RejectSpeed = item["stalerate"].ExtractDecimal() / 1000000;

                    var balances = item["balances"] as Dictionary<string, object>;
                    entry.Balance = balances["confirmed"].ExtractDecimal() + balances["unconverted"].ExtractDecimal();
                }

                Balance = PriceEntries.Select(o => o.Balance).Sum();

                MiningEngine.PricesUpdated = true;
            }
        }
    }
}
