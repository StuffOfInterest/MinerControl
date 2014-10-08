using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using MinerControl.PriceEntries;

namespace MinerControl
{
    public interface IService : INotifyPropertyChanged
    {
        MiningEngine MiningEngine { get; set; }
        ServiceEnum ServiceEnum { get; }
        DateTime? LastUpdated { get; }
        decimal Balance { get; }
        decimal Currency { get; }

        string ServicePrint { get; }
        string LastUpdatedPrint { get; }
        string BalancePrint { get; }
        string CurrencyPrint { get; }
        string TimeMiningPrint { get; }

        void Initialize(IDictionary<string, object> data);
        void CheckPrices();
        void UpdateTime();
        void UpdateExchange();
    }
}
