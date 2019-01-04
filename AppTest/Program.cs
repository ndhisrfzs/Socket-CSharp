using GameSocket;
using GameSocket.TCP;
using System;
using System.Threading;

namespace AppTest
{
    class Program
    {
        static void Main(string[] args)
        {
            int[] counts = new int[21];
            ISocketService ss = new TcpSocketService(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 8888), ServiceCallback);

            void ServiceCallback(ISocketClient client)
            {
                client.ReadCallback += (p) =>
                {
                    //counts[client.id]++;
                };
                client.Start();
            }

            ISocketClient sc = ss.ClientConnect("127.0.0.1:8888");
            sc.Start();
            for (int i = 0; i < 10; i++)
            {
                Thread t = new Thread(() =>
                {
                    while (true)
                    {
                        Thread.Sleep(1);
                        sc.Send(new byte[] { 1, 2, 3, 4 });
                    }
                });
                t.Start();
            }

            while (true)
            {
                Console.Clear();
                for(int i = 0; i < 21; i++)
                {
                    Console.WriteLine("id:" + i + " count:" + counts[i]);
                }
                Thread.Sleep(100);
            }
        }
    }
}
