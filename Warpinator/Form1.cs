﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Warpinator.Controls;

namespace Warpinator
{
    public partial class Form1 : Form
    {
        readonly Server server;
        readonly Timer rescanTimer = new Timer();
        static Form1 current;
        bool quit = false;

        public Form1()
        {
            current = this;
            InitializeComponent();
            flowLayoutPanel.ClientSizeChanged += FlowLayoutPanel_ClientSizeChanged;
            notifyIcon.DoubleClick += (s, e) => Show();
            notifyIcon.Icon = Properties.Resources.warplogo;
            rescanTimer.Interval = 2000;
            rescanTimer.Tick += (s, e) => {
                btnRescan.Enabled = true; rescanToolStripMenuItem.Enabled = true; rescanTimer.Stop();
            };
            server = new Server();
        }

        private async void Form1_Show(object sender, EventArgs e)
        {
            //server.Remotes.Add("a", new Remote { DisplayName = "TEST", UserName = "test", Hostname = "PC1", Address = System.Net.IPAddress.Parse("192.168.1.1"),
            //    Port = 42000, Status = RemoteStatus.DISCONNECTED });
            DoUpdateUI();
            await server.Start();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (Properties.Settings.Default.RunInBackground && !quit)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private async void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            current = null;
            await server.Stop();
        }

        private void Quit()
        {
            quit = true;
            Close();
        }

        public static void UpdateUI()
        {
            if (current != null)
                current.Invoke(new Action(() => current.DoUpdateUI()));
        }

        private void DoUpdateUI()
        {
            flowLayoutPanel.Controls.Clear();
            foreach (var r in server.Remotes.Values)
            {
                var btn = new RemoteButton(r);
                flowLayoutPanel.Controls.Add(btn);
                btn.Width = flowLayoutPanel.ClientSize.Width - 10;
                btn.Show();
            }
            lblNoDevicesFound.Visible = server.Remotes.Count == 0 && server.Running;
            btnRescan.Visible = server.Remotes.Count == 0 && server.Running;
            if (lblInitializing.Visible && server.Running)
            {
                btnRescan.Enabled = false;
                rescanTimer.Start();
            }
            lblInitializing.Visible = !server.Running;
            this.Cursor = server.Running ? Cursors.Default : Cursors.WaitCursor;

            string iface = Makaretu.Dns.MulticastService.GetNetworkInterfaces().FirstOrDefault((i) => i.Id == server.SelectedInterface)?.Name ?? Resources.Strings.interface_unavailable;
            if (String.IsNullOrEmpty(server.SelectedInterface))
                iface = Resources.Strings.any;
            lblIP.Text = Utils.GetLocalIPAddress() + " | " + iface;
            
            lblStatus.Text = server.Running ? Resources.Strings.service_running : Resources.Strings.service_not_running;
        }

        public static void OnIncomingTransfer(Transfer t)
        {
            if (Properties.Settings.Default.NotifyIncoming && current != null)
                current.Invoke(new Action(() => current.ShowTransferBaloon(t)));
            UpdateUI();
        }

        EventHandler ballonClickHandler;
        private void ShowTransferBaloon(Transfer t)
        {
            notifyIcon.BalloonTipTitle = String.Format(Resources.Strings.incoming_transfer, server.Remotes[t.RemoteUUID].Hostname);
            notifyIcon.BalloonTipText = (t.FileCount == 1 ? t.SingleName : String.Format(Resources.Strings.files, t.FileCount)) + " (" + Utils.BytesToHumanReadable((long)t.TotalSize) + ")";

            if (ballonClickHandler != null)
                notifyIcon.BalloonTipClicked -= ballonClickHandler;
            ballonClickHandler = (a, b) => server.Remotes[t.RemoteUUID].OpenWindow();
            notifyIcon.BalloonTipClicked += ballonClickHandler;
            notifyIcon.ShowBalloonTip(5000);
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox().ShowDialog();
        }

        private void FlowLayoutPanel_ClientSizeChanged(object sender, EventArgs e)
        {
            foreach(Control c in flowLayoutPanel.Controls)
            {
                c.Width = flowLayoutPanel.ClientSize.Width - 6;
            }
        }

        private void SettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new SettingsForm().Show();
        }

        private void QuitToolStripMenuItem_Click(object sender, EventArgs e) => Quit();
        private void RescanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            server.Rescan();
            btnRescan.Enabled = false;
            rescanToolStripMenuItem.Enabled = false;
            rescanTimer.Start();
        }
        private void ReannounceToolStripMenuItem_Click(object sender, EventArgs e) => server.Reannounce();
        private void GitHubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://github.com/slowscript/warpinator-windows"));
            } catch
            {
                MessageBox.Show(String.Format(Resources.Strings.cant_open_browser, "https://github.com/slowscript/warpinator-windows"), Resources.Strings.info);
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e) => this.Show();
        private void quitToolStripMenuItem1_Click(object sender, EventArgs e) => Quit();
    }
}
