using System;
using System.Net;

namespace GameSocket
{
    public interface ISocketService : IDisposable
    {
        ISocketClient GetClient(int id);
        ISocketClient ClientConnect(string address);
        ISocketClient ClientConnect(IPEndPoint ipEndPoint);
        void Remove(int id);
    }
}
