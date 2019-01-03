using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GameSocket.TCP
{
    public class TcpSocketService : ISocketService
    {
        private const int MAX_SOCKETS = 1 << 16;

        private int alloc_id = 0;
        private readonly ISocketClient[] clients = new ISocketClient[MAX_SOCKETS];

        private event Action<ISocketClient> acceptCallback;

        private Socket listener;
        private SocketAsyncEventArgs innArgs = new SocketAsyncEventArgs();

        private int isDisposed = 0;

        public TcpSocketService(IPEndPoint ipEndPoint, Action<ISocketClient> acceptCallback)
        {
            this.acceptCallback += acceptCallback;

            this.listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            this.innArgs.Completed += this.OnComplete;

            this.listener.Bind(ipEndPoint);
            this.listener.Listen(1000);

            this.AcceptAsync();
        }

        public void AcceptAsync()
        {
            this.innArgs.AcceptSocket = null;
            if (this.listener.AcceptAsync(this.innArgs))
            {
                return;
            }
            OnAcceptComplete(this.innArgs);
        }

        private void OnAcceptComplete(object o)
        {
            if (this.listener == null)
            {
                return;
            }
            SocketAsyncEventArgs e = (SocketAsyncEventArgs)o;

            if (e.SocketError != SocketError.Success)
            {
                //Log.Error($"accept error {e.SocketError}");
                goto End;
            }
            int id = ReserveId();
            if(id == -1)
            {
                //Log.Error("refuses connecting, max clients");
                goto End;
            }
            ISocketClient client = new TcpSocketClient(id, e.AcceptSocket, this);
            this.clients[id] = client;

            try
            {
                this.OnAccept(client);
            }
            catch (Exception ex)
            {
                //Log.Error(ex);
            }

        End:
            if (this.listener == null)
            {
                return;
            }

            this.AcceptAsync();
        }

        private int ReserveId()
        {
            for(int i = 0; i < MAX_SOCKETS; i++)
            {
                int id = Interlocked.Increment(ref this.alloc_id);
                if(id < 0)
                {
                    int new_id = this.alloc_id & 0x7fffffff;
                    Interlocked.CompareExchange(ref this.alloc_id, new_id, id);
                    id = new_id;
                }

                if(this.clients[id] == null)
                {
                    return id; 
                }
            }
            return -1;
        }

        private int HashId(int id)
        {
            return id % MAX_SOCKETS;
        }

        private void OnComplete(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    this.OnAcceptComplete(e);
                    break;
                default:
                    throw new SocketException($"socket accept error: {e.LastOperation}");
            }
        }

        public ISocketClient ClientConnect(string address)
        {
            IPEndPoint ipEndPoint = NetworkHelper.ToIPEndPoint(address);
            return ClientConnect(ipEndPoint); 
        }

        public ISocketClient ClientConnect(IPEndPoint ipEndPoint)
        {
            int id = ReserveId();
            if(id == -1)
            {
                throw new SocketException("Can't connecting service, max clients");
            }
            ISocketClient client = new TcpSocketClient(id, ipEndPoint, this);
            this.clients[id] = client;
            return client;
        }

        public ISocketClient GetClient(int id)
        {
            return this.clients[id];
        }

        private void OnAccept(ISocketClient client)
        {
            this.acceptCallback?.Invoke(client);
        }

        public void Remove(int id)
        {
            ISocketClient client = this.clients[id];
            if (client == null)
            {
                return;
            }
            this.clients[id] = null;
            client.Dispose();
        }

        public void Dispose()
        {
            if(Interlocked.CompareExchange(ref isDisposed, 1, 0) == 1)
            {
                return;
            }

            for(int i = 0; i < MAX_SOCKETS; i++)
            {
                ISocketClient client = this.clients[i];
                if(client != null)
                {
                    client.Dispose();
                }
            }
            this.listener?.Close();
            this.listener = null;
            this.innArgs.Dispose();
        }
    }
}
