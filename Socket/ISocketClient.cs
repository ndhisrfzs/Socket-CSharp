using System;

namespace GameSocket
{
    public interface ISocketClient : IDisposable
    {
        int id { get; set; }
        event Action<ISocketClient, int> ErrorCallback;
        event Action<Packet> ReadCallback;
        void Start();
        void Send(byte[] message);
    }
}
