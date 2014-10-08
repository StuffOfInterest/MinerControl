using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using MinerControl.PriceEntries;
using MinerControl.Utility;

namespace MinerControl.Services
{
    public abstract class ServiceBase<TEntry> : PropertyChangedBase, IService
        where TEntry : PriceEntryBase, new()
    {
        private ServiceEnum _serviceEnum;
        private DateTime? _lastUpdated;
        private decimal _balance;

        public MiningEngine MiningEngine { get; set; }
        public ServiceEnum ServiceEnum { get { return _serviceEnum; } protected set { SetField(ref _serviceEnum, value, () => ServiceEnum, () => ServicePrint); } }
        public DateTime? LastUpdated { get { return _lastUpdated; } protected set { SetField(ref _lastUpdated, value, () => LastUpdated, () => LastUpdatedPrint); } }
        public decimal Balance { get { return _balance; } protected set { SetField(ref _balance, value, () => Balance, () => BalancePrint, () => CurrencyPrint); } }
        public decimal Currency { get { return Balance * MiningEngine.Exchange; } }

        public virtual string ServicePrint { get { return ServiceEnum.ToString(); } }
        public string LastUpdatedPrint { get { return LastUpdated == null ? string.Empty : LastUpdated.Value.ToString("HH:mm:ss"); } }
        public string BalancePrint { get { return Balance == 0.0m ? string.Empty : Balance.ToString("N8"); } }
        public string CurrencyPrint { get { return Currency == 0.0m ? string.Empty : Currency.ToString("N4"); } }
        public string TimeMiningPrint
        {
            get
            {
                var seconds = PriceEntries.Sum(o => o.TimeMiningWithCurrent.TotalSeconds);
                return TimeSpan.FromSeconds(seconds).FormatTime(true);
            }
        }

        public ServiceBase()
        {
            DonationAccount = string.Empty;
            DonationWorker = string.Empty;
        }

        public void UpdateTime()
        {
            OnPropertyChanged(() => TimeMiningPrint);
        }

        public void UpdateExchange()
        {
            OnPropertyChanged(() => CurrencyPrint);
        }

        protected IList<TEntry> PriceEntries { get { return MiningEngine.PriceEntries.Where(o => o.ServiceEntry.ServiceEnum == ServiceEnum).Select(o => (TEntry)o).ToList(); } }

        protected string _account;
        protected string _worker;
        protected string _param1;
        protected string _param2;
        protected string _param3;
        protected decimal _weight = 1.0m;
        protected string DonationAccount { get; set;}
        protected string DonationWorker { get; set; }
        protected IDictionary<string, string> AlgoTranslations { get; set; }

        public abstract void Initialize(IDictionary<string, object> data);
        public abstract void CheckPrices();

        protected void ExtractCommon(IDictionary<string, object> data)
        {
            _account = data.GetString("account") ?? string.Empty;
            _worker = data.GetString("worker") ?? string.Empty;
            if (data.ContainsKey("weight"))
                _weight = data["weight"].ExtractDecimal();
            _param1 = data.GetString("param1") ?? string.Empty;
            _param2 = data.GetString("param2") ?? string.Empty;
            _param3 = data.GetString("param3") ?? string.Empty;
        }

        protected string ProcessedSubstitutions(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            return raw
                .Replace("_ACCOUNT_", _account)
                .Replace("_WORKER_", _worker)
                .Replace("_PARAM1_", _param1)
                .Replace("_PARAM2_", _param2)
                .Replace("_PARAM3_", _param3);
        }

        protected string ProcessedDonationSubstitutions(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            return raw
                .Replace("_ACCOUNT_", DonationAccount)
                .Replace("_WORKER_", DonationWorker)
                .Replace("_PARAM1_", _param1)
                .Replace("_PARAM2_", _param2)
                .Replace("_PARAM3_", _param3);
        }

        protected TEntry GetEntry(Dictionary<string, object> item)
        {
            var entry = new TEntry();
            entry.MiningEngine = MiningEngine;
            entry.ServiceEntry = this;

            entry.AlgoName = item.GetString("algo");
            var algo = MiningEngine.AlgorithmEntries.Single(o => o.Name == entry.AlgoName);
            entry.Hashrate = algo.Hashrate;
            entry.Power = algo.Power;
            entry.Weight = _weight;
            entry.Folder = ProcessedSubstitutions(item.GetString("folder")) ?? string.Empty;
            entry.Command = ProcessedSubstitutions(item.GetString("command"));
            entry.Arguments = ProcessedSubstitutions(item.GetString("arguments")) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(DonationAccount))
            {
                entry.DonationFolder = ProcessedDonationSubstitutions(item.GetString("folder")) ?? string.Empty;
                entry.DonationCommand = ProcessedDonationSubstitutions(item.GetString("command"));
                entry.DonationArguments = ProcessedDonationSubstitutions(item.GetString("arguments")) ?? string.Empty;
            }

            return entry;
        }

        protected void Add(TEntry entry)
        {
            MiningEngine.PriceEntries.Add(entry);
        }

        protected static void LaunchChecker(string url, DownloadStringCompletedEventHandler complete)
        {
            try
            {
                using (var client = new WebClient())
                {
                    var uri = new Uri(url);
                    client.DownloadStringCompleted += complete;
                    client.DownloadStringAsync(uri);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
            }
        }

        protected string GetAlgoName(string name)
        {
            if (AlgoTranslations == null || !AlgoTranslations.ContainsKey(name)) return name;
            return AlgoTranslations[name];
        }
    }
}
