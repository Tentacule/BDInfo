using System;
using BDInfo.BDROM;

namespace BDInfo.Scanner
{
    public class ScannerErrorEventArgs
    {
        public TSPlaylistFile PlaylistFile { get; set; }
        public TSStreamFile StreamFile { get; set; }
        public TSStreamClipFile StreamClipFile { get; set; }
        public Exception Exception { get; set; }
        public bool ContinueScan { get; set; }
    }
}