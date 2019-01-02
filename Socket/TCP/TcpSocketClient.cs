using System;
using System.Net;
using System.Net.Sockets;

namespace GameSocket.TCP
{
    public class TcpSocketClient : ISocketClient
    {
        public event Action<ISocketClient, int> ErrorCallback;
        public event Action<byte[]> ReadCallback;

        private Socket socket;
        private SocketAsyncEventArgs innArgs = new SocketAsyncEventArgs();
        private SocketAsyncEventArgs outArgs = new SocketAsyncEventArgs();

        private int id;
        private ISocketService service;
        private IPEndPoint RemoteAddress;

        public TcpSocketClient(int id, IPEndPoint ipEndPoint, ISocketService service)
        {
            this.id = id;
            this.RemoteAddress = ipEndPoint;
            this.service = service;
        }

        public TcpSocketClient(int id, Socket socket, ISocketService service)
        {
            this.id = id;
            this.socket = socket;
        }

        public void Send(byte[] message)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
