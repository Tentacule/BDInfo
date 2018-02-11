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
using System.IO;
using System.Threading;
using BDInfo.Scanner;
using BDInfo.Utilities;
using DiscUtils;

namespace BDInfo.BDROM
{
    public class BdRomIso
    {
        public readonly string Path;
        public DiscDirectoryInfo DirectoryRoot = null;
        public DiscDirectoryInfo DirectoryBDMV = null;

        public DiscDirectoryInfo DirectoryBDJO = null;
        public DiscDirectoryInfo DirectoryCLIPINF = null;
        public DiscDirectoryInfo DirectoryPLAYLIST = null;
        public DiscDirectoryInfo DirectorySNP = null;
        public DiscDirectoryInfo DirectorySSIF = null;
        public DiscDirectoryInfo DirectorySTREAM = null;

        public string VolumeLabel = null;
        public ulong Size = 0;
        public bool IsBDPlus = false;
        public bool IsBDJava = false;
        public bool IsDBOX = false;
        public bool IsPSP = false;
        public bool Is3D = false;
        public bool Is50Hz = false;

        public Dictionary<string, TSPlaylistFile> PlaylistFiles =
            new Dictionary<string, TSPlaylistFile>();

        public Dictionary<string, TSStreamClipFile> StreamClipFiles =
            new Dictionary<string, TSStreamClipFile>();

        public Dictionary<string, TSStreamFile> StreamFiles =
            new Dictionary<string, TSStreamFile>();

        public Dictionary<string, TSInterleavedFile> InterleavedFiles =
            new Dictionary<string, TSInterleavedFile>();

        public delegate bool OnStreamClipFileScanError(
            TSStreamClipFile streamClipFile, Exception ex);

        public event OnStreamClipFileScanError StreamClipFileScanError;

        public delegate bool OnStreamFileScanError(
            TSStreamFile streamClipFile, Exception ex);

        public event OnStreamFileScanError StreamFileScanError;

        public delegate bool OnPlaylistFileScanError(
            TSPlaylistFile playlistFile, Exception ex);

        public event OnPlaylistFileScanError PlaylistFileScanError;

        private Stream _isoStream;

        public CancellationToken CancellationToken { get; set; }
        public delegate void ScanEventHandler(object sender, ScanBitratesEventArgs e);
        public event ScanEventHandler ScanBitratesProgress;

        public BdRomIso(string path)
        {
            Path = path;
        }

        private void CloseIsoStream()
        {
            _isoStream?.Close();
            _isoStream = null;
        }

        public void Scan()
        {
            CloseIsoStream();

            using (var fileSystem = FileSystemUtilities.GetFileSystem(Path, ref _isoStream))
            {
                // Locate BDMV directories.
                SetBdmvDirectories(fileSystem);

                // Initialize basic disc properties.
                SetBdmvProperties(fileSystem);

                // Scan bdrom files
                InitializeFileLists();
                ScanStreamClips();
                ScanInterleavedFiles();
                ScanPlaylist(fileSystem);
                ScanFor50HzContent();
            }

            CloseIsoStream();
        }

        private void SetBdmvDirectories(DiscFileSystem fileSystem)
        {
            DirectoryBDMV = GetDirectory("BDMV", fileSystem.Root, 0);

            if (DirectoryBDMV == null)
            {
                throw new Exception("Unable to locate BD structure.");
            }

            DirectoryRoot =
                DirectoryBDMV.Parent;
            DirectoryBDJO =
                GetDirectory("BDJO", DirectoryBDMV, 0);
            DirectoryCLIPINF =
                GetDirectory("CLIPINF", DirectoryBDMV, 0);
            DirectoryPLAYLIST =
                GetDirectory("PLAYLIST", DirectoryBDMV, 0);
            DirectorySNP =
                GetDirectory("SNP", DirectoryRoot, 0);
            DirectorySTREAM =
                GetDirectory("STREAM", DirectoryBDMV, 0);
            DirectorySSIF =
                GetDirectory("SSIF", DirectorySTREAM, 0);

            if (DirectoryCLIPINF == null
                || DirectoryPLAYLIST == null)
            {
                throw new Exception("Unable to locate BD structure.");
            }
        }

        private void SetBdmvProperties(DiscFileSystem fileSystem)
        {
            VolumeLabel = fileSystem.VolumeLabel;
            Size = (ulong)GetDirectorySize(DirectoryRoot);

            if (null != GetDirectory("BDSVM", DirectoryRoot, 0))
            {
                IsBDPlus = true;
            }
            if (null != GetDirectory("SLYVM", DirectoryRoot, 0))
            {
                IsBDPlus = true;
            }
            if (null != GetDirectory("ANYVM", DirectoryRoot, 0))
            {
                IsBDPlus = true;
            }

            if (DirectoryBDJO != null &&
                DirectoryBDJO.GetFiles().Length > 0)
            {
                IsBDJava = true;
            }

            if (DirectorySNP != null &&
                (DirectorySNP.GetFiles("*.mnv").Length > 0 || DirectorySNP.GetFiles("*.MNV").Length > 0))
            {
                IsPSP = true;
            }

            if (DirectorySSIF != null &&
                DirectorySSIF.GetFiles().Length > 0)
            {
                Is3D = true;
            }

            if (File.Exists(System.IO.Path.Combine(DirectoryRoot.FullName, "FilmIndex.xml")))
            {
                IsDBOX = true;
            }
        }

        private void InitializeFileLists()
        {
            if (DirectoryPLAYLIST != null)
            {
                DiscFileInfo[] files = DirectoryPLAYLIST.GetFiles("*.mpls");
                if (files.Length == 0)
                {
                    files = DirectoryPLAYLIST.GetFiles("*.MPLS");
                }
                foreach (DiscFileInfo file in files)
                {
                    PlaylistFiles.Add(
                        file.Name.ToUpper(), new TSPlaylistFile(this, file));
                }
            }

            if (DirectorySTREAM != null)
            {
                DiscFileInfo[] files = DirectorySTREAM.GetFiles("*.m2ts");
                if (files.Length == 0)
                {
                    files = DirectoryPLAYLIST.GetFiles("*.M2TS");
                }
                foreach (DiscFileInfo file in files)
                {
                    StreamFiles.Add(
                        file.Name.ToUpper(), new TSStreamFile(file));
                }
            }

            if (DirectoryCLIPINF != null)
            {
                DiscFileInfo[] files = DirectoryCLIPINF.GetFiles("*.clpi");
                if (files.Length == 0)
                {
                    files = DirectoryPLAYLIST.GetFiles("*.CLPI");
                }
                foreach (DiscFileInfo file in files)
                {
                    StreamClipFiles.Add(
                        file.Name.ToUpper(), new TSStreamClipFile(file));
                }
            }

            if (DirectorySSIF != null)
            {
                DiscFileInfo[] files = DirectorySSIF.GetFiles("*.ssif");
                if (files.Length == 0)
                {
                    files = DirectorySSIF.GetFiles("*.SSIF");
                }
                foreach (DiscFileInfo file in files)
                {
                    InterleavedFiles.Add(
                        file.Name.ToUpper(), new TSInterleavedFile(file));
                }
            }

        }

        private void ScanStreamClips()
        {

            List<TSStreamClipFile> errorStreamClipFiles = new List<TSStreamClipFile>();
            foreach (var streamClipFile in StreamClipFiles.Values)
            {
                try
                {
                    streamClipFile.Scan();
                }
                catch (Exception ex)
                {
                    errorStreamClipFiles.Add(streamClipFile);
                    if (StreamClipFileScanError != null)
                    {
                        if (StreamClipFileScanError(streamClipFile, ex))
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else throw ex;
                }
            }
        }

        private void ScanInterleavedFiles()
        {
            foreach (var streamFile in StreamFiles.Values)
            {
                string ssifName = System.IO.Path.GetFileNameWithoutExtension(streamFile.Name) + ".SSIF";
                if (InterleavedFiles.ContainsKey(ssifName))
                {
                    streamFile.InterleavedFile = InterleavedFiles[ssifName];
                }
            }
        }

        private void ScanPlaylist(DiscFileSystem fileSystem)
        {
            TSStreamFile[] streamFiles = new TSStreamFile[StreamFiles.Count];
            StreamFiles.Values.CopyTo(streamFiles, 0);
            Array.Sort(streamFiles, ComparerUtilities.CompareStreamFiles);

            List<TSPlaylistFile> errorPlaylistFiles = new List<TSPlaylistFile>();
            foreach (var playlistFile in PlaylistFiles.Values)
            {
                try
                {
                    playlistFile.Scan(StreamFiles, StreamClipFiles);
                }
                catch (Exception ex)
                {
                    errorPlaylistFiles.Add(playlistFile);
                    if (PlaylistFileScanError != null)
                    {
                        if (PlaylistFileScanError(playlistFile, ex))
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else throw ex;
                }
            }

            List<TSStreamFile> errorStreamFiles = new List<TSStreamFile>();
            foreach (TSStreamFile streamFile in streamFiles)
            {
                try
                {
                    List<TSPlaylistFile> playlists = new List<TSPlaylistFile>();
                    foreach (var playlist in PlaylistFiles.Values)
                    {
                        foreach (var streamClip in playlist.StreamClips)
                        {
                            if (streamClip.Name == streamFile.Name)
                            {
                                playlists.Add(playlist);
                                break;
                            }
                        }
                    }
                    streamFile.Scan(fileSystem, playlists, false);
                }
                catch (Exception ex)
                {
                    errorStreamFiles.Add(streamFile);
                    if (StreamFileScanError != null)
                    {
                        if (StreamFileScanError(streamFile, ex))
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else throw ex;
                }
            }
        }

        private void ScanFor50HzContent()
        {
            foreach (var playlistFile in PlaylistFiles.Values)
            {
                playlistFile.Initialize();
                if (!Is50Hz)
                {
                    foreach (var videoStream in playlistFile.VideoStreams)
                    {
                        if (videoStream.FrameRate == TSFrameRate.FRAMERATE_25 ||
                            videoStream.FrameRate == TSFrameRate.FRAMERATE_50)
                        {
                            Is50Hz = true;
                        }
                    }
                }
            }
        }

        private static DiscDirectoryInfo GetDirectory(string name, DiscDirectoryInfo dir, int searchDepth)
        {
            if (dir != null)
            {
                DiscDirectoryInfo[] children = dir.GetDirectories();
                foreach (var child in children)
                {
                    if (child.Name == name)
                    {
                        return child;
                    }
                }
                if (searchDepth > 0)
                {
                    foreach (var child in children)
                    {
                        GetDirectory(name, child, searchDepth - 1);
                    }
                }
            }
            return null;
        }

        private static long GetDirectorySize(DiscDirectoryInfo directoryInfo)
        {
            long size = 0;

            //if (!ExcludeDirs.Contains(directoryInfo.Name.ToUpper()))  // TODO: Keep?
            {
                DiscFileInfo[] pathFiles = directoryInfo.GetFiles();
                foreach (DiscFileInfo pathFile in pathFiles)
                {
                    if (pathFile.Extension.ToUpper() == ".SSIF")
                    {
                        continue;
                    }
                    size += pathFile.Length;
                }

                DiscDirectoryInfo[] pathChildren = directoryInfo.GetDirectories();
                foreach (DiscDirectoryInfo pathChild in pathChildren)
                {
                    size += GetDirectorySize(pathChild);
                }
            }

            return size;
        }

        public DiscFileInfo GetDiscFileInfo(DiscFileSystem fileSystem, string fullName)
        {
            return fileSystem.GetFileInfo(fullName);
        }

        #region ScanBitrates

        public ScanBDROMResult ScanBitrates(List<TSStreamFile> streamFiles)
        {
            ScanBDROMResult scanResult = new ScanBDROMResult { ScanException = new Exception("Scan is still running.") };
            ScanBDROMState scanState = new ScanBDROMState();

            Timer timer = null;
            Stream isoStream = null;

            try
            {
                using (var fileSystem = FileSystemUtilities.GetFileSystem(Path, ref isoStream))
                {
                    scanState.FileSystem = fileSystem;

                    foreach (var streamFile in streamFiles)
                    {
                        if (BDInfoSettings.EnableSSIF &&
                            streamFile.InterleavedFile != null)
                        {
                            scanState.TotalBytes += GetDiscFileInfo(fileSystem, streamFile.InterleavedFile.FileInfo.FullName).Length;
                        }
                        else
                        {
                            scanState.TotalBytes += GetDiscFileInfo(fileSystem, streamFile.FileInfo.FullName).Length;
                        }

                        if (!scanState.PlaylistMap.ContainsKey(streamFile.Name))
                        {
                            scanState.PlaylistMap[streamFile.Name] = new List<TSPlaylistFile>();
                        }

                        foreach (TSPlaylistFile playlist in PlaylistFiles.Values)
                        {
                            playlist.ClearBitrates();

                            foreach (TSStreamClip clip in playlist.StreamClips)
                            {
                                if (clip.Name == streamFile.Name)
                                {
                                    if (!scanState.PlaylistMap[streamFile.Name].Contains(playlist))
                                    {
                                        scanState.PlaylistMap[streamFile.Name].Add(playlist);
                                    }
                                }
                            }
                        }
                    }

                    timer = new Timer(ScanBDROMEvent, scanState, 1000, 1000);

                    foreach (TSStreamFile streamFile in streamFiles)
                    {
                        scanState.StreamFile = streamFile;

                        Thread thread = new Thread(ScanBDROMThread);

                        thread.Start(scanState);
                        while (thread.IsAlive)
                        {
                            if (CancellationToken.IsCancellationRequested)
                            {
                                scanResult.ScanException = new Exception("Scan was cancelled.");
                                thread.Abort();
                                thread.Join();
                                return scanResult;
                            }
                            Thread.Sleep(0);
                        }
                        scanState.FinishedBytes += GetDiscFileInfo(fileSystem, streamFile.FileInfo.FullName).Length;

                        if (scanState.Exception != null)
                        {
                            scanResult.FileExceptions[streamFile.Name] = scanState.Exception;
                        }
                    }
                    scanResult.ScanException = null;
                }
            }
            catch (Exception ex)
            {
                scanResult.ScanException = ex;
            }
            finally
            {
                isoStream?.Close();
                timer?.Dispose();
            }

            return scanResult;
        }

        private static void ScanBDROMThread(object parameter)
        {
            ScanBDROMState scanState = (ScanBDROMState)parameter;
            try
            {
                TSStreamFile streamFile = scanState.StreamFile;
                List<TSPlaylistFile> playlists = scanState.PlaylistMap[streamFile.Name];

                streamFile.Scan(scanState.FileSystem, playlists, true);
            }
            catch (Exception ex)
            {
                scanState.Exception = ex;
            }
        }

        private void ScanBDROMEvent(object state)
        {
            ScanBitratesProgress?.Invoke(this, new ScanBitratesEventArgs(null, (ScanBDROMState)state));
        }

        #endregion
    }
}
