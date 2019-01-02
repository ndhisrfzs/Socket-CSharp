using System;

namespace GameSocket
{
    public class SocketException : Exception
    {
        public SocketException(string message) : base("Socket Error:" + message)
        {
        }
    }
}
