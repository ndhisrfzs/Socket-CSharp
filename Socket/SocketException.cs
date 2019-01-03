using System;

namespace GameSocket
{
    public class SocketException : Exception
    {
        public SocketException(string message) : base("Socket Error:" + message)
        {
        }

        public SocketException(string message, Exception exception) : base("Socket Error:" + message, exception)
        {

        }
    }
}
