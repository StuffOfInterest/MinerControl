using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Script.Serialization;
using MinerControl.PriceEntries;
using MinerControl.Services;
using MinerControl.Utility;

namespace MinerControl
{
    public class MiningEngine
    {
        private Process _process;
        private IList<AlgorithmEntry> _algorithmEntries = new List<AlgorithmEntry>();
        private IList<PriceEntryBase> _priceEntries = new List<PriceEntryBase>();
        private IList<IService> _services = new List<IService>();
        private decimal _powerCost;
        private decimal _exchange;
        private PriceEntryBase _currentRunning;
        private DateTime? _startMining;
        private TimeSpan _minTime;
        private TimeSpan _maxTime;
        private TimeSpan _switchTime;
        private TimeSpan _deadtime;
        private int? _nextRun;                    // Next algo to run
        private DateTime? _nextRunFromTime;       // When the next run algo became best

        public MiningModeEnum MiningMode { get; set; }

        public int MinerKillMode { get; private set; }
        public int GridSortMode { get; private set; }
        public int TrayMode { get; private set; }     // How to handle minimizing ot the tray

        public int? CurrentRunning { get { return _currentRunning == null ? (int?)null : _currentRunning.Id; } }
        public int? NextRun { get { return _nextRun; } }
        public DateTime? StartMining { get { return _startMining; } }
        public decimal PowerCost { get { return _powerCost; } }
        public decimal Exchange { get { return _exchange; } }
        public TimeSpan DeadTime { get { return _deadtime; } }

        public TimeSpan? RestartTime
        {
            get
            {
                return _startMining.HasValue && _maxTime > TimeSpan.Zero
                    ? _maxTime - (DateTime.Now - _startMining.Value)
                    : (TimeSpan?)null;
            }
        }

        public TimeSpan? MiningTime
        {
            get
            {
                return _startMining.HasValue
                    ? DateTime.Now - _startMining.Value
                    : (TimeSpan?)null;
            }
        }

        // How long until next run starts
        public TimeSpan? NextRunTime
        {
            get
            {
                if (_nextRun == null || _nextRunFromTime == null || _startMining == null)
                    return (TimeSpan?)null;

                var timeToSwitch = _switchTime - (DateTime.Now - _nextRunFromTime);
                var timeToMin = _minTime - (DateTime.Now - _startMining);

                return timeToMin > timeToSwitch ? timeToMin : timeToSwitch;
            }
        }

        public IList<IService> Services { get { return _services; } }
        public IList<PriceEntryBase> PriceEntries { get { return _priceEntries; } }       
        public IList<AlgorithmEntry> AlgorithmEntries { get { return _algorithmEntries; } }

        public TimeSpan TotalTime
        {
            get
            {
                var totalTime = PriceEntries.Sum(o => o.TimeMining.TotalMilliseconds);
                if (_startMining.HasValue)
                    totalTime += (DateTime.Now - _startMining.Value).TotalMilliseconds;
                return TimeSpan.FromMilliseconds(totalTime);
            }
        }

        public bool BalancesUpdated { get; set; }
        public bool PricesUpdated { get; set; }
        public bool HasPrices { get; set; }

        #region Donation mining settings

        private double _donationpercentage = 0.02;
        public bool DoDonationMinging { get { return _donationpercentage > 0 && _donationfrequency > TimeSpan.Zero; } }
        private TimeSpan _donationfrequency = TimeSpan.FromMinutes(240);
        private TimeSpan _autoMiningTime = TimeSpan.Zero;
        private TimeSpan _donationMiningTime = TimeSpan.Zero;
        private MiningModeEnum _donationMiningMode = MiningModeEnum.Stopped;

        private TimeSpan MiningBeforeDonation
        {
            get
            {
                if (!DoDonationMinging) return TimeSpan.Zero;
                return TimeSpan.FromMinutes(_donationfrequency.TotalMinutes * (1 - _donationpercentage));
            }
        }

        private TimeSpan MiningDuringDonation
        {
            get
            {
                if (!DoDonationMinging) return TimeSpan.Zero;
                return _donationfrequency - MiningBeforeDonation;
            }
        }

        public TimeSpan TimeUntilDonation
        {
            get
            {
                if (!DoDonationMinging) return TimeSpan.Zero;
                var miningTime = _autoMiningTime;
                if (MiningMode == MiningModeEnum.Automatic && _startMining.HasValue) miningTime += (DateTime.Now - _startMining.Value);
                var value = MiningBeforeDonation - miningTime;
                return value < TimeSpan.Zero ? TimeSpan.Zero : value;
            }
        }

        public TimeSpan TimeDuringDonation
        {
            get
            {
                if (!DoDonationMinging) return TimeSpan.Zero;
                var miningTime = _donationMiningTime;
                if (MiningMode == MiningModeEnum.Donation && _startMining.HasValue) miningTime += (DateTime.Now - _startMining.Value);
                var value = MiningDuringDonation - miningTime;
                return value < TimeSpan.Zero ? TimeSpan.Zero : value;
            }
        }

        #endregion

        public MiningEngine()
        {
            MinerKillMode = 1;
            GridSortMode = 1;
            MiningMode = MiningModeEnum.Stopped;
        }

        public void Cleanup(){
            if (_process != null)
                _process.Dispose();
        }

        public void LoadConfig()
        {
            var pageString = File.ReadAllText("MinerControl.conf");
            var serializer = new JavaScriptSerializer();
            var data = serializer.DeserializeObject(pageString) as Dictionary<string, object>;

            LoadConfigGeneral(data["general"] as Dictionary<string, object>);
            LoadConfigAlgorithms(data["algorithms"] as object[]);

            LoadService(new NiceHashService(), data, "nicehash");
            LoadService(new WestHashService(), data, "westhash");
            LoadService(new TradeMyBitService(), data, "trademybit");
            LoadService(new YaampService(), data, "yaamp");
            LoadService(new ManualService(), data, "manual");

            // Set Id for each entry
            for (var x = 0; x < _priceEntries.Count; x++)
                _priceEntries[x].Id = x + 1;
        }

        private void LoadService(IService service, IDictionary<string, object> data, string name) 
        {
            if (!data.ContainsKey(name)) return;
            service.MiningEngine = this;
            _services.Add(service);
            service.Initialize(data[name] as Dictionary<string, object>);
        }

        private void LoadConfigGeneral(IDictionary<string, object> data)
        {
            _powerCost = data["power"].ExtractDecimal();
            _exchange = data["exchange"].ExtractDecimal();
            _minTime = TimeSpan.FromMinutes((double)data["mintime"].ExtractDecimal());
            _maxTime = TimeSpan.FromMinutes((double)data["maxtime"].ExtractDecimal());
            _switchTime = TimeSpan.FromMinutes((double)data["switchtime"].ExtractDecimal());
            _deadtime = TimeSpan.FromMinutes((double)data["deadtime"].ExtractDecimal());

            if (data.ContainsKey("logerrors"))
                ErrorLogger.LogExceptions = bool.Parse(data["logerrors"].ToString());
            if (data.ContainsKey("minerkillmode"))
                MinerKillMode = int.Parse(data["minerkillmode"].ToString());
            if (data.ContainsKey("gridsortmode"))
                GridSortMode = int.Parse(data["gridsortmode"].ToString());
            if (data.ContainsKey("traymode"))
                TrayMode = int.Parse(data["traymode"].ToString());
            if (Program.MinimizeToTray && TrayMode == 0)
                TrayMode = 2;
            if (data.ContainsKey("donationpercentage"))
                _donationpercentage = (double)(data["donationpercentage"].ExtractDecimal()) / 100;
            if (data.ContainsKey("donationfrequency"))
                _donationfrequency = TimeSpan.FromMinutes((double)data["donationfrequency"].ExtractDecimal());
        }

        private void LoadConfigAlgorithms(object[] data)
        {
            foreach (var rawitem in data)
            {
                var item = rawitem as Dictionary<string, object>;
                var entry = new AlgorithmEntry
                {
                    Name = item["name"] as string,
                    Hashrate = item["hashrate"].ExtractDecimal(),
                    Power = item["power"].ExtractDecimal()
                };

                _algorithmEntries.Add(entry);
            }
        }

        public void StopMiner()
        {
            if (_process == null || _process.HasExited) return;

            RecordMiningTime();
            if (MinerKillMode == 0)
                ProcessUtil.KillProcess(_process);
            else
                ProcessUtil.KillProcessAndChildren(_process.Id);

            _process = null;
            _donationMiningMode = MiningModeEnum.Stopped;

            if (_currentRunning != null)
            {
                var entry = PriceEntries.Where(o => o.Id == _currentRunning.Id).Single();
                entry.UpdateStatus();
            }

            _currentRunning = null;
        }

        private void RecordMiningTime()
        {
            if (_currentRunning == null || !_startMining.HasValue) return;

            if (_donationMiningMode == MiningModeEnum.Automatic) _autoMiningTime += (DateTime.Now - _startMining.Value);
            if (_donationMiningMode == MiningModeEnum.Donation) _donationMiningTime += (DateTime.Now - _startMining.Value);

            _currentRunning.TimeMining += (DateTime.Now - _startMining.Value);
            _currentRunning.UpdateStatus();
            _startMining = null;
        }

        private void StartMiner(PriceEntryBase entry, bool isMinimizedToTray = false)
        {
            _nextRun = null;
            _nextRunFromTime = null;
            _currentRunning = entry;
            _startMining = DateTime.Now;

            _process = new Process();
            if (_donationMiningMode == MiningModeEnum.Donation)
            {
                if (!string.IsNullOrWhiteSpace(entry.DonationFolder))
                    _process.StartInfo.WorkingDirectory = entry.DonationFolder;
                _process.StartInfo.FileName = entry.DonationCommand;
                _process.StartInfo.Arguments = entry.DonationArguments;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(entry.Folder))
                    _process.StartInfo.WorkingDirectory = entry.Folder;
                _process.StartInfo.FileName = entry.Command;
                _process.StartInfo.Arguments = entry.Arguments;
            }
            _process.StartInfo.WindowStyle = (isMinimizedToTray && TrayMode == 2) ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Minimized;
            _process.Start();
            _startMining = DateTime.Now;
            _donationMiningMode = MiningMode;
            Thread.Sleep(100);
            try
            {
                ProcessUtil.SetWindowTitle(_process, string.Format("{0} {1} Miner", entry.ServicePrint, entry.Name));
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
            }

            if (isMinimizedToTray && TrayMode == 1)
                HideMinerWindow();

            entry.UpdateStatus();
        }

        private void ClearDeadTimes()
        {
            foreach (var entry in _priceEntries)
                entry.DeadTime = DateTime.MinValue;
        }

        public void RequestStop()
        {
            _nextRun = null;
            _nextRunFromTime = null;

            StopMiner();
            ClearDeadTimes();
        }

        public void RequestStart(int id, bool isMinimizedToTray = false)
        {
            var entry = _priceEntries.Single(o => o.Id == id);
            StartMiner(entry, isMinimizedToTray);
        }

        public void CheckPrices()
        {
            foreach (var service in _services)
                service.CheckPrices();
        }

        public void RunBestAlgo(bool isMinimizedToTray)
        {
            try
            {
                // Check for dead process
                if (_process != null && _process.HasExited && _currentRunning != null)
                {
                    lock (this)
                    {
                        _currentRunning.DeadTime = DateTime.Now;
                        RecordMiningTime();
                    }
                }

                // Clear information if process not running
                if (_process == null || _process.HasExited)
                {
                    _currentRunning = null;
                    _startMining = null;
                    _nextRun = null;
                    _nextRunFromTime = null;
                }

                // Donation mining
                if (DoDonationMinging)
                {
                    if (_donationMiningMode == MiningModeEnum.Automatic && TimeUntilDonation == TimeSpan.Zero)
                    {
                        StopMiner();
                        _donationMiningMode = MiningModeEnum.Donation;
                        MiningMode = _donationMiningMode;
                        _autoMiningTime = TimeSpan.Zero;
                    }
                    else if (_donationMiningMode == MiningModeEnum.Donation && TimeDuringDonation == TimeSpan.Zero)
                    {
                        StopMiner();
                        _donationMiningMode = MiningModeEnum.Automatic;
                        MiningMode = _donationMiningMode;
                        _donationMiningTime = TimeSpan.Zero;
                    }
                }

                // Restart miner if max time reached
                if (RestartTime.HasValue && RestartTime.Value <= TimeSpan.Zero)
                    StopMiner();

                // Find the best, live entry
                var best = _donationMiningMode == MiningModeEnum.Donation
                    ? _priceEntries
                        .Where(o => !o.IsDead)
                        .Where(o => !string.IsNullOrWhiteSpace(o.DonationCommand))
                        .OrderByDescending(o => o.NetEarn)
                        .First()
                    : _priceEntries
                        .Where(o => !o.IsDead)
                        .Where(o => !string.IsNullOrWhiteSpace(o.Command))
                        .OrderByDescending(o => o.NetEarn)
                        .First();

                // Handle minimum time for better algorithm before switching
                if (_switchTime > TimeSpan.Zero && _currentRunning != null)
                {
                    if (!_nextRun.HasValue && _currentRunning.Id != best.Id)
                    {
                        _nextRun = best.Id;
                        _nextRunFromTime = DateTime.Now;
                    }
                    else if (_nextRun.HasValue && _currentRunning.Id == best.Id)
                    {
                        _nextRun = null;
                        _nextRunFromTime = null;
                    }
                    if (NextRunTime.HasValue && NextRunTime > TimeSpan.Zero)
                        best = _priceEntries.First(o => o.Id == _currentRunning.Id);
                }

                // Update undead entries
                var entries = PriceEntries.Where(o => !o.IsDead && o.DeadTime != DateTime.MinValue);
                foreach (var entry in entries)
                    entry.DeadTime = DateTime.MinValue;

                // Just update time if we are already running the right entry
                if (_currentRunning != null && _currentRunning.Id == best.Id)
                {
                    _currentRunning.UpdateStatus();
                    return;
                }

                // Honor minimum time to run in auto mode
                if (MiningTime.HasValue && MiningTime.Value < _minTime)   
                {
                    _currentRunning.UpdateStatus();
                    return;
                }

                StopMiner();
                StartMiner(best, isMinimizedToTray);
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
            }
        }

        public void HideMinerWindow()
        {
            ProcessUtil.HideWindow(_process);
        }

        public void MinimizeMinerWindow()
        {
            ProcessUtil.MinimizeWindow(_process);
        }
    }
}
