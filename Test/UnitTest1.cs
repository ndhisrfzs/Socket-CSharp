using GameSocket;
using GameSocket.TCP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            ISocketService ss = new TcpSocketService(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 8888), ServiceCallback);

            void ServiceCallback(ISocketClient client)
            {
                client.ReadCallback += (p)=> { Debug.WriteLine(p.Length); };
            }

            ISocketClient sc = ss.ClientConnect("127.0.0.1:8888");
            sc.Start();
            while (true)
            {
                sc.Send(new byte[] { 1, 2, 3, 4 });
            }
        }
    }
}
