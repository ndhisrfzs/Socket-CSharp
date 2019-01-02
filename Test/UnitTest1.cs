using GameSocket;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Test
{
    [TestClass]
    public class UnitTest1
    {
        int[] testv = new int[65535];
        [TestMethod]
        public void TestMethod1()
        {
            for(int i = 0; i < 65535; i++)
            {
                if(testv[i] == 0)
                {
                    testv[i] = 1;
                }
            }
            //for (int i = 0; i < 1000000; i++)
            //{
            //    Activator.CreateInstance<UnitTest1>();
            //}
        }

        readonly ConcurrentQueue<int> queue = new ConcurrentQueue<int>();
        [TestMethod]
        public void TestMethod2()
        {
            for (int i = 0; i < 65535; i++)
            {
                linkedlist.AddFirst(i);
            }

            for (int i = 0; i < 65535; i++)
            {
                linkedlist.RemoveFirst();
            }
        }

        readonly LinkedList<int> linkedlist = new LinkedList<int>();
        [TestMethod]
        public void TestMethod3()
        {
            for (int i = 0; i < 65535; i++)
            {
                queue.Enqueue(i);
            }
            for (int i = 0; i < 65535; i++)
            {
                queue.TryDequeue(out int a);
            }
           
        }

        byte[] buf1 = new byte[8192];

        [TestMethod]
        public void TestMethod4()
        {
            byte[] des = new byte[8192];
            for (int i = 0; i < 100000; i++)
            {
                Array.Copy(buf2, 0, des, 0, 8192);
            }
            
        }

        byte[] buf2 = new byte[8192];
        [TestMethod]
        public void TestMethod5()
        {
            byte[] des = new byte[8192];
            for (int i = 0; i < 100000; i++)
            {
                Buffer.BlockCopy(buf1, 0, des, 0, 8192);
            }
        }
    }
}
