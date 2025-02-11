﻿using GChan.Controllers;
using GChan.Models;
using GChan.Properties;
using GChan.Trackers;
using NLog;
using NLog.Targets;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Type = GChan.Trackers.Type;

namespace GChan.Forms
{
    public partial class MainForm : Form
    {
        internal MainFormModel Model => Controller.Model;

        private readonly MainController Controller;

        /// <summary>
        /// Get the index of the selected row in the thread grid view.
        /// </summary>
        private int ThreadGridViewSelectedRowIndex => threadGridView?.CurrentCell?.RowIndex ?? -1;

        private int BoardsListBoxSelectedRowIndex { get { return boardsListBox.SelectedIndex; } set {boardsListBox.SelectedIndex = value;} }

        public MainForm()
        {
            InitializeComponent();

            Controller = new MainController(this);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Controller.LoadTrackers();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            //Start minimized if program was started with Program.TRAY_CMDLINE_ARG (-tray) command line argument.
            if (Program.arguments.Contains(Program.TRAY_CMDLINE_ARG))
            {
                WindowState = FormWindowState.Minimized;
                Hide();
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Program.WM_MY_MSG)
            {
                if ((m.WParam.ToInt32() == 0xCDCD) && (m.LParam.ToInt32() == 0xEFEF))
                {
                    if (WindowState == FormWindowState.Minimized)
                    {
                        WindowState = FormWindowState.Normal;
                    }

                    // Bring window to front.
                    bool temp = TopMost;
                    TopMost = true;
                    TopMost = temp;

                    // Set focus to the window.
                    Activate();
                }
            }
            else
            {
                base.WndProc(ref m);
            }
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            // This way, it doesn't flash text during lazy entry
            string textBox = urlTextBox.Text; 

            // Clear TextBox faster
            urlTextBox.Text = "";

            if (string.IsNullOrWhiteSpace(textBox) && Clipboard.ContainsText() && Settings.Default.AddUrlFromClipboardWhenTextboxEmpty)
                textBox = Clipboard.GetText();

            // Get url from TextBox
            var urls = textBox.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < urls.Length; i++)
            {
                urls[i] = Utils.PrepareURL(urls[i]);
                Controller.AddUrl(urls[i]);
            }
        }

        private void DragDropHandler(object sender, DragEventArgs e)
        {
            var textData = (string)e.Data.GetData(DataFormats.Text);

            // Get url from TextBox
            var urls = textData.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < urls.Length; i++)
            {
                urls[i] = Utils.PrepareURL(urls[i]);
                Controller.AddUrl(urls[i]);
            }
        }

        private void DragEnterHandler(object sender, DragEventArgs e)
        {
            // See if this is a copy and the data includes text.
            if (e.Data.GetDataPresent(DataFormats.Text) && e.AllowedEffect.HasFlag(DragDropEffects.Copy))
            {
                // Allow this.
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                // Don't allow any other drop.
                e.Effect = DragDropEffects.None;
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ThreadGridViewSelectedRowIndex != -1)
            {
                try
                {
                    Controller.RemoveThread(Model.Threads[ThreadGridViewSelectedRowIndex], true);
                }
                catch
                {

                }
            }
        }

        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ThreadGridViewSelectedRowIndex != -1)
            {
                string spath = Model.Threads[ThreadGridViewSelectedRowIndex].SaveTo;
                if (!Directory.Exists(spath))
                    Directory.CreateDirectory(spath);
                Process.Start(spath);
            }
        }

        private void openInBrowserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ThreadGridViewSelectedRowIndex != -1)
            {
                string spath = Model.Threads[ThreadGridViewSelectedRowIndex].Url;
                Process.Start(spath);
            }
        }

        private void copyURLToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ThreadGridViewSelectedRowIndex != -1)
            {
                string spath = Model.Threads[ThreadGridViewSelectedRowIndex].Url;
                Clipboard.SetText(spath);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox tAbout = new AboutBox();
            tAbout.ShowDialog();
            tAbout.Dispose();
        }

        private void changelogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Changelog tvInf = new Changelog();
            tvInf.ShowDialog();
            tvInf.Dispose();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsForm tSettings = new SettingsForm();
            tSettings.ShowDialog();
            tSettings.Dispose();
            if (Settings.Default.MinimizeToTray)
                systemTrayNotifyIcon.Visible = true;
            else
                systemTrayNotifyIcon.Visible = false;

            Controller.ScanTimerInterval = Settings.Default.ScanTimer;
        }

        private void openBoardFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (BoardsListBoxSelectedRowIndex != -1)
            {
                string spath = (Model.Boards[BoardsListBoxSelectedRowIndex]).SaveTo;
                if (!Directory.Exists(spath))
                    Directory.CreateDirectory(spath);
                Process.Start(spath);
            }
        }

        private void openBoardURLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (BoardsListBoxSelectedRowIndex != -1)
            {
                string spath = boardsListBox.Items[BoardsListBoxSelectedRowIndex].ToString();
                Process.Start(spath);
            }
        }

        private void deleteBoardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (BoardsListBoxSelectedRowIndex != -1)
            {
                Controller.RemoveBoard((Board)boardsListBox.SelectedItem);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void openFolderToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Process.Start(Settings.Default.SavePath);
        }

        private void clearAllButton_Click(object sender, EventArgs e)
        {
            if (listsTabControl.SelectedIndex > 1)
            { 
                return;
            }

            Controller.ClearTrackers(listsTabControl.SelectedIndex == 0 ? Type.Thread : Type.Board);
        }

        private void boardsListBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int pos;

            if (e.Button == MouseButtons.Left)
            {
                if ((pos = boardsListBox.IndexFromPoint(e.Location)) != -1)
                {
                    string spath = Model.Boards[pos].SaveTo;
                    if (!Directory.Exists(spath))
                        Directory.CreateDirectory(spath);
                    Process.Start(spath);
                }
            }
        }

        /// <summary>
        /// Quality of life features.
        ///     Left click allows user to deselect all rows by clicking white space. 
        ///     Right click highlights the clicked row (otherwise it would not be done, just looks nicer with the context menu).
        ///         But this is also 100% necessary because for the context menu click events we must know which row is selected.
        /// </summary>
        private void threadGridView_MouseDown(object sender, MouseEventArgs e)
        {
            DataGridView.HitTestInfo hitInfo = threadGridView.HitTest(e.X, e.Y);

            if (e.Button == MouseButtons.Left)
            {
                if (hitInfo.Type == DataGridViewHitTestType.None)
                {
                    threadGridView.ClearSelection();
                    threadGridView.CurrentCell = null;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (e.Button == MouseButtons.Right)
                {
                    if (hitInfo.RowIndex != -1)
                    {
                        threadGridView.ClearSelection();
                        threadGridView.Rows[hitInfo.RowIndex].Selected = true;
                        threadGridView.CurrentCell = threadGridView.Rows[hitInfo.RowIndex].Cells[0];
                        threadsContextMenu.Show(Cursor.Position);
                    }
                }
            }
        }

        /// <summary>
        /// Copies threads/boards to clipboard depending on currently selected tab.
        /// </summary>
        private void clipboardButton_Click(object sender, EventArgs e)
        {
            string text;

            if (listsTabControl.SelectedIndex == 0)
            {
                text = string.Join(",", Model.Threads.Select(thread => thread.Url)).Replace("\n", "").Replace("\r", "");
            }
            else
            {
                text = string.Join(",", Model.Boards.Select(board => board.Url)).Replace("\n", "").Replace("\r", "");
            }

            Clipboard.SetText(text);
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ThreadGridViewSelectedRowIndex != -1)
            {
                Controller.RenameThreadSubjectPrompt(ThreadGridViewSelectedRowIndex);
            }
        }

        private void openLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var fullPath = Path.Combine(assemblyPath, "logs");
            Process.Start(fullPath);
        }

        /// <summary>
        /// Have to use this event because BalloonTipShown event doesn't fire for some reason.
        /// </summary>
        private void systemTrayNotifyIcon_MouseMove(object sender, MouseEventArgs e)
        {
            Utils.SetNotifyIconText(systemTrayNotifyIcon, Model.NotificationTrayTooltip);
        }

        private void systemTrayNotifyIcon_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (Visible)
                    Hide();
                else
                {
                    Show();
                    WindowState = FormWindowState.Normal;
                    Activate();
                }
            }
        }

        private void systemTrayOpenFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string spath = Settings.Default.SavePath;

            if (!Directory.Exists(spath))
                Directory.CreateDirectory(spath);

            Process.Start(spath);
        }

        private void systemTrayExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            if (Settings.Default.MinimizeToTray && WindowState == FormWindowState.Minimized)
            {
                // When minimized hide from taskbar if trayicon enabled
                Hide();
            }
        }

        private void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateController.Instance.CheckForUpdates(true);
        }

        private void updateAvailableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateController.Instance.ShowUpdateDialog();
        }

        internal void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = Controller.Closing();
        }
    }
}