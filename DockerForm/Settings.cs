﻿using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DockerForm
{
    public partial class Settings : Form
    {
        Dictionary<Guid, PowerProfile> Profiles = new Dictionary<Guid, PowerProfile>();
        bool Initialized = false;

        public Settings()
        {
            InitializeComponent();
            checkBoxMinimizeOnStartup.Checked = Properties.Settings.Default.MinimizeOnStartup;
            checkBoxMinimizeOnClosing.Checked = Properties.Settings.Default.MinimizeOnClosing;
            checkBoxBootOnStartup.Checked = Properties.Settings.Default.BootOnStartup;
            checkBoxToastNotifications.Checked = Properties.Settings.Default.ToastNotifications;
            checkBoxMonitorProcesses.Checked = Properties.Settings.Default.MonitorProcesses;
            checkBoxSaveOnExit.Checked = Properties.Settings.Default.SaveOnExit;
            checkBoxMonitorHardware.Checked = Properties.Settings.Default.MonitorHardware;
            checkBoxMonitorPowerProfiles.Checked = Properties.Settings.Default.MonitorProfiles;
            checkBoxPlaySound.Checked = Properties.Settings.Default.PlaySound;
            checkBoxSpeechSynthesizer.Checked = Properties.Settings.Default.SpeechSynthesizer;

            textBoxSpecificScreenDeviceName.Text = Properties.Settings.Default.SpecificScreenDeviceName;

            comboBoxVoices.Items.Clear();
            foreach (InstalledVoice voice in MainForm.CurrentSynthesizer.GetInstalledVoices())
            {
                VoiceInfo info = voice.VoiceInfo;
                string voiceline = $"{info.Name} - {info.Culture}";
                comboBoxVoices.Items.Add(voiceline);
            }
            comboBoxVoices.SelectedItem = Properties.Settings.Default.CultureVoice;
            comboBoxLanguage.SelectedItem = Properties.Settings.Default.Culture;

            Initialized = true;
        }

        private void checkBoxPowerSpecific_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.MinimizeOnStartup = checkBoxMinimizeOnStartup.Checked;
        }

        private void checkBoxMinimizeOnClosing_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.MinimizeOnClosing = checkBoxMinimizeOnClosing.Checked;
        }

        private void checkBoxBootOnStartup_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.BootOnStartup = checkBoxBootOnStartup.Checked;
            MainForm.UpdateTask();
        }

        private void checkBoxToastNotifications_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.ToastNotifications = checkBoxToastNotifications.Checked;
        }

        private void checkBoxMonitorProcesses_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.MonitorProcesses = checkBoxMonitorProcesses.Checked;
        }

        private void checkBoxSaveOnExit_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.SaveOnExit = checkBoxSaveOnExit.Checked;
        }

        private void checkBoxMonitorHardware_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.MonitorHardware = checkBoxMonitorHardware.Checked;
        }

        private void checkBoxReadNotifications_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.SpeechSynthesizer = checkBoxSpeechSynthesizer.Checked;
        }

        private void checkBoxPlaySound_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.PlaySound = checkBoxPlaySound.Checked;
        }

        private void checkBoxMonitorPowerProfiles_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.MonitorProfiles = checkBoxMonitorPowerProfiles.Checked;
        }

        private void textBoxSpecificScreenDeviceName_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.SpecificScreenDeviceName = textBoxSpecificScreenDeviceName.Text;
        }

        private void Settings_FormClosing(Object sender, FormClosingEventArgs e)
        {
            foreach (PowerProfile profile in MainForm.ProfileDB.Values)
            {
                if (profile.JustCreated)
                    profile.RunMe = MainForm.CanRunProfile(profile, false);
                if (profile.JustUpdated)
                    profile.Serialize();
            }

            Properties.Settings.Default.Save();
            MainForm.UpdateSettings();
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            foreach (CheckBox ctrl in groupBoxGeneral.Controls.Cast<Control>().Concat(groupBox1.Controls.Cast<Control>()).Concat(groupBox2.Controls.Cast<Control>()).Where(a => a.GetType() == typeof(CheckBox)))
            {
                if(toolTip1.GetToolTip(ctrl).Equals(""))
                    toolTip1.SetToolTip(ctrl, ctrl.Text);
            }

            foreach (PowerProfile profile in MainForm.ProfileDB.Values)
            {
                bool isOnBattery = profile._ApplyMask.HasFlag(ProfileMask.OnBattery);
                bool isPluggedIn = profile._ApplyMask.HasFlag(ProfileMask.PluggedIn);
                bool isExtGPU = profile._ApplyMask.HasFlag(ProfileMask.ExternalGPU);
                bool isOnBoot = profile._ApplyMask.HasFlag(ProfileMask.OnStartup);
                bool isOnStatusChange = profile._ApplyMask.HasFlag(ProfileMask.OnStatusChange);
                bool isOnScreen = profile._ApplyMask.HasFlag(ProfileMask.ExternalScreen);
                bool isOnSpecificScreen = profile._ApplyMask.HasFlag(ProfileMask.SpecificScreen);
                bool isNotOnSpecificScreen = profile._ApplyMask.HasFlag(ProfileMask.NoSpecificScreen);
                bool isGameBounds = profile._ApplyMask.HasFlag(ProfileMask.GameBounds);

                ListViewItem newProfile = new ListViewItem(new string[] { profile.ProfileName }, profile.ProfileName);
                newProfile.Tag = profile.ProfileGuid;

                // skip default
                if (profile.ApplyPriority == -1)
                    continue;

                ProfilesList.Items.Add(newProfile);
                Profiles.Add(profile.ProfileGuid, profile);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListViewItem item = ProfilesList.SelectedItems[0];
            Guid ProfileGuid = (Guid)item.Tag;
            PowerProfile CurrentProfile = Profiles[ProfileGuid];

            // reset before updating
            CurrentProfile._ApplyMask = 0;

            if (listBoxTriggers.GetSelected(0))
                CurrentProfile._ApplyMask |= ProfileMask.OnBattery;
            if (listBoxTriggers.GetSelected(1))
                CurrentProfile._ApplyMask |= ProfileMask.PluggedIn;
            if (listBoxTriggers.GetSelected(2))
                CurrentProfile._ApplyMask |= ProfileMask.ExternalGPU;
            if (listBoxTriggers.GetSelected(3))
                CurrentProfile._ApplyMask |= ProfileMask.ExternalScreen;
            if (listBoxTriggers.GetSelected(4))
                CurrentProfile._ApplyMask |= ProfileMask.SpecificScreen;
            if (listBoxTriggers.GetSelected(5))
                CurrentProfile._ApplyMask |= ProfileMask.NoSpecificScreen;
            if (listBoxTriggers.GetSelected(6))
                CurrentProfile._ApplyMask |= ProfileMask.OnStartup;
            if (listBoxTriggers.GetSelected(7))
                CurrentProfile._ApplyMask |= ProfileMask.OnStatusChange;
            if (listBoxTriggers.GetSelected(8))
                CurrentProfile._ApplyMask |= ProfileMask.GameBounds;
        }

        private void ProfilesList_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (e.Label == null)
                return;

            ListViewItem item = ProfilesList.SelectedItems[0];
            Guid ProfileGuid = (Guid)item.Tag;
            PowerProfile CurrentProfile = Profiles[ProfileGuid];
            CurrentProfile.ProfileName = e.Label;
            CurrentProfile.JustUpdated = true;
        }

        private void ProfilesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            MenuItemRemoveSetting.Enabled = false;

            groupBoxPowerBalance.Enabled = false;
            groupBoxPowerProfile.Enabled = false;
            groupBoxFIVR.Enabled = false;
            groupBoxTriggers.Enabled = false;

            foreach (ListViewItem item in ProfilesList.SelectedItems)
            {
                Guid ProfileGuid = (Guid)item.Tag;

                if (MainForm.ProfileDB.ContainsKey(ProfileGuid))
                {
                    groupBoxPowerProfile.Enabled = true;
                    groupBoxTriggers.Enabled = true;
                    switch (MainForm.CurrentCPU.Constructor)
                    {
                        case CPU.Manufacturer.Intel:
                            groupBoxFIVR.Enabled = true;
                            groupBoxPowerBalance.Enabled = true;
                            break;
                        case CPU.Manufacturer.AMD:
                            groupBoxFIVR.Enabled = false;
                            groupBoxPowerBalance.Enabled = false;
                            break;
                    }

                    MenuItemRemoveSetting.Enabled = true;
                }
            }

            foreach (Control ctrl in groupBoxFIVR.Controls)
                if (ctrl.GetType() == typeof(TextBox))
                    ctrl.Text = "";

            foreach (Control ctrl in groupBoxPowerProfile.Controls)
                if (ctrl.GetType() == typeof(TextBox))
                    ctrl.Text = "";

            foreach (ListViewItem item in ProfilesList.SelectedItems)
            {
                string ProfileName = item.Text;
                Guid ProfileGuid = (Guid)item.Tag;

                PowerProfile CurrentProfile = Profiles[ProfileGuid];

                bool isOnBattery = CurrentProfile._ApplyMask.HasFlag(ProfileMask.OnBattery);
                bool isPluggedIn = CurrentProfile._ApplyMask.HasFlag(ProfileMask.PluggedIn);
                bool isExtGPU = CurrentProfile._ApplyMask.HasFlag(ProfileMask.ExternalGPU);
                bool isOnScreen = CurrentProfile._ApplyMask.HasFlag(ProfileMask.ExternalScreen);
                bool isOnSpecificScreen = CurrentProfile._ApplyMask.HasFlag(ProfileMask.SpecificScreen);
                bool isNotOnSpecificScreen = CurrentProfile._ApplyMask.HasFlag(ProfileMask.NoSpecificScreen);
                bool isOnBoot = CurrentProfile._ApplyMask.HasFlag(ProfileMask.OnStartup);
                bool isOnStatusChange = CurrentProfile._ApplyMask.HasFlag(ProfileMask.OnStatusChange);
                bool isGameBounds = CurrentProfile._ApplyMask.HasFlag(ProfileMask.GameBounds);

                // Misc
                numericUpDown1.Value = CurrentProfile.HasLongPowerMax() ? decimal.Parse(CurrentProfile.TurboBoostLongPowerMax) : 0;
                numericUpDown2.Value = CurrentProfile.HasShortPowerMax() ? decimal.Parse(CurrentProfile.TurboBoostShortPowerMax) : 0;
                numericUpDown3.Value = CurrentProfile.HasPowerBalanceCPU() ? decimal.Parse(CurrentProfile.PowerBalanceCPU) : 9;
                numericUpDown4.Value = CurrentProfile.HasPowerBalanceGPU() ? decimal.Parse(CurrentProfile.PowerBalanceGPU) : 13;

                // FIVR
                numericUpDown5.Value = CurrentProfile.HasCPUCore() ? decimal.Parse(CurrentProfile.CPUCore) : 0;
                numericUpDown6.Value = CurrentProfile.HasCPUCache() ? decimal.Parse(CurrentProfile.CPUCache) : 0;
                numericUpDown7.Value = CurrentProfile.HasSystemAgent() ? decimal.Parse(CurrentProfile.SystemAgent) : 0;
                numericUpDown8.Value = CurrentProfile.HasIntelGPU() ? decimal.Parse(CurrentProfile.IntelGPU) : 0;

                // Triggers
                listBoxTriggers.ClearSelected();
                if (isOnBattery)
                    listBoxTriggers.SetSelected(0, true);
                if (isPluggedIn)
                    listBoxTriggers.SetSelected(1, true);
                if (isExtGPU)
                    listBoxTriggers.SetSelected(2, true);
                if (isOnScreen)
                    listBoxTriggers.SetSelected(3, true);
                if (isOnSpecificScreen)
                    listBoxTriggers.SetSelected(4, true);
                if (isNotOnSpecificScreen)
                    listBoxTriggers.SetSelected(5, true);
                if (isOnBoot)
                    listBoxTriggers.SetSelected(6, true);
                if (isOnStatusChange)
                    listBoxTriggers.SetSelected(7, true);
                if (isGameBounds)
                    listBoxTriggers.SetSelected(8, true);
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            ListViewItem item = ProfilesList.SelectedItems[0];
            Guid ProfileGuid = (Guid)item.Tag;
            PowerProfile CurrentProfile = Profiles[ProfileGuid];
            CurrentProfile.TurboBoostLongPowerMax = numericUpDown1.Value.ToString();
            CurrentProfile.JustUpdated = true;
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            ListViewItem item = ProfilesList.SelectedItems[0];
            Guid ProfileGuid = (Guid)item.Tag;
            PowerProfile CurrentProfile = Profiles[ProfileGuid];
            CurrentProfile.TurboBoostShortPowerMax = numericUpDown2.Value.ToString();
            CurrentProfile.JustUpdated = true;
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            ListViewItem item = ProfilesList.SelectedItems[0];
            Guid ProfileGuid = (Guid)item.Tag;
            PowerProfile CurrentProfile = Profiles[ProfileGuid];
            CurrentProfile.PowerBalanceCPU = numericUpDown3.Value.ToString();
            CurrentProfile.JustUpdated = true;
        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            ListViewItem item = ProfilesList.SelectedItems[0];
            Guid ProfileGuid = (Guid)item.Tag;
            PowerProfile CurrentProfile = Profiles[ProfileGuid];
            CurrentProfile.PowerBalanceGPU = numericUpDown4.Value.ToString();
            CurrentProfile.JustUpdated = true;
        }

        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            ListViewItem item = ProfilesList.SelectedItems[0];
            Guid ProfileGuid = (Guid)item.Tag;
            PowerProfile CurrentProfile = Profiles[ProfileGuid];
            CurrentProfile.CPUCore = numericUpDown5.Value.ToString();
            CurrentProfile.JustUpdated = true;
        }

        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            ListViewItem item = ProfilesList.SelectedItems[0];
            Guid ProfileGuid = (Guid)item.Tag;
            PowerProfile CurrentProfile = Profiles[ProfileGuid];
            CurrentProfile.CPUCache = numericUpDown6.Value.ToString();
            CurrentProfile.JustUpdated = true;
        }

        private void numericUpDown7_ValueChanged(object sender, EventArgs e)
        {
            ListViewItem item = ProfilesList.SelectedItems[0];
            Guid ProfileGuid = (Guid)item.Tag;
            PowerProfile CurrentProfile = Profiles[ProfileGuid];
            CurrentProfile.SystemAgent = numericUpDown7.Value.ToString();
            CurrentProfile.JustUpdated = true;
        }

        private void numericUpDown8_ValueChanged(object sender, EventArgs e)
        {
            ListViewItem item = ProfilesList.SelectedItems[0];
            Guid ProfileGuid = (Guid)item.Tag;
            PowerProfile CurrentProfile = Profiles[ProfileGuid];
            CurrentProfile.IntelGPU = numericUpDown8.Value.ToString();
            CurrentProfile.JustUpdated = true;
        }

        private void MenuItemRemoveSetting_Click(object sender, EventArgs e)
        {
            ListViewItem item = ProfilesList.SelectedItems[0];
            Guid ProfileGuid = (Guid)item.Tag;

            ProfilesList.Items.Remove(item);
            Profiles.Remove(ProfileGuid);

            if (MainForm.ProfileDB.ContainsKey(ProfileGuid))
            {
                PowerProfile profile = MainForm.ProfileDB[ProfileGuid];
                profile.Remove();
            }
        }

        private void MenuItemCreateSetting_Click(object sender, EventArgs e)
        {
            string ProfileName = Interaction.InputBox("Please make sure profile name is not already used.", "New Power Profile");

            if (ProfileName == "")
            {
                MessageBox.Show("Profile name can't be empty.");
                return;
            }

            PowerProfile pP = new PowerProfile(ProfileName);
            MainForm.ProfileDB[pP.ProfileGuid] = pP;
            Profiles[pP.ProfileGuid] = pP;

            ListViewItem newProfile = new ListViewItem(new string[] { pP.ProfileName }, pP.ProfileName);
            newProfile.Tag = pP.ProfileGuid;
            ProfilesList.Items.Add(newProfile);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string path = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            FileInfo fileinfo = new FileInfo(path);
            Process.Start(fileinfo.DirectoryName);
        }

        private void comboBoxLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            // change language settings
            if (Properties.Settings.Default.Culture == comboBoxLanguage.Text)
                return;

            Properties.Settings.Default.Culture = comboBoxLanguage.Text;

            for (int i = 0; i < comboBoxVoices.Items.Count; i++)
            {
                string value = comboBoxVoices.GetItemText(comboBoxVoices.Items[i]);
                if (value.Contains(comboBoxLanguage.Text))
                    comboBoxVoices.SelectedItem = value;
            }

            if (Initialized)
                MessageBox.Show("The software needs to restart to apply your language settings to the UI.");
        }

        private void comboBoxVoices_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.CultureVoice = comboBoxVoices.Text;
        }
    }
}
