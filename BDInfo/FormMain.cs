//============================================================================
// BDInfo - Blu-ray Video and Audio Analysis Tool
// Copyright © 2010 Cinema Squid
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//=============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using BDInfo.BDROM;
using BDInfo.Scanner;
using BDInfo.Utilities;

namespace BDInfo
{
    public partial class FormMain : Form
    {
        private BdRomIso _bdRomIso = null;
        private ScanBDROMResult _scanResult = null;

        private int CustomPlaylistCount = 0;
        private BdRomIsoScanner _scanner;

        #region UI Handlers

        private ListViewColumnSorter PlaylistColumnSorter;

        public FormMain(string[] args)
        {
            InitializeComponent();

            PlaylistColumnSorter = new ListViewColumnSorter();
            listViewPlaylistFiles.ListViewItemSorter = PlaylistColumnSorter;
            if (args.Length > 0)
            {
                var path = args[0];
                textBoxSource.Text = path;
                StartScan(path);
            }
            else
            {
                textBoxSource.Text = BDInfoSettings.LastPath;
            }
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            ResetColumnWidths();
        }

        private void textBoxSource_TextChanged(object sender, EventArgs e)
        {
            if (textBoxSource.Text.Length > 0)
            {
                buttonRescan.Enabled = true;
            }
            else
            {
                buttonRescan.Enabled = false;
            }
        }

        private void FormMain_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.All : DragDropEffects.None;
        }

        private void FormMain_DragDrop(object sender, DragEventArgs e)
        {
            var sources = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (sources.Length > 0)
            {
                var path = sources[0];
                textBoxSource.Text = path;
                StartScan(path);
            }
        }

        private void buttonIso_Click(object sender, EventArgs e)
        {
            string path = null;
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Title = "Select a Blu-ray iso";
                dialog.Filter = "Iso|*.iso";
                if (!string.IsNullOrEmpty(textBoxSource.Text))
                {
                    dialog.FileName = textBoxSource.Text;
                }
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    path = dialog.FileName;
                    textBoxSource.Text = path;
                    StartScan(path);
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format(
                    "Error opening path {0}: {1}{2}",
                    path,
                    ex.Message,
                    Environment.NewLine);

                MessageBox.Show(msg, "BDInfo Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            string path = null;
            try
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.Description = "Select a Blu-ray Folder:";
                dialog.ShowNewFolderButton = false;
                if (!string.IsNullOrEmpty(textBoxSource.Text))
                {
                    dialog.SelectedPath = textBoxSource.Text;
                }
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    path = dialog.SelectedPath;
                    textBoxSource.Text = path;
                    StartScan(path);
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format(
                    "Error opening path {0}: {1}{2}",
                    path,
                    ex.Message,
                    Environment.NewLine);

                MessageBox.Show(msg, "BDInfo Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonRescan_Click(object sender, EventArgs e)
        {
            string path = textBoxSource.Text;
            try
            {
                StartScan(path);
            }
            catch (Exception ex)
            {
                string msg = string.Format(
                    "Error opening path {0}: {1}{2}",
                    path,
                    ex.Message,
                    Environment.NewLine);

                MessageBox.Show(msg, "BDInfo Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartScan(string path)
        {
            ShowNotification("Please wait while we scan the disc...");

            CustomPlaylistCount = 0;
            buttonBrowse.Enabled = false;
            buttonRescan.Enabled = false;
            buttonSelectAll.Enabled = false;
            buttonUnselectAll.Enabled = false;
            buttonCustomPlaylist.Enabled = false;
            buttonScan.Enabled = false;
            buttonViewReport.Enabled = false;
            textBoxDetails.Enabled = false;
            listViewPlaylistFiles.Enabled = false;
            listViewStreamFiles.Enabled = false;
            listViewStreams.Enabled = false;
            textBoxDetails.Clear();
            listViewPlaylistFiles.Items.Clear();
            listViewStreamFiles.Items.Clear();
            listViewStreams.Items.Clear();

            _scanner = new BdRomIsoScanner(path);
            _scanner.ScanStreamClipFileError += OnStreamClipFileScanError;
            _scanner.ScanPlaylistFileError += OnPlaylistFileScanError;
            _scanner.ScanStreamFileError += OnStreamFileScanError;
            _scanner.ScanBitratesProgress += ScanBitratesProgress;
            _scanner.ScanBitratesCompleted += ScanBitratesOnScanCompleted;

            _scanner.ScanCompleted += ScannerOnScanCompleted;
            _scanner.Scan();
        }

        private void StartScanBitrates(List<TSStreamFile> streamFiles)
        {
            if (_scanner != null)
            {

                buttonScan.Text = "Cancel Scan";
                progressBarScan.Value = 0;
                progressBarScan.Minimum = 0;
                progressBarScan.Maximum = 100;
                labelProgress.Text = "Scanning disc...";
                labelTimeElapsed.Text = "00:00:00";
                labelTimeRemaining.Text = "00:00:00";
                buttonBrowse.Enabled = false;
                buttonRescan.Enabled = false;
                button1.Enabled = false;

                _scanner.ScanBitrates(streamFiles);
            }
        }

        private void OnPlaylistFileScanError(object sender, ScannerErrorEventArgs e)
        {
            DialogResult result = MessageBox.Show(string.Format(
                    "An error occurred while scanning the playlist file {0}.\n\nThe disc may be copy-protected or damaged.\n\nDo you want to continue scanning the playlist files?", e.PlaylistFile.Name),
                "BDInfo Scan Error", MessageBoxButtons.YesNo);

            e.ContinueScan = (result == DialogResult.Yes);
        }

        private void OnStreamFileScanError(object sender, ScannerErrorEventArgs e)
        {
            DialogResult result = MessageBox.Show(string.Format(
                    "An error occurred while scanning the stream file {0}.\n\nThe disc may be copy-protected or damaged.\n\nDo you want to continue scanning the stream files?", e.StreamFile.Name),
                "BDInfo Scan Error", MessageBoxButtons.YesNo);

            e.ContinueScan = (result == DialogResult.Yes);
        }

        private void OnStreamClipFileScanError(object sender, ScannerErrorEventArgs e)
        {
            DialogResult result = MessageBox.Show(string.Format(
                    "An error occurred while scanning the stream clip file {0}.\n\nThe disc may be copy-protected or damaged.\n\nDo you want to continue scanning the stream clip files?", e.StreamClipFile.Name),
                "BDInfo Scan Error", MessageBoxButtons.YesNo);

            e.ContinueScan = (result == DialogResult.Yes);
        }

        private void ScannerOnScanCompleted(object sender, ScannerEventArgs e)
        {
            HideNotification();

            BdRomIso bdRomIso = e.BdRomIso;
            _bdRomIso = bdRomIso;


            if (e.Exception != null)
            {
                string msg = string.Format(
                    "{0}", e.Exception.Message);
                MessageBox.Show(msg, "BDInfo Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                buttonBrowse.Enabled = true;
                buttonRescan.Enabled = true;
                button1.Enabled = true;
                return;
            }

            buttonBrowse.Enabled = true;
            buttonRescan.Enabled = true;
            buttonScan.Enabled = true;
            buttonSelectAll.Enabled = true;
            buttonUnselectAll.Enabled = true;
            buttonCustomPlaylist.Enabled = true;
            buttonViewReport.Enabled = true;
            textBoxDetails.Enabled = true;
            listViewPlaylistFiles.Enabled = true;
            listViewStreamFiles.Enabled = true;
            listViewStreams.Enabled = true;
            progressBarScan.Value = 0;
            labelProgress.Text = "";
            labelTimeElapsed.Text = "00:00:00";
            labelTimeRemaining.Text = "00:00:00";

            //     textBoxSource.Text = BdRomIso.DirectoryRoot.FullName;

            textBoxDetails.Text += string.Format(
                "Detected BDMV Folder: {0} ({1}) {2}",
                bdRomIso.DirectoryBDMV.FullName,
                bdRomIso.VolumeLabel,
                Environment.NewLine);

            List<string> features = new List<string>();
            if (bdRomIso.Is50Hz)
            {
                features.Add("50Hz Content");
            }
            if (bdRomIso.IsBDPlus)
            {
                features.Add("BD+ Copy Protection");
            }
            if (bdRomIso.IsBDJava)
            {
                features.Add("BD-Java");
            }
            if (bdRomIso.Is3D)
            {
                features.Add("Blu-ray 3D");
            }
            if (bdRomIso.IsDBOX)
            {
                features.Add("D-BOX Motion Code");
            }
            if (bdRomIso.IsPSP)
            {
                features.Add("PSP Digital Copy");
            }
            if (features.Count > 0)
            {
                textBoxDetails.Text += "Detected Features: " + string.Join(", ", features.ToArray()) + Environment.NewLine;
            }

            textBoxDetails.Text += string.Format(
                "Disc Size: {0:N0} bytes{1}",
                bdRomIso.Size,
                Environment.NewLine);

            LoadPlaylists();
        }

        private void ScanBitratesProgress(object sender, ScannerEventArgs e)
        {
            ScanBDROMState scanState = e.ScanState;

            try
            {
                if (scanState.StreamFile != null)
                {
                    labelProgress.Text = string.Format(
                        "Scanning {0}...\r\n",
                        scanState.StreamFile.DisplayName);
                }

                long finishedBytes = scanState.FinishedBytes;
                if (scanState.StreamFile != null)
                {
                    finishedBytes += scanState.StreamFile.Size;
                }

                double progress = ((double)finishedBytes / scanState.TotalBytes);
                int progressValue = (int)Math.Round(progress * 100);
                if (progressValue < 0) progressValue = 0;
                if (progressValue > 100) progressValue = 100;
                progressBarScan.Value = progressValue;

                TimeSpan elapsedTime = DateTime.Now.Subtract(scanState.TimeStarted);
                TimeSpan remainingTime;
                if (progress > 0 && progress < 1)
                {
                    remainingTime = new TimeSpan(
                        (long)((double)elapsedTime.Ticks / progress) - elapsedTime.Ticks);
                }
                else
                {
                    remainingTime = new TimeSpan(0);
                }

                labelTimeElapsed.Text = string.Format(
                    "{0:D2}:{1:D2}:{2:D2}",
                    elapsedTime.Hours,
                    elapsedTime.Minutes,
                    elapsedTime.Seconds);

                labelTimeRemaining.Text = string.Format(
                    "{0:D2}:{1:D2}:{2:D2}",
                    remainingTime.Hours,
                    remainingTime.Minutes,
                    remainingTime.Seconds);

                UpdatePlaylistBitrates();
            }
            catch { }
        }

        private void ScanBitratesOnScanCompleted(object sender, ScannerEventArgs e)
        {
            buttonScan.Enabled = false;

            UpdatePlaylistBitrates();

            labelProgress.Text = "Scan complete.";
            progressBarScan.Value = 100;
            labelTimeRemaining.Text = "00:00:00";

            _scanResult = e.ScanResult;

            if (e.ScanResult.ScanException != null)
            {
                string msg = string.Format(
                    "{0}", e.ScanResult.ScanException.Message);

                MessageBox.Show(msg, "BDInfo Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                if (BDInfoSettings.AutosaveReport)
                {
                    GenerateReport();
                }
                else if (e.ScanResult.FileExceptions.Count > 0)
                {
                    MessageBox.Show(
                        "Scan completed with errors (see report).", "BDInfo Scan",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show(
                        "Scan completed successfully.", "BDInfo Scan",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            buttonBrowse.Enabled = true;
            button1.Enabled = true;
            buttonRescan.Enabled = true;
            buttonScan.Enabled = true;
            buttonScan.Text = "Scan Bitrates";
        }

        private void buttonSettings_Click(object sender, EventArgs e)
        {
            FormSettings settings = new FormSettings();
            settings.ShowDialog();
        }

        private void buttonSelectAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewPlaylistFiles.Items)
            {
                item.Checked = true;
            }
        }

        private void buttonUnselectAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewPlaylistFiles.Items)
            {
                item.Checked = false;
            }
        }

        private void buttonCustomPlaylist_Click(object sender, EventArgs e)
        {
            string name = string.Format(
                "USER.{0}", (++CustomPlaylistCount).ToString("D3"));

            FormPlaylist form = new FormPlaylist(name, _bdRomIso, OnCustomPlaylistAdded);
            form.LoadPlaylists();
            form.Show();
        }

        public void OnCustomPlaylistAdded()
        {
            LoadPlaylists();
        }

        private void buttonScan_Click(object sender, EventArgs e)
        {
            if (_scanner != null && _scanner.IsBusy)
            {
                _scanner.CancelAsync();

                return;
            }

            string path = textBoxSource.Text;
            try
            {
                StartScanBitrates(GetSelectedStreamFiles());
            }
            catch (Exception ex)
            {
                string msg = string.Format(
                    "Error opening path {0}: {1}{2}",
                    path,
                    ex.Message,
                    Environment.NewLine);

                MessageBox.Show(msg, "BDInfo Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void buttonViewReport_Click(object sender, EventArgs e)
        {
            GenerateReport();
        }

        private void listViewPlaylistFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadPlaylist();
        }

        private void listViewPlaylistFiles_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == PlaylistColumnSorter.SortColumn)
            {
                if (PlaylistColumnSorter.Order == SortOrder.Ascending)
                {
                    PlaylistColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    PlaylistColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                PlaylistColumnSorter.SortColumn = e.Column;
                PlaylistColumnSorter.Order = SortOrder.Ascending;
            }
            listViewPlaylistFiles.Sort();
        }

        private void ResetColumnWidths()
        {
            listViewPlaylistFiles.Columns[0].Width =
                (int)(listViewPlaylistFiles.ClientSize.Width * 0.23);
            listViewPlaylistFiles.Columns[1].Width =
                (int)(listViewPlaylistFiles.ClientSize.Width * 0.08);
            listViewPlaylistFiles.Columns[2].Width =
                (int)(listViewPlaylistFiles.ClientSize.Width * 0.23);
            listViewPlaylistFiles.Columns[3].Width =
                (int)(listViewPlaylistFiles.ClientSize.Width * 0.23);
            listViewPlaylistFiles.Columns[4].Width =
                (int)(listViewPlaylistFiles.ClientSize.Width * 0.23);

            listViewStreamFiles.Columns[0].Width =
                (int)(listViewStreamFiles.ClientSize.Width * 0.23);
            listViewStreamFiles.Columns[1].Width =
                (int)(listViewStreamFiles.ClientSize.Width * 0.08);
            listViewStreamFiles.Columns[2].Width =
                (int)(listViewStreamFiles.ClientSize.Width * 0.23);
            listViewStreamFiles.Columns[3].Width =
                (int)(listViewStreamFiles.ClientSize.Width * 0.23);
            listViewStreamFiles.Columns[4].Width =
                (int)(listViewStreamFiles.ClientSize.Width * 0.23);

            listViewStreams.Columns[0].Width =
                (int)(listViewStreams.ClientSize.Width * 0.25);
            listViewStreams.Columns[1].Width =
                (int)(listViewStreams.ClientSize.Width * 0.15);
            listViewStreams.Columns[2].Width =
                (int)(listViewStreams.ClientSize.Width * 0.15);
            listViewStreams.Columns[3].Width =
                (int)(listViewStreams.ClientSize.Width * 0.45);
        }

        private void FormMain_FormClosing(
            object sender,
            FormClosingEventArgs e)
        {
            BDInfoSettings.LastPath = textBoxSource.Text;
            BDInfoSettings.SaveSettings();

            if (_scanner != null &&
                _scanner.IsBusy)
            {
                _scanner.CancelAsync();
            }

            if (ReportWorker != null &&
                ReportWorker.IsBusy)
            {
                ReportWorker.CancelAsync();
            }
        }

        #endregion

        #region BdRomIso Initialization Worker


        #endregion

        #region File/Stream Lists

        private void LoadPlaylists()
        {
            listViewPlaylistFiles.Items.Clear();
            listViewStreamFiles.Items.Clear();
            listViewStreams.Items.Clear();

            if (_bdRomIso == null) return;

            bool hasHiddenTracks = false;

            //Dictionary<string, int> playlistGroup = new Dictionary<string, int>();
            List<List<TSPlaylistFile>> groups = new List<List<TSPlaylistFile>>();

            TSPlaylistFile[] sortedPlaylistFiles = new TSPlaylistFile[_bdRomIso.PlaylistFiles.Count];
            _bdRomIso.PlaylistFiles.Values.CopyTo(sortedPlaylistFiles, 0);
            Array.Sort(sortedPlaylistFiles, ComparerUtilities.ComparePlaylistFiles);

            foreach (TSPlaylistFile playlist1
                in sortedPlaylistFiles)
            {
                if (!playlist1.IsValid) continue;

                int matchingGroupIndex = 0;
                for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
                {
                    List<TSPlaylistFile> group = groups[groupIndex];
                    foreach (TSPlaylistFile playlist2 in group)
                    {
                        if (!playlist2.IsValid) continue;

                        foreach (TSStreamClip clip1 in playlist1.StreamClips)
                        {
                            foreach (TSStreamClip clip2 in playlist2.StreamClips)
                            {
                                if (clip1.Name == clip2.Name)
                                {
                                    matchingGroupIndex = groupIndex + 1;
                                    break;
                                }
                            }
                            if (matchingGroupIndex > 0) break;
                        }
                        if (matchingGroupIndex > 0) break;
                    }
                    if (matchingGroupIndex > 0) break;
                }
                if (matchingGroupIndex > 0)
                {
                    groups[matchingGroupIndex - 1].Add(playlist1);
                }
                else
                {
                    groups.Add(new List<TSPlaylistFile> { playlist1 });
                    //matchingGroupIndex = groups.Count;
                }
                //playlistGroup[playlist1.Name] = matchingGroupIndex;
            }

            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                List<TSPlaylistFile> group = groups[groupIndex];
                group.Sort(ComparerUtilities.ComparePlaylistFiles);

                foreach (TSPlaylistFile playlist in group)
                //in BdRomIso.PlaylistFiles.Values)
                {
                    if (!playlist.IsValid) continue;

                    if (playlist.HasHiddenTracks)
                    {
                        hasHiddenTracks = true;
                    }

                    ListViewItem.ListViewSubItem playlistIndex =
                        new ListViewItem.ListViewSubItem();
                    playlistIndex.Text = (groupIndex + 1).ToString();
                    playlistIndex.Tag = groupIndex;

                    ListViewItem.ListViewSubItem playlistName =
                        new ListViewItem.ListViewSubItem();
                    playlistName.Text = playlist.Name;
                    playlistName.Tag = playlist.Name;

                    TimeSpan playlistLengthSpan =
                        new TimeSpan((long)(playlist.TotalLength * 10000000));
                    ListViewItem.ListViewSubItem playlistLength =
                        new ListViewItem.ListViewSubItem();
                    playlistLength.Text = string.Format(
                        "{0:D2}:{1:D2}:{2:D2}",
                        playlistLengthSpan.Hours,
                        playlistLengthSpan.Minutes,
                        playlistLengthSpan.Seconds);
                    playlistLength.Tag = playlist.TotalLength;

                    ListViewItem.ListViewSubItem playlistSize =
                        new ListViewItem.ListViewSubItem();
                    if (BDInfoSettings.EnableSSIF &&
                        playlist.InterleavedFileSize > 0)
                    {
                        playlistSize.Text = playlist.InterleavedFileSize.ToString("N0");
                        playlistSize.Tag = playlist.InterleavedFileSize;
                    }
                    else if (playlist.FileSize > 0)
                    {
                        playlistSize.Text = playlist.FileSize.ToString("N0");
                        playlistSize.Tag = playlist.FileSize;
                    }
                    else
                    {
                        playlistSize.Text = "-";
                        playlistSize.Tag = playlist.FileSize;
                    }

                    ListViewItem.ListViewSubItem playlistSize2 =
                        new ListViewItem.ListViewSubItem();
                    if (playlist.TotalAngleSize > 0)
                    {
                        playlistSize2.Text = (playlist.TotalAngleSize).ToString("N0");
                    }
                    else
                    {
                        playlistSize2.Text = "-";
                    }
                    playlistSize2.Tag = playlist.TotalAngleSize;

                    ListViewItem.ListViewSubItem[] playlistSubItems =
                        new ListViewItem.ListViewSubItem[]
                        {
                            playlistName,
                            playlistIndex,
                            playlistLength,
                            playlistSize,
                            playlistSize2
                        };

                    ListViewItem playlistItem =
                        new ListViewItem(playlistSubItems, 0);
                    listViewPlaylistFiles.Items.Add(playlistItem);
                }
            }

            if (hasHiddenTracks)
            {
                textBoxDetails.Text += "(*) Some playlists on this disc have hidden tracks. These tracks are marked with an asterisk.";
            }

            if (listViewPlaylistFiles.Items.Count > 0)
            {
                listViewPlaylistFiles.Items[0].Selected = true;
            }
            ResetColumnWidths();
        }

        private void LoadPlaylist()
        {
            listViewStreamFiles.Items.Clear();
            listViewStreams.Items.Clear();

            if (_bdRomIso == null) return;
            if (listViewPlaylistFiles.SelectedItems.Count == 0) return;

            ListViewItem playlistItem = listViewPlaylistFiles.SelectedItems[0];
            if (playlistItem == null) return;

            TSPlaylistFile playlist = null;
            string playlistFileName = playlistItem.Text;
            if (_bdRomIso.PlaylistFiles.ContainsKey(playlistFileName))
            {
                playlist = _bdRomIso.PlaylistFiles[playlistFileName];
            }
            if (playlist == null) return;

            int clipCount = 0;
            foreach (TSStreamClip clip in playlist.StreamClips)
            {
                if (clip.AngleIndex == 0)
                {
                    ++clipCount;
                }

                ListViewItem.ListViewSubItem clipIndex =
                    new ListViewItem.ListViewSubItem();
                clipIndex.Text = clipCount.ToString();
                clipIndex.Tag = clipCount;

                ListViewItem.ListViewSubItem clipName =
                    new ListViewItem.ListViewSubItem();
                clipName.Text = clip.DisplayName;
                clipName.Tag = clip.Name;
                if (clip.AngleIndex > 0)
                {
                    clipName.Text += string.Format(
                        " ({0})", clip.AngleIndex);
                }

                TimeSpan clipLengthSpan =
                    new TimeSpan((long)(clip.Length * 10000000));

                ListViewItem.ListViewSubItem clipLength =
                    new ListViewItem.ListViewSubItem();
                clipLength.Text = string.Format(
                    "{0:D2}:{1:D2}:{2:D2}",
                    clipLengthSpan.Hours,
                    clipLengthSpan.Minutes,
                    clipLengthSpan.Seconds);
                clipLength.Tag = clip.Length;

                ListViewItem.ListViewSubItem clipSize =
                    new ListViewItem.ListViewSubItem();
                if (BDInfoSettings.EnableSSIF &&
                    clip.InterleavedFileSize > 0)
                {
                    clipSize.Text = clip.InterleavedFileSize.ToString("N0");
                    clipSize.Tag = clip.InterleavedFileSize;
                }
                else if (clip.FileSize > 0)
                {
                    clipSize.Text = clip.FileSize.ToString("N0");
                    clipSize.Tag = clip.FileSize;
                }
                else
                {
                    clipSize.Text = "-";
                    clipSize.Tag = clip.FileSize;
                }

                ListViewItem.ListViewSubItem clipSize2 =
                    new ListViewItem.ListViewSubItem();
                if (clip.PacketSize > 0)
                {
                    clipSize2.Text = clip.PacketSize.ToString("N0");
                }
                else
                {
                    clipSize2.Text = "-";
                }
                clipSize2.Tag = clip.PacketSize;

                ListViewItem.ListViewSubItem[] streamFileSubItems =
                    new ListViewItem.ListViewSubItem[]
                    {
                        clipName,
                        clipIndex,
                        clipLength,
                        clipSize,
                        clipSize2
                    };

                ListViewItem streamFileItem =
                    new ListViewItem(streamFileSubItems, 0);
                listViewStreamFiles.Items.Add(streamFileItem);
            }

            foreach (TSStream stream in playlist.SortedStreams)
            {
                ListViewItem.ListViewSubItem codec =
                    new ListViewItem.ListViewSubItem();
                codec.Text = stream.CodecName;
                if (stream.AngleIndex > 0)
                {
                    codec.Text += string.Format(
                        " ({0})", stream.AngleIndex);
                }
                codec.Tag = stream.CodecName;

                if (stream.IsHidden)
                {
                    codec.Text = "* " + codec.Text;
                }

                ListViewItem.ListViewSubItem language =
                    new ListViewItem.ListViewSubItem();
                language.Text = stream.LanguageName;
                language.Tag = stream.LanguageName;

                ListViewItem.ListViewSubItem bitrate =
                    new ListViewItem.ListViewSubItem();

                if (stream.AngleIndex > 0)
                {
                    if (stream.ActiveBitRate > 0)
                    {
                        bitrate.Text = string.Format(
                            "{0} kbps", Math.Round((double)stream.ActiveBitRate / 1000));
                    }
                    else
                    {
                        bitrate.Text = "-";
                    }
                    bitrate.Tag = stream.ActiveBitRate;
                }
                else
                {
                    if (stream.BitRate > 0)
                    {
                        bitrate.Text = string.Format(
                            "{0} kbps", Math.Round((double)stream.BitRate / 1000));
                    }
                    else
                    {
                        bitrate.Text = "-";
                    }
                    bitrate.Tag = stream.BitRate;
                }

                ListViewItem.ListViewSubItem description =
                    new ListViewItem.ListViewSubItem();
                description.Text = stream.Description;
                description.Tag = stream.Description;

                ListViewItem.ListViewSubItem[] streamSubItems =
                    new ListViewItem.ListViewSubItem[]
                    {
                        codec,
                        language,
                        bitrate,
                        description
                    };

                ListViewItem streamItem =
                    new ListViewItem(streamSubItems, 0);
                streamItem.Tag = stream.PID;
                listViewStreams.Items.Add(streamItem);
            }

            ResetColumnWidths();
        }

        private void UpdatePlaylistBitrates()
        {
            foreach (ListViewItem item in listViewPlaylistFiles.Items)
            {
                string playlistName = (string)item.SubItems[0].Tag;
                if (_bdRomIso.PlaylistFiles.ContainsKey(playlistName))
                {
                    TSPlaylistFile playlist =
                        _bdRomIso.PlaylistFiles[playlistName];
                    item.SubItems[4].Text = string.Format(
                        "{0}", (playlist.TotalAngleSize).ToString("N0"));
                    item.SubItems[4].Tag = playlist.TotalAngleSize;
                }
            }

            if (listViewPlaylistFiles.SelectedItems.Count == 0)
            {
                return;
            }

            ListViewItem selectedPlaylistItem =
                listViewPlaylistFiles.SelectedItems[0];
            if (selectedPlaylistItem == null)
            {
                return;
            }

            string selectedPlaylistName = (string)selectedPlaylistItem.SubItems[0].Tag;
            TSPlaylistFile selectedPlaylist = null;
            if (_bdRomIso.PlaylistFiles.ContainsKey(selectedPlaylistName))
            {
                selectedPlaylist = _bdRomIso.PlaylistFiles[selectedPlaylistName];
            }
            if (selectedPlaylist == null)
            {
                return;
            }

            for (int i = 0; i < listViewStreamFiles.Items.Count; i++)
            {
                ListViewItem item = listViewStreamFiles.Items[i];
                if (selectedPlaylist.StreamClips.Count > i &&
                    selectedPlaylist.StreamClips[i].Name == (string)item.SubItems[0].Tag)
                {
                    item.SubItems[4].Text = string.Format(
                         "{0}", (selectedPlaylist.StreamClips[i].PacketSize).ToString("N0"));
                    item.Tag = selectedPlaylist.StreamClips[i].PacketSize;

                }
            }

            for (int i = 0; i < listViewStreams.Items.Count; i++)
            {
                ListViewItem item = listViewStreams.Items[i];
                if (i < selectedPlaylist.SortedStreams.Count &&
                    selectedPlaylist.SortedStreams[i].PID == (ushort)item.Tag)
                {
                    TSStream stream = selectedPlaylist.SortedStreams[i];
                    int kbps = 0;
                    if (stream.AngleIndex > 0)
                    {
                        kbps = (int)Math.Round((double)stream.ActiveBitRate / 1000);
                    }
                    else
                    {
                        kbps = (int)Math.Round((double)stream.BitRate / 1000);
                    }
                    item.SubItems[2].Text = string.Format(
                        "{0} kbps", kbps);
                    item.SubItems[3].Text =
                        stream.Description;
                }
            }
        }

        #endregion

        private List<TSStreamFile> GetSelectedStreamFiles()
        {
            List<TSStreamFile> streamFiles = new List<TSStreamFile>();
            if (listViewPlaylistFiles.CheckedItems == null ||
                listViewPlaylistFiles.CheckedItems.Count == 0)
            {
                foreach (var streamFile in _bdRomIso.StreamFiles.Values)
                {
                    streamFiles.Add(streamFile);
                }
            }
            else
            {
                foreach (ListViewItem item
                    in listViewPlaylistFiles.CheckedItems)
                {
                    if (_bdRomIso.PlaylistFiles.ContainsKey(item.Text))
                    {
                        TSPlaylistFile playlist =
                            _bdRomIso.PlaylistFiles[item.Text];

                        foreach (TSStreamClip clip
                            in playlist.StreamClips)
                        {
                            if (!streamFiles.Contains(clip.StreamFile))
                            {
                                streamFiles.Add(clip.StreamFile);
                            }
                        }
                    }
                }
            }

            return streamFiles;
        }

        #region Report Generation

        private BackgroundWorker ReportWorker = null;

        private void GenerateReport()
        {
            ShowNotification("Please wait while we generate the report...");
            buttonViewReport.Enabled = false;

            List<TSPlaylistFile> playlists = new List<TSPlaylistFile>();
            if (listViewPlaylistFiles.CheckedItems == null ||
                listViewPlaylistFiles.CheckedItems.Count == 0)
            {
                foreach (ListViewItem item
                    in listViewPlaylistFiles.Items)
                {
                    if (_bdRomIso.PlaylistFiles.ContainsKey(item.Text))
                    {
                        playlists.Add(_bdRomIso.PlaylistFiles[item.Text]);
                    }
                }
            }
            else
            {
                foreach (ListViewItem item
                    in listViewPlaylistFiles.CheckedItems)
                {
                    if (_bdRomIso.PlaylistFiles.ContainsKey(item.Text))
                    {
                        playlists.Add(_bdRomIso.PlaylistFiles[item.Text]);
                    }
                }
            }

            ReportWorker = new BackgroundWorker();
            ReportWorker.WorkerReportsProgress = true;
            ReportWorker.WorkerSupportsCancellation = true;
            ReportWorker.DoWork += GenerateReportWork;
            ReportWorker.ProgressChanged += GenerateReportProgress;
            ReportWorker.RunWorkerCompleted += GenerateReportCompleted;
            ReportWorker.RunWorkerAsync(playlists);
        }

        private void GenerateReportWork(
            object sender,
            DoWorkEventArgs e)
        {
            try
            {
                List<TSPlaylistFile> playlists = (List<TSPlaylistFile>)e.Argument;
                FormReport report = new FormReport();
                report.Generate(_bdRomIso, playlists, _scanResult);
                e.Result = report;
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private void GenerateReportProgress(
            object sender,
            ProgressChangedEventArgs e)
        {
        }

        private void GenerateReportCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            HideNotification();
            if (e.Result != null)
            {
                if (e.Result.GetType().Name == "FormReport")
                {
                    ((Form)e.Result).Show();
                }
                else if (e.Result.GetType().Name == "Exception")
                {
                    string msg = string.Format(
                        "{0}", ((Exception)e.Result).Message);

                    MessageBox.Show(msg, "BDInfo Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            buttonViewReport.Enabled = true;
        }

        #endregion

        #region Notification Display

        private Form FormNotification = null;

        private void ShowNotification(
            string text)
        {
            HideNotification();

            Label label = new Label();
            label.AutoSize = true;
            label.Font = new Font(Font.SystemFontName, 12);
            label.Text = text;

            FormNotification = new Form();
            FormNotification.ControlBox = false;
            FormNotification.ShowInTaskbar = false;
            FormNotification.ShowIcon = false;
            FormNotification.FormBorderStyle = FormBorderStyle.Fixed3D;
            FormNotification.Controls.Add(label);
            FormNotification.Size = new Size(label.Width + 10, 18);
            FormNotification.Show(this);
            FormNotification.Location = new Point(
                this.Location.X + this.Width / 2 - FormNotification.Width / 2,
                this.Location.Y + this.Height / 2 - FormNotification.Height / 2);
        }

        private void HideNotification()
        {
            if (FormNotification != null &&
                !FormNotification.IsDisposed)
            {
                FormNotification.Close();
                FormNotification = null;
            }
        }

        private void UpdateNotification()
        {
            if (FormNotification != null &&
                !FormNotification.IsDisposed &&
                FormNotification.Visible)
            {
                FormNotification.Location = new Point(
                    this.Location.X + this.Width / 2 - FormNotification.Width / 2,
                    this.Location.Y + this.Height / 2 - FormNotification.Height / 2);
            }
        }

        private void FormMain_Resize(object sender, EventArgs e)
        {
            ResetColumnWidths();
            UpdateNotification();
        }

        private void FormMain_LocationChanged(object sender, EventArgs e)
        {
            UpdateNotification();
        }

        #endregion

    }
}
