using System;
using System.Collections.Generic;
using System.Text;

namespace GameSocket
{
    public static class ByteHelper
    {
        public static void WriteTo(this byte[] bytes, int offset, byte num)
        {
            bytes[offset] = num;
        }
        public static void WriteTo(this byte[] bytes, int offset, short num)
        {
            bytes[offset] = (byte)(num & 0xff);
            bytes[offset + 1] = (byte)((num >> 8) & 0xff);
        }
        public static void WriteTo(this byte[] bytes, int offset, ushort num)
        {
            bytes[offset] = (byte)(num & 0xff);
            bytes[offset + 1] = (byte)((num >> 8) & 0xff);
        }
    }
}
