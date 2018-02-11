using BDInfo.BDROM;

namespace BDInfo.Scanner
{
    public class ScanBitratesEventArgs
    {
        public ScanBitratesEventArgs(BdRomIso bdRomIso, ScanBDROMState scanState)
        {
            BdRomIso = bdRomIso;
            ScanState = scanState;
        }

        public BdRomIso BdRomIso { get; }
        public ScanBDROMState ScanState { get; }
    }
}