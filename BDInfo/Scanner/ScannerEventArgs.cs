using System;
using BDInfo.BDROM;

namespace BDInfo.Scanner
{
    public class ScannerEventArgs : EventArgs
    {
        public ScannerEventArgs(BdRomIso bdRomIso, Exception exception, ScanBDROMResult scanResult, ScanBDROMState scanState)
        {
            BdRomIso = bdRomIso;
            Exception = exception;
            ScanResult = scanResult;
            ScanState = scanState;
        }

        public BdRomIso BdRomIso { get; }
        public Exception Exception { get; }
        public ScanBDROMResult ScanResult { get; }
        public ScanBDROMState ScanState { get; }
    }
}