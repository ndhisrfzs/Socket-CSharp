using System;

namespace GameSocket
{
    internal enum ParserState
    {
        PacketSize,
        PacketBody
    }
    public class SocketMessage 
    {
        public byte[] Bytes { get; }
        public ushort Length { get; set; }
        public SocketMessage(int length)
        {
            this.Length = 0;
            this.Bytes = new byte[length];
        }
    }

    public class SocketMessageParser
    {
        private readonly CircularBuffer buffer;

        private ushort packetSize = 0;
        private ParserState state = ParserState.PacketSize;
        private SocketMessage message = new SocketMessage(ushort.MaxValue);
        private bool isOk = false;

        public SocketMessageParser(CircularBuffer buffer)
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
                                this.buffer.Read(this.message.Bytes, 0, 2);
                                this.packetSize = BitConverter.ToUInt16(this.message.Bytes, 0);
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
                                this.buffer.Read(this.message.Bytes, 0, this.packetSize);
                                this.message.Length = this.packetSize;
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

        public SocketMessage GetPacket()
        {
            this.isOk = false;
            return this.message;
        }
    }
}
