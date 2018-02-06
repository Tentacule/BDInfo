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
using System.Collections.Generic;

namespace BDInfo.BDROM
{
    public class TSPacketParser
    {
        public bool SyncState = false;
        public byte TimeCodeParse = 4;
        public byte PacketLength = 0;
        public byte HeaderParse = 0;

        public uint TimeCode;
        public byte TransportErrorIndicator;
        public byte PayloadUnitStartIndicator;
        public byte TransportPriority;
        public ushort PID;
        public byte TransportScramblingControl;
        public byte AdaptionFieldControl;

        public bool AdaptionFieldState = false;
        public byte AdaptionFieldParse = 0;
        public byte AdaptionFieldLength = 0;

        public ushort PCRPID = 0xFFFF;
        public byte PCRParse = 0;
        public ulong PreviousPCR = 0;
        public ulong PCR = 0;
        public ulong PCRCount = 0;
        public ulong PTSFirst = ulong.MaxValue;
        public ulong PTSLast = ulong.MinValue;
        public ulong PTSDiff = 0;

        public byte[] PAT = new byte[1024];
        public bool PATSectionStart = false;
        public byte PATPointerField = 0;
        public uint PATOffset = 0;
        public byte PATSectionLengthParse = 0;
        public ushort PATSectionLength = 0;
        public uint PATSectionParse = 0;
        public bool PATTransferState = false;
        public byte PATSectionNumber = 0;
        public byte PATLastSectionNumber = 0;

        public ushort TransportStreamId = 0xFFFF;

        public List<TSDescriptor> PMTProgramDescriptors = new List<TSDescriptor>();
        public ushort PMTPID = 0xFFFF;
        public Dictionary<ushort, byte[]> PMT = new Dictionary<ushort, byte[]>();
        public bool PMTSectionStart = false;
        public ushort PMTProgramInfoLength = 0;
        public byte PMTProgramDescriptor = 0;
        public byte PMTProgramDescriptorLengthParse = 0;
        public byte PMTProgramDescriptorLength = 0;
        public ushort PMTStreamInfoLength = 0;
        public uint PMTStreamDescriptorLengthParse = 0;
        public uint PMTStreamDescriptorLength = 0;
        public byte PMTPointerField = 0;
        public uint PMTOffset = 0;
        public uint PMTSectionLengthParse = 0;
        public ushort PMTSectionLength = 0;
        public uint PMTSectionParse = 0;
        public bool PMTTransferState = false;
        public byte PMTSectionNumber = 0;
        public byte PMTLastSectionNumber = 0;

        public byte PMTTemp = 0;

        public TSStream Stream = null;
        public TSStreamState StreamState = null;

        public ulong TotalPackets = 0;
    }
}
