using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDInfo.Utilities
{
    public abstract class BytesReaderUtilities
    {
        public static string ReadString(
            byte[] data,
            int count,
            ref int pos)
        {
            string val =
                ASCIIEncoding.ASCII.GetString(data, pos, count);

            pos += count;

            return val;
        }

        public static int ReadInt32(
            byte[] data,
            ref int pos)
        {
            int val =
                ((int) data[pos] << 24) +
                ((int) data[pos + 1] << 16) +
                ((int) data[pos + 2] << 8) +
                ((int) data[pos + 3]);

            pos += 4;

            return val;
        }

        public static int ReadInt16(
            byte[] data,
            ref int pos)
        {
            int val =
                ((int) data[pos] << 8) +
                ((int) data[pos + 1]);

            pos += 2;

            return val;
        }

        public static byte ReadByte(
            byte[] data,
            ref int pos)
        {
            return data[pos++];
        }


        private BytesReaderUtilities()
        {


        }
    }
}
