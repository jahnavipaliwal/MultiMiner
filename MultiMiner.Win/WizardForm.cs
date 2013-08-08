﻿using MultiMiner.Coinchoose.Api;
using MultiMiner.Engine;
using MultiMiner.Engine.Configuration;
using MultiMiner.Xgminer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Linq;

namespace MultiMiner.Win
{
    public partial class WizardForm : Form
    {
        private List<CryptoCoin> coins;

        public WizardForm(List<CryptoCoin> knownCoins)
        {
            InitializeComponent();
            this.coins = knownCoins;
        }

        private void configureMobileMinerPage_Click(object sender, EventArgs e)
        {

        }

        private void mobileMinerInfoLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://mobileminerapp.com/");
        }

        private void WizardForm_Load(object sender, EventArgs e)
        {
            SetupWizardTabControl();
            PopulateCoins();
            minerComboBox.SelectedIndex = 0;
            coinComboBox.SelectedIndex = 0;
        }

        private void PopulateCoins()
        {
            coinComboBox.Items.Clear();

            coinComboBox.Items.Add("Bitcoin");
            coinComboBox.Items.Add("Litecoin");
            coinComboBox.Items.Add("-");

            foreach (CryptoCoin coin in coins)
            {
                if (coinComboBox.Items.IndexOf(coin.Name) == -1)
                    coinComboBox.Items.Add(coin.Name);
            }
        }

        private void SetupWizardTabControl()
        {
            const int margin = 3;
            const int tabHeight = 21;

            wizardTabControl.SelectedTab = chooseMinerPage;
            wizardTabControl.Dock = DockStyle.None;
            wizardTabControl.Top = -(margin + tabHeight);
            wizardTabControl.Left = -(margin);
            wizardTabControl.Width = this.ClientSize.Width + (margin * 2);
            wizardTabControl.Height = this.ClientSize.Height - buttonPanel.Height + (margin * 2) + tabHeight;

            foreach (TabPage tabPage in wizardTabControl.TabPages)
            {
                tabPage.Padding = new Padding(6, tabPage.Padding.Left, 6, tabPage.Padding.Right);
            }
        }

        private void nextButton_Click(object sender, EventArgs e)
        {
            if (wizardTabControl.SelectedIndex < wizardTabControl.TabPages.Count - 1)
                wizardTabControl.SelectedIndex += 1;
            else
                DialogResult = System.Windows.Forms.DialogResult.OK;
            
            if (wizardTabControl.SelectedTab == downloadingMinerPage)
            {
                downloadChosenMiner();
            }
        }

        private void backButton_Click(object sender, EventArgs e)
        {
            if (wizardTabControl.SelectedIndex > 0)
                wizardTabControl.SelectedIndex -= 1;
        }

        private void wizardTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtons();
        }

        private void downloadChosenMiner()
        {
            MinerBackend minerBackend = MinerBackend.Cgminer;
            if (minerComboBox.SelectedIndex == 1)
                minerBackend = MinerBackend.Bfgminer;

            string minerName = MinerPath.GetMinerName(minerBackend);
            string minerPath = Path.Combine("Miners", minerName);
            string destinationFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, minerPath);

            downloadingMinerLabel.Text = String.Format("Please wait while {0} is downloaded from {2} and installed into the folder {1}", minerName, destinationFolder, Installer.GetMinerDownloadRoot(minerBackend));
            Application.DoEvents();

            Cursor = Cursors.WaitCursor;
            Installer.InstallMiner(minerBackend, destinationFolder);
            Cursor = Cursors.Default;

            wizardTabControl.SelectedTab = chooseCoinPage;
        }

        private void UpdateButtons()
        {
            bool nextButtonEnabled = true;
            bool closeButtonEnabled = true;
            bool backButtonEnabled = true;

            if (wizardTabControl.SelectedIndex == wizardTabControl.TabPages.Count - 1)
                nextButton.Text = "Finish";
            else
                nextButton.Text = "Next >";

            backButtonEnabled = wizardTabControl.SelectedIndex > 0;

            if (wizardTabControl.SelectedTab == chooseCoinPage)
            {
                nextButtonEnabled = coinComboBox.Text != "-";
            }
            else if (wizardTabControl.SelectedTab == configurePoolPage)
            {
                int dummy;
                nextButtonEnabled = !String.IsNullOrEmpty(hostEdit.Text) &&
                    !String.IsNullOrEmpty(portEdit.Text) &&
                    !String.IsNullOrEmpty(usernameEdit.Text) &&
                    Int32.TryParse(portEdit.Text, out dummy);
            }
            else if (wizardTabControl.SelectedTab == downloadingMinerPage)
            {
                nextButtonEnabled = false;
                backButtonEnabled = false;
                closeButtonEnabled = false;
            }

            nextButton.Enabled = nextButtonEnabled;
            closeButton.Enabled = closeButtonEnabled;
            backButton.Enabled = backButtonEnabled;
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void bitcoinPoolsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(String.Format("https://google.com/search?q={0}+mining+pools", coinComboBox.Text));
        }

        private void coinComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtons();
            poolsLink.Text = coinComboBox.Text + " mining pools";
        }

        private void hostEdit_TextChanged(object sender, EventArgs e)
        {
            UpdateButtons();
        }

        private void remoteMonitoringCheck_CheckedChanged(object sender, EventArgs e)
        {
            UpdateMobileMinerEdits();
        }

        private void remoteCommandsCheck_CheckedChanged(object sender, EventArgs e)
        {
            UpdateMobileMinerEdits();
        }

        private void UpdateMobileMinerEdits()
        {
            emailAddressEdit.Enabled = remoteMonitoringCheck.Checked || remoteCommandsCheck.Checked;
            appKeyEdit.Enabled = emailAddressEdit.Enabled;
        }

        public void CreateConfigurations(out EngineConfiguration engineConfiguration, 
            out ApplicationConfiguration applicationConfiguraion)
        {
            engineConfiguration = CreateEngineConfiguration();
            applicationConfiguraion = CreateApplicationConfiguration();
        }

        private EngineConfiguration CreateEngineConfiguration()
        {
            EngineConfiguration engineConfiguration;
            engineConfiguration = new EngineConfiguration();

            engineConfiguration.XgminerConfiguration.MinerBackend = MinerBackend.Cgminer;
            if (minerComboBox.SelectedIndex == 1)
                engineConfiguration.XgminerConfiguration.MinerBackend = MinerBackend.Bfgminer;

            CoinConfiguration coinConfiguration = new CoinConfiguration();

            CryptoCoin coin = coins.Single(c => c.Name.Equals(coinComboBox.Text));

            coinConfiguration.Coin = coin;
            coinConfiguration.Enabled = true;

            MiningPool miningPool = new MiningPool();

            miningPool.Host = hostEdit.Text;
            miningPool.Port = Int32.Parse(portEdit.Text);
            miningPool.Username = usernameEdit.Text;
            miningPool.Password = passwordEdit.Text;

            coinConfiguration.Pools.Add(miningPool);

            engineConfiguration.CoinConfigurations.Add(coinConfiguration);
            return engineConfiguration;
        }

        private ApplicationConfiguration CreateApplicationConfiguration()
        {
            ApplicationConfiguration applicationConfiguraion;
            applicationConfiguraion = new ApplicationConfiguration();
            applicationConfiguraion.MobileMinerMonitoring = remoteMonitoringCheck.Checked;
            applicationConfiguraion.MobileMinerRemoteCommands = remoteCommandsCheck.Checked;
            applicationConfiguraion.MobileMinerEmailAddress = emailAddressEdit.Text;
            applicationConfiguraion.MobileMinerApplicationKey = appKeyEdit.Text;
            return applicationConfiguraion;
        }
    }
}
