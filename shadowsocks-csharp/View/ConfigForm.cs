﻿using System;
using System.Drawing;
using System.Windows.Forms;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;

namespace Shadowsocks.View
{
    public partial class ConfigForm : Form
    {
        private ShadowsocksController controller;

        // this is a copy of configuration that we are working on
        private Configuration _modifiedConfiguration;
        private int _lastSelectedIndex = -1;

        public ConfigForm(ShadowsocksController controller)
        {
            this.Font = SystemFonts.MessageBoxFont;
            InitializeComponent();

            // a dirty hack
            this.ServersListBox.Dock = DockStyle.Fill;
            this.tableLayoutPanel5.Dock = DockStyle.Fill;
            this.PerformLayout();

            UpdateTexts();
            SetupValueChangedListeners();
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());

            this.controller = controller;
            controller.ConfigChanged += controller_ConfigChanged;

            LoadCurrentConfiguration();
        }

        private void UpdateTexts()
        {
            AddButton.Text = I18N.GetString("&Add");
            DeleteButton.Text = I18N.GetString("&Delete");
            DuplicateButton.Text = I18N.GetString("Dupli&cate");
            IPLabel.Text = I18N.GetString("Server Addr");
            ServerPortLabel.Text = I18N.GetString("Server Port");
            PasswordLabel.Text = I18N.GetString("Password");
            ShowPasswdCheckBox.Text = I18N.GetString("Show Password");
            EncryptionLabel.Text = I18N.GetString("Encryption");
            PluginLabel.Text = I18N.GetString("Plugin Program");
            PluginOptionsLabel.Text = I18N.GetString("Plugin Options");
            PluginArgumentsLabel.Text = I18N.GetString("Plugin Arguments");
            NeedPluginArgCheckBox.Text = I18N.GetString("Need Plugin Argument");
            ProxyPortLabel.Text = I18N.GetString("Proxy Port");
            PortableModeCheckBox.Text = I18N.GetString("Portable Mode");
            toolTip1.SetToolTip(this.PortableModeCheckBox, I18N.GetString("Restart required"));
            RemarksLabel.Text = I18N.GetString("Remarks");
            TimeoutLabel.Text = I18N.GetString("Timeout(Sec)");
            ServerGroupBox.Text = I18N.GetString("Server");
            OKButton.Text = I18N.GetString("OK");
            MyCancelButton.Text = I18N.GetString("Cancel");
            ApplyButton.Text = I18N.GetString("Apply");
            MoveUpButton.Text = I18N.GetString("Move &Up");
            MoveDownButton.Text = I18N.GetString("Move D&own");
            this.Text = I18N.GetString("Edit Servers");
        }

        private void SetupValueChangedListeners()
        {
            IPTextBox.TextChanged += ConfigValueChanged;
            ProxyPortTextBox.TextChanged += ConfigValueChanged;
            PasswordTextBox.TextChanged += ConfigValueChanged;
            EncryptionSelect.SelectedIndexChanged += ConfigValueChanged;
            PluginTextBox.TextChanged += ConfigValueChanged;
            PluginArgumentsTextBox.TextChanged += ConfigValueChanged;
            PluginOptionsTextBox.TextChanged += ConfigValueChanged;
            RemarksTextBox.TextChanged += ConfigValueChanged;
            TimeoutTextBox.TextChanged += ConfigValueChanged;
            PortableModeCheckBox.CheckedChanged += ConfigValueChanged;
            ServerPortTextBox.TextChanged += ConfigValueChanged;
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void ConfigValueChanged(object sender, EventArgs e)
        {
            ApplyButton.Enabled = true;
        }

        private bool ValidateAndSaveSelectedServerDetails()
        {
            try
            {
                if (_lastSelectedIndex == -1 || _lastSelectedIndex >= _modifiedConfiguration.configs.Count)
                {
                    return true;
                }
                Server server = GetServerDetailsFromUI();
                if (server == null)
                {
                    return false;
                }
                Configuration.CheckServer(server);
                _modifiedConfiguration.configs[_lastSelectedIndex] = server;

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return false;
        }

        private Server GetServerDetailsFromUI()
        {
            Server server = new Server();
            if (Uri.CheckHostName(server.server = IPTextBox.Text.Trim()) == UriHostNameType.Unknown)
            {
                MessageBox.Show(I18N.GetString("Invalid server address"));
                IPTextBox.Focus();
                return null;
            }
            if (!int.TryParse(ServerPortTextBox.Text, out server.server_port))
            {
                MessageBox.Show(I18N.GetString("Illegal port number format"));
                ServerPortTextBox.Focus();
                return null;
            }
            server.password = PasswordTextBox.Text;
            server.method = EncryptionSelect.Text;
            server.plugin = PluginTextBox.Text;
            server.plugin_opts = PluginOptionsTextBox.Text;
            server.plugin_args = PluginArgumentsTextBox.Text;
            server.remarks = RemarksTextBox.Text;
            if (!int.TryParse(TimeoutTextBox.Text, out server.timeout))
            {
                MessageBox.Show(I18N.GetString("Illegal timeout format"));
                TimeoutTextBox.Focus();
                return null;
            }
            return server;
        }

        private void LoadSelectedServerDetails()
        {
            if (ServersListBox.SelectedIndex >= 0 && ServersListBox.SelectedIndex < _modifiedConfiguration.configs.Count)
            {
                Server server = _modifiedConfiguration.configs[ServersListBox.SelectedIndex];
                SetServerDetailsToUI(server);
            }
        }

        private void SetServerDetailsToUI(Server server)
        {
            IPTextBox.Text = server.server;
            ServerPortTextBox.Text = server.server_port.ToString();
            PasswordTextBox.Text = server.password;
            EncryptionSelect.Text = server.method ?? "aes-256-cfb";
            PluginTextBox.Text = server.plugin;
            PluginOptionsTextBox.Text = server.plugin_opts;
            PluginArgumentsTextBox.Text = server.plugin_args;

            bool showPluginArgInput = !string.IsNullOrEmpty(server.plugin_args);
            NeedPluginArgCheckBox.Checked = showPluginArgInput;
            ShowHidePluginArgInput(showPluginArgInput);

            RemarksTextBox.Text = server.remarks;
            TimeoutTextBox.Text = server.timeout.ToString();
        }

        private void ShowHidePluginArgInput(bool show)
        {
            PluginArgumentsTextBox.Visible = show;
            PluginArgumentsLabel.Visible = show;
        }


        private void LoadServerNameListToUI(Configuration configuration)
        {
            ServersListBox.Items.Clear();
            foreach (Server server in configuration.configs)
            {
                ServersListBox.Items.Add(server.FriendlyName());
            }
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedConfiguration = controller.GetConfigurationCopy();
            LoadServerNameListToUI(_modifiedConfiguration);

            _lastSelectedIndex = _modifiedConfiguration.index;
            if (_lastSelectedIndex < 0 || _lastSelectedIndex >= ServersListBox.Items.Count)
            {
                _lastSelectedIndex = 0;
            }

            ServersListBox.SelectedIndex = _lastSelectedIndex;
            UpdateButtons();
            LoadSelectedServerDetails();
            ProxyPortTextBox.Text = _modifiedConfiguration.localPort.ToString();
            PortableModeCheckBox.Checked = _modifiedConfiguration.portableMode;
            ApplyButton.Enabled = false;
        }

        private bool SaveValidConfiguration()
        {
            if (!ValidateAndSaveSelectedServerDetails())
            {
                return false;
            }
            if (_modifiedConfiguration.configs.Count == 0)
            {
                MessageBox.Show(I18N.GetString("Please add at least one server"));
                return false;
            }

            int localPort = int.Parse(ProxyPortTextBox.Text);
            Configuration.CheckLocalPort(localPort);
            _modifiedConfiguration.localPort = localPort;

            _modifiedConfiguration.portableMode = PortableModeCheckBox.Checked;

            controller.SaveServers(_modifiedConfiguration.configs, _modifiedConfiguration.localPort, _modifiedConfiguration.portableMode);
            // SelectedIndex remains valid
            // We handled this in event handlers, e.g. Add/DeleteButton, SelectedIndexChanged
            // and move operations
            controller.SelectServerIndex(ServersListBox.SelectedIndex);
            return true;
        }

        private void ConfigForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Sometimes the users may hit enter key by mistake, and the form will close without saving entries.

            if (e.KeyCode == Keys.Enter)
            {
                SaveValidConfiguration();
            }
        }

        private void ServersListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!ServersListBox.CanSelect)
            {
                return;
            }
            if (_lastSelectedIndex == ServersListBox.SelectedIndex)
            {
                // we are moving back to oldSelectedIndex or doing a force move
                return;
            }
            if (!ValidateAndSaveSelectedServerDetails())
            {
                // why this won't cause stack overflow?
                ServersListBox.SelectedIndex = _lastSelectedIndex;
                return;
            }
            if (_lastSelectedIndex >= 0 && _lastSelectedIndex < _modifiedConfiguration.configs.Count)
            {
                ServersListBox.Items[_lastSelectedIndex] = _modifiedConfiguration.configs[_lastSelectedIndex].FriendlyName();
            }
            UpdateButtons();
            LoadSelectedServerDetails();
            _lastSelectedIndex = ServersListBox.SelectedIndex;
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (!ValidateAndSaveSelectedServerDetails())
            {
                return;
            }
            Server server = Configuration.GetDefaultServer();
            _modifiedConfiguration.configs.Add(server);
            LoadServerNameListToUI(_modifiedConfiguration);
            ServersListBox.SelectedIndex = _modifiedConfiguration.configs.Count - 1;
            _lastSelectedIndex = ServersListBox.SelectedIndex;
        }

        private void DuplicateButton_Click(object sender, EventArgs e)
        {
            if (!ValidateAndSaveSelectedServerDetails())
            {
                return;
            }
            Server currServer = _modifiedConfiguration.configs[_lastSelectedIndex];
            var currIndex = _modifiedConfiguration.configs.IndexOf(currServer);
            _modifiedConfiguration.configs.Insert(currIndex + 1, currServer);
            LoadServerNameListToUI(_modifiedConfiguration);
            ServersListBox.SelectedIndex = currIndex + 1;
            _lastSelectedIndex = ServersListBox.SelectedIndex;
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            _lastSelectedIndex = ServersListBox.SelectedIndex;
            if (_lastSelectedIndex >= 0 && _lastSelectedIndex < _modifiedConfiguration.configs.Count)
            {
                _modifiedConfiguration.configs.RemoveAt(_lastSelectedIndex);
            }
            if (_lastSelectedIndex >= _modifiedConfiguration.configs.Count)
            {
                // can be -1
                _lastSelectedIndex = _modifiedConfiguration.configs.Count - 1;
            }
            ServersListBox.SelectedIndex = _lastSelectedIndex;
            LoadServerNameListToUI(_modifiedConfiguration);
            ServersListBox.SelectedIndex = _lastSelectedIndex;
            LoadSelectedServerDetails();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (SaveValidConfiguration())
            {
                this.Close();
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            SaveValidConfiguration();
        }

        private void ConfigForm_Shown(object sender, EventArgs e)
        {
            IPTextBox.Focus();
        }

        private void ConfigForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            controller.ConfigChanged -= controller_ConfigChanged;
        }

        private void MoveConfigItem(int step)
        {
            int index = ServersListBox.SelectedIndex;
            Server server = _modifiedConfiguration.configs[index];
            object item = ServersListBox.Items[index];

            _modifiedConfiguration.configs.Remove(server);
            _modifiedConfiguration.configs.Insert(index + step, server);
            _modifiedConfiguration.index += step;

            ServersListBox.BeginUpdate();
            ServersListBox.Enabled = false;
            _lastSelectedIndex = index + step;
            ServersListBox.Items.Remove(item);
            ServersListBox.Items.Insert(index + step, item);
            ServersListBox.Enabled = true;
            ServersListBox.SelectedIndex = index + step;
            ServersListBox.EndUpdate();

            UpdateButtons();
        }

        private void UpdateButtons()
        {
            DeleteButton.Enabled = (ServersListBox.Items.Count > 0);
            MoveUpButton.Enabled = (ServersListBox.SelectedIndex > 0);
            MoveDownButton.Enabled = (ServersListBox.SelectedIndex < ServersListBox.Items.Count - 1);
        }

        private void MoveUpButton_Click(object sender, EventArgs e)
        {
            if (!ValidateAndSaveSelectedServerDetails())
            {
                return;
            }
            if (ServersListBox.SelectedIndex > 0)
            {
                MoveConfigItem(-1);  // -1 means move backward
            }
        }

        private void MoveDownButton_Click(object sender, EventArgs e)
        {
            if (!ValidateAndSaveSelectedServerDetails())
            {
                return;
            }
            if (ServersListBox.SelectedIndex < ServersListBox.Items.Count - 1)
            {
                MoveConfigItem(+1);  // +1 means move forward
            }
        }

        private void ShowPasswdCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.PasswordTextBox.UseSystemPasswordChar = !this.ShowPasswdCheckBox.Checked;
        }

        private void UsePluginArgCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ShowHidePluginArgInput(this.NeedPluginArgCheckBox.Checked);
        }
    }
}
