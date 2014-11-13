using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows.Forms;
using MinerControl.PriceEntries;
using MinerControl.Services;
using MinerControl.Utility;

namespace MinerControl
{
    public partial class MainWindow : Form
    {
        private MiningEngine _engine = new MiningEngine();
        private DateTime AppStartTime = DateTime.Now;

        private bool IsMinimizedToTray
        {
            get { return _engine.TrayMode > 0 && this.WindowState == FormWindowState.Minimized; }
        }

        public MainWindow()
        {
            _engine.WriteConsoleAction = WriteConsole;
            _engine.WriteRemoteAction = WriteRemote;

            InitializeComponent();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            if (!_engine.LoadConfig())
                this.Close();
            if (!string.IsNullOrWhiteSpace(_engine.CurrencyCode))
                _engine.LoadExchangeRates();
        }

        private void MainWindow_Shown(object sender, EventArgs e)
        {
            // speeds up data grid view performance.
            typeof(DataGridView).InvokeMember("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty, null, dgPrices, new object[] { true });
            typeof(DataGridView).InvokeMember("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty, null, dgServices, new object[] { true });

            dgServices.AutoGenerateColumns = false;
            dgServices.DataSource = new SortableBindingList<IService>(_engine.Services);

            dgPrices.AutoGenerateColumns = false;
            dgPrices.DataSource = new SortableBindingList<PriceEntryBase>(_engine.PriceEntries);

            if (!_engine.DoDonationMinging)
            {
                textDonationStart.Enabled = false;
                textDonationEnd.Enabled = false;
            }

            lblCurrencySymbol.Text = string.Empty;  // Avoid flashing template value when starting

            if (!_engine.RemoteReceive)
                tabPage.TabPages.Remove(tabRemote);

            UpdateButtons();
            RunCycle();
            UpdateGrid(true);

            if (Program.MinimizeOnStart)
                MinimizeWindow();

            tmrPriceCheck.Enabled = true;
            if (!string.IsNullOrWhiteSpace(_engine.CurrencyCode))
                tmrExchangeUpdate.Enabled = true;
            if (Program.HasAutoStart)
            {
                _engine.MiningMode = MiningModeEnum.Automatic;
                UpdateButtons();
                RunBestAlgo();
            }
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            _engine.Cleanup();
        }

        #region Timer events

        private void tmrPriceCheck_Tick(object sender, EventArgs e)
        {
            RunCycle();
            UpdateGrid();
        }
        
        private void tmrExchangeUpdate_Tick(object sender, EventArgs e)
        {
            _engine.LoadExchangeRates();
        }

        private void tmrTimeUpdate_Tick(object sender, EventArgs e)
        {
            UpdateTimes();

            if (_engine.PricesUpdated)
            {
                UpdateGrid();
                _engine.PricesUpdated = false;
            }

            var autoModes = new[] { MiningModeEnum.Automatic, MiningModeEnum.Donation };
            if (!autoModes.Contains(_engine.MiningMode)) return;

            RunBestAlgo();
        }

        #endregion

        private void RunCycle()
        {
            _engine.CheckPrices();
        }

        private void RunBestAlgo()
        {
            if (!_engine.HasPrices || (Program.HasAutoStart && (DateTime.Now - AppStartTime).TotalSeconds < 3)) return;

            var oldCurrent = _engine.CurrentRunning;
            var oldNext = _engine.NextRun;
            _engine.RunBestAlgo(IsMinimizedToTray);
            if (_engine.CurrentRunning != oldCurrent || _engine.NextRun != oldNext)
                UpdateGrid();
        }

        private void UpdateButtons()
        {
            btnStart.Enabled = _engine.MiningMode == MiningModeEnum.Stopped;
            btnStop.Enabled = _engine.MiningMode != MiningModeEnum.Stopped;
            dgPrices.Columns[dgPrices.Columns.Count - 2].Visible = _engine.MiningMode != MiningModeEnum.Stopped; // Status column
            dgPrices.Columns[dgPrices.Columns.Count - 1].Visible = _engine.MiningMode == MiningModeEnum.Stopped; // Action column
        }

        private void UpdateGrid(bool forceReorder = false)
        {
            lock (_engine)
            {
                // mode 2 == sort always, mode 1 == sort when running, mode 0 == sort never
                if (_engine.GridSortMode == 2 || (_engine.GridSortMode == 1 && (forceReorder || _engine.MiningMode == MiningModeEnum.Automatic)))
                {
                    dgPrices.Sort(dgPrices.Columns["NetEarn"], ListSortDirection.Descending);
                }
            }
        }

        #region Buttons

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (_engine.MiningMode != MiningModeEnum.Stopped) return;
            _engine.MiningMode = MiningModeEnum.Automatic;

            UpdateButtons();
            RunBestAlgo();
            UpdateGrid();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (_engine.MiningMode == MiningModeEnum.Stopped) return;
            _engine.MiningMode = MiningModeEnum.Stopped;

            UpdateButtons();
            _engine.RequestStop();
            UpdateGrid();
        }

        private void dgPrices_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_engine.MiningMode != MiningModeEnum.Stopped) return;

            var senderGrid = (DataGridView)sender;

            if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.RowIndex >= 0)
            {
                var data = senderGrid.DataSource as IList<PriceEntryBase>;
                var entry = data[e.RowIndex];

                _engine.MiningMode = MiningModeEnum.Manual;
                UpdateButtons();
                _engine.RequestStart(entry.Id, IsMinimizedToTray);
                UpdateGrid();
            }
        }

        #endregion

        private void linkDonate_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://blockchain.info/address/1PMj3nrVq5CH4TXdJSnHHLPdvcXinjG72y");
        }

        #region Show/hide window

        private void MinimizeWindow()
        {
            if (_engine.TrayMode == 0)
            {
                this.WindowState = FormWindowState.Minimized;
            }
            else
            {
                HideWindow();
            }
        }

        private void HideWindow()
        {
            notifyIcon.Visible = true;
            notifyIcon.ShowBalloonTip(500);
            this.Hide();

            _engine.HideMinerWindow();
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            this.Show();
            this.WindowState = FormWindowState.Normal;

            _engine.MinimizeMinerWindow();
        }

        private void MainWindow_Resize(object sender, EventArgs e)
        {
            if (_engine.TrayMode > 0 && this.WindowState == FormWindowState.Minimized)
            {
                HideWindow();
            }
        }
       
        #endregion

        private void UpdateTimes()
        {
            textRunningTotal.Text = _engine.TotalTime.FormatTime();
            textTimeCurrent.Text = _engine.MiningTime.FormatTime();
            textTimeSwitch.Text = _engine.NextRunTime.FormatTime();
            textTimeRestart.Text = _engine.RestartTime.FormatTime();
            textDonationStart.Text = _engine.TimeUntilDonation.FormatTime();
            textDonationEnd.Text = _engine.TimeDuringDonation.FormatTime();
            textCurrencyExchange.Text = _engine.Exchange.ToString("N2");
            lblCurrencySymbol.Text = _engine.CurrencySymbol;
            if (_engine.Services != null)
            {
                var balance = _engine.Services.Select(o => o.Currency).Sum();
                textCurrencyBalance.Text = balance.ToString("N4");
            }
        }

        private string ActiveTime(PriceEntryBase priceEntry)
        {
            var time = priceEntry.TimeMining;
            if (_engine.CurrentRunning == priceEntry.Id && _engine.StartMining.HasValue)
                time += (DateTime.Now - _engine.StartMining.Value);
            return time.FormatTime();
        }

        SlidingBuffer<string> _consoleBuffer = new SlidingBuffer<string>(200);

        private void WriteConsole(string text)
        {
            Invoke(new MethodInvoker(
                    delegate
                    {
                        _consoleBuffer.Add(text);

                        textConsole.Lines = _consoleBuffer.ToArray();
                        textConsole.Focus();
                        textConsole.SelectionStart = textConsole.Text.Length;
                        textConsole.SelectionLength = 0;
                        textConsole.ScrollToCaret();
                        textConsole.Refresh();
                    }
                ));
        }

        SlidingBuffer<string> _remoteBuffer = new SlidingBuffer<string>(200);
        
        private void WriteRemote(IPAddress source, string text)
        {
            Invoke(new MethodInvoker(
                    delegate
                    {
                        _remoteBuffer.Add(string.Format("[{0}] {1}", source, text));

                        textRemote.Lines = _remoteBuffer.ToArray();
                        textRemote.Focus();
                        textRemote.SelectionStart = textRemote.Text.Length;
                        textRemote.SelectionLength = 0;
                        textRemote.ScrollToCaret();
                        textRemote.Refresh();
                    }
                ));
        }
    }
}
