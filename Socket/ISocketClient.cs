using System;

namespace GameSocket
{
    public interface ISocketClient : IDisposable
    {
        event Action<ISocketClient, int> ErrorCallback;
        event Action<byte[]> ReadCallback;
        void Start();
        void Send(byte[] message);
    }
}
