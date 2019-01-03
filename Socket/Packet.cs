using System;

namespace GameSocket
{
    internal enum ParserState
    {
        PacketSize,
        PacketBody
    }
    public class Packet
    {
        public byte[] Bytes { get; }
        public ushort Length { get; set; }
        public Packet(int length)
        {
            this.Length = 0;
            this.Bytes = new byte[length];
        }
    }

    public class PacketParser
    {
        private readonly CircularBuffer buffer;

        private ushort packetSize = 0;
        private ParserState state = ParserState.PacketSize;
        private Packet packet = new Packet(ushort.MaxValue);
        private bool isOk = false;

        public PacketParser(CircularBuffer buffer)
        {
            this.buffer = buffer;
        }

        public bool Parse()
        {
            if (this.isOk)
            {
                return true;
            }

            bool finish = false;
            while (!finish)
            {
                switch (this.state)
                {
                    case ParserState.PacketSize:
                        {
                            if(this.buffer.Length < 2)
                            {
                                finish = true;
                            }
                            else
                            {
                                this.buffer.Read(this.packet.Bytes, 0, 2);
                                this.packetSize = BitConverter.ToUInt16(this.packet.Bytes, 0);
                                this.state = ParserState.PacketBody;
                            }
                        }
                        break;
                    case ParserState.PacketBody:
                        {
                            if(this.buffer.Length < this.packetSize)
                            {
                                finish = true;
                            }
                            else
                            {
                                this.buffer.Read(this.packet.Bytes, 0, this.packetSize);
                                this.packet.Length = this.packetSize;
                                this.isOk = true;
                                this.state = ParserState.PacketSize;
                                finish = true;
                            }
                        }
                        break;
                }
            }

            return this.isOk;
        }

        public Packet GetPacket()
        {
            this.isOk = false;
            return this.packet;
        }
    }
}
