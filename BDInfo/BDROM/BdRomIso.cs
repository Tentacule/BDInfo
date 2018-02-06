﻿//============================================================================
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
using BDInfo.Utilities;
using DiscUtils;
using DiscUtils.Udf;

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

        public BdRomIso(string path)
        {
            Path = path;
        }

        public void Scan()
        {
            using (var isoStream = File.Open(Path, FileMode.Open))
            using (var udfReader = new UdfReader(isoStream))
            {
                // Locate BDMV directories.
                SetBdmvDirectories(udfReader);

                // Initialize basic disc properties.
                SetBdmvProperties(udfReader);

                // Scan bdrom files
                InitializeFileLists();
                ScanStreamClips();
                ScanInterleavedFiles();
                ScanPlaylist(udfReader);
                ScanFor50HzContent();
            }
        }

        private void SetBdmvDirectories(UdfReader udfReader)
        {
            DirectoryBDMV = GetDirectory("BDMV", udfReader.Root, 0);

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

        private void SetBdmvProperties(UdfReader udfReader)
        {
            VolumeLabel = udfReader.VolumeLabel;
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

        private void ScanPlaylist(UdfReader udfReader)
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
                    streamFile.Scan(udfReader, playlists, false);
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
    }
}