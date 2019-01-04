using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GameSocket.TCP
{
    public class TcpSocketClient : ISocketClient
    {
        private const int PACKET_SIZE = 2;

        public event Action<ISocketClient, int> ErrorCallback;
        public event Action<SocketMessage> ReadCallback;

        private Socket socket;
        private SocketAsyncEventArgs innArgs = new SocketAsyncEventArgs();
        private SocketAsyncEventArgs outArgs = new SocketAsyncEventArgs();

        private readonly CircularBuffer recvBuffer = new CircularBuffer();
        private readonly CircularBuffer sendBuffer = new CircularBuffer();

        private SocketMessageParser parser;

        public int Id { get; }
        private ISocketService service;
        private IPEndPoint RemoteAddress;

        private int isSending;
        private bool isConnected;

        private readonly byte[] packetSizeCache;

        private int isDisposed = 0;

        public TcpSocketClient(int id, IPEndPoint ipEndPoint, ISocketService service)
        {
            this.Id = id;
            this.RemoteAddress = ipEndPoint;
            this.service = service;
            this.packetSizeCache = new byte[PACKET_SIZE];
            this.parser = new SocketMessageParser(recvBuffer);

            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.socket.NoDelay = true;
            this.innArgs.Completed += OnComplete;
            this.outArgs.Completed += OnComplete;

            this.isConnected = false;
            this.isSending = 0;
        }

        public TcpSocketClient(int id, Socket socket, ISocketService service)
        {
            this.Id = id;
            this.RemoteAddress = (IPEndPoint)socket.RemoteEndPoint;
            this.service = service;
            this.packetSizeCache = new byte[PACKET_SIZE];
            this.parser = new SocketMessageParser(recvBuffer);

            this.socket = socket;
            this.socket.NoDelay = true;
            this.innArgs.Completed += OnComplete;
            this.outArgs.Completed += OnComplete;

            this.isConnected = true;
            this.isSending = 0;
        }

        private void OnError(SocketError e)
        {
            if (this.isDisposed == 1)
            {
                return;
            }

            this.ErrorCallback?.Invoke(this, (int)e);
        }

        private void OnRead(SocketMessage message)
        {
            this.ReadCallback?.Invoke(message);
        }

        public void Send(byte[] bytes)
        {
            if (this.isDisposed == 1)
            {
                throw new SocketException("TCPClient is disposed, send message error");
            }

            ushort len = (ushort)bytes.Length;
            this.packetSizeCache.WriteTo(0, len);

            this.sendBuffer.Write(this.packetSizeCache, 0, 2);
            this.sendBuffer.Write(bytes, 0, len);

            if(Interlocked.CompareExchange(ref isSending, 1, 0) == 0)
            {
                StartSend();
            }
        }

        public void Start()
        {
            if (!this.isConnected)
            {
                ConnectAsync(this.RemoteAddress);
                return;
            }

            StartRecv();

            if(Interlocked.CompareExchange(ref isSending, 1, 0) == 0)
            {
                this.StartSend();
            }
        }

        private void OnComplete(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    OnConnectComplete(e);
                    break;
                case SocketAsyncOperation.Receive:
                    OnRecvComplete(e);
                    break;
                case SocketAsyncOperation.Send:
                    OnSendComplete(e);
                    break;
                case SocketAsyncOperation.Disconnect:
                    OnDisconnectComplete(e);
                    break;
                default:
                    throw new SocketException($"socket error: { e.LastOperation }");
            }
        }

        private void StartSend()
        {
            if(!this.isConnected)
            {
                this.isSending = 0;
                return;
            }

            int sendSize = CircularBuffer.CHUNK_SIZE - this.sendBuffer.FirstIndex;
            int bufferLength = (int)this.sendBuffer.Length;
            if(sendSize > bufferLength)
            {
                sendSize = bufferLength; 
            }

            SendAsync(this.sendBuffer.First, this.sendBuffer.FirstIndex, sendSize);
        }

        private void SendAsync(byte[] buff, int offset, int count)
        {
            try
            {
                this.outArgs.SetBuffer(buff, offset, count);
            }
            catch(Exception ex)
            {
                throw new SocketException($"socket set buffer error: { buff.Length }, { offset }, { count }", ex);
            }

            if(this.socket.SendAsync(this.outArgs))
            {
                return;
            }

            OnSendComplete(this.outArgs);
        }

        private void OnSendComplete(object o)
        {
            if(this.socket == null)
            {
                return;
            }

            SocketAsyncEventArgs e = (SocketAsyncEventArgs)o;
            if(e.SocketError != SocketError.Success)
            {
                OnError(e.SocketError);
                return;
            }

            this.sendBuffer.FirstIndex += e.BytesTransferred;
            if(this.sendBuffer.FirstIndex == CircularBuffer.CHUNK_SIZE)
            {
                this.sendBuffer.FirstIndex = 0;
                this.sendBuffer.RemoveFirst();
            }
            if(this.sendBuffer.Length == 0)
            {
                this.isSending = 0;
                return;
            }

            if(isSending == 1)
            {
                this.StartSend();
            }
        }

        private void ConnectAsync(IPEndPoint ipEndPoint)
        {
            this.outArgs.RemoteEndPoint = ipEndPoint;
            if(this.socket.ConnectAsync(this.outArgs))
            {
                return;
            }

            OnConnectComplete(this.outArgs);
        }

        private void OnConnectComplete(object o)
        {
            if(this.socket == null)
            {
                return;
            }

            SocketAsyncEventArgs e = (SocketAsyncEventArgs)o;
            if(e.SocketError != SocketError.Success)
            {
                OnError(e.SocketError);
                return;
            }

            e.RemoteEndPoint = null;
            this.isConnected = true;

            StartRecv();

            if(Interlocked.CompareExchange(ref isSending, 1, 0) == 0)
            {
                StartSend();
            }
        }

        private void StartRecv()
        {
            int size = CircularBuffer.CHUNK_SIZE - this.recvBuffer.LastIndex;
            RecvAsync(this.recvBuffer.Last, this.recvBuffer.LastIndex, size);
        }

        private void RecvAsync(byte[] buff, int offset, int count)
        {
            try
            {
                this.innArgs.SetBuffer(buff, offset, count);
            }
            catch(Exception ex)
            {
                throw new SocketException($"socket set buffer error: { buff.Length }, { offset }, { count }", ex);
            }

            if(this.socket.ReceiveAsync(this.innArgs))
            {
                return;
            }

            OnRecvComplete(this.innArgs);
        }

        private void OnRecvComplete(object o)
        {
            if(this.socket == null)
            {
                return;
            }

            SocketAsyncEventArgs e = (SocketAsyncEventArgs)o;
            if(e.SocketError != SocketError.Success)
            {
                OnError(e.SocketError);
                return;
            }

            if(e.BytesTransferred == 0)
            {
                OnError(e.SocketError);
                return;
            }

            this.recvBuffer.LastIndex += e.BytesTransferred;
            if(this.recvBuffer.LastIndex == CircularBuffer.CHUNK_SIZE)
            {
                this.recvBuffer.AddLast();
                this.recvBuffer.LastIndex = 0;
            }

            while(true)
            {
                if (!this.parser.Parse())
                {
                    break;
                }

                try
                {
                    this.OnRead(this.parser.GetPacket());
                }
                catch (Exception ex)
                {
                    //Log.Error(ex.ToString());
                    OnError(SocketError.SocketError);
                }
            }

            if(this.socket == null)
            {
                return;
            }

            StartRecv();
        }

        private void OnDisconnectComplete(object o)
        {
            SocketAsyncEventArgs e = (SocketAsyncEventArgs)o;
            OnError(e.SocketError);
        }

        public void Dispose()
        {
            if(Interlocked.CompareExchange(ref isDisposed, 1, 0) == 1)
            {
                return;
            }

            this.isConnected = false;
            this.socket.Close();
            this.innArgs.Dispose();
            this.outArgs.Dispose();
            this.innArgs = null;
            this.outArgs = null;
            this.socket = null;
        }
    }
}
