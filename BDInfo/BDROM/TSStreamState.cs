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

#undef DEBUG
namespace BDInfo.BDROM
{
    public class TSStreamState
    {
        public ulong TransferCount = 0;

        public string StreamTag = null;

        public ulong TotalPackets = 0;
        public ulong WindowPackets = 0;

        public ulong TotalBytes = 0;
        public ulong WindowBytes = 0;

        public long PeakTransferLength = 0;
        public long PeakTransferRate = 0;

        public double TransferMarker = 0;
        public double TransferInterval = 0;

        public TSStreamBuffer StreamBuffer = new TSStreamBuffer();

        public uint Parse = 0;
        public bool TransferState = false;
        public int TransferLength = 0;
        public int PacketLength = 0;
        public byte PacketLengthParse = 0;
        public byte PacketParse = 0;

        public byte PTSParse = 0;
        public ulong PTS = 0;
        public ulong PTSTemp = 0;
        public ulong PTSLast = 0;
        public ulong PTSPrev = 0;
        public ulong PTSDiff = 0;
        public ulong PTSCount = 0;
        public ulong PTSTransfer = 0;

        public byte DTSParse = 0;
        public ulong DTSTemp = 0;
        public ulong DTSPrev = 0;

        public byte PESHeaderLength = 0;
        public byte PESHeaderFlags = 0;
#if DEBUG
        public byte PESHeaderIndex = 0;
        public byte[] PESHeader = new byte[256 + 9];
#endif
    }
}
