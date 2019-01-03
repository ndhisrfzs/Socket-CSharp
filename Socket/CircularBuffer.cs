using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace GameSocket
{
    public class CircularBuffer : Stream
    {
        private static readonly ConcurrentQueue<byte[]> bufferCache = new ConcurrentQueue<byte[]>();

        public const int CHUNK_SIZE = 8192;
        private readonly Queue<byte[]> bufferQueue = new Queue<byte[]>();

        public int LastIndex { get; set; }
        public int FirstIndex { get; set; }

        private byte[] lastBuffer;

        public CircularBuffer()
        {
            AddLast();
        }

        public override long Length
        {
            get
            {
                int c = 0;
                if(this.bufferQueue.Count == 0)
                {
                    c = 0;
                }
                else
                {
                    c = (this.bufferQueue.Count - 1) * CHUNK_SIZE + this.LastIndex - this.FirstIndex;
                }

                return c;
            }
        }

        public void AddLast()
        {
            byte[] buffer;
            if(!bufferCache.TryDequeue(out buffer))
            {
                buffer = new byte[CHUNK_SIZE];
            }
            this.bufferQueue.Enqueue(buffer);
            this.lastBuffer = buffer;
        }

        public void RemoveFirst()
        {
            byte[] buffer;
            if (this.bufferQueue.TryDequeue(out buffer))
            {
                bufferCache.Enqueue(buffer);
            }
        }

        public byte[] First
        {
            get
            {
                if(this.bufferQueue.Count == 0)
                {
                    AddLast();
                }
                return this.bufferQueue.Peek();
            }
        }

        public byte[] Last
        {
            get
            {
                if(this.bufferQueue.Count == 0)
                {
                    AddLast();
                }

                return this.lastBuffer;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if(buffer.Length < offset + count)
            {
                throw new Exception($"BufferList length < count, buffer length: { buffer.Length } { offset } { count }");
            }

            long length = this.Length;
            if(length < count)
            {
                count = (int)length;
            }

            int alreadyCopyCount = 0;
            while(alreadyCopyCount < count)
            {
                int n = count - alreadyCopyCount;
                if(CHUNK_SIZE - this.FirstIndex > n)
                {
                    Buffer.BlockCopy(this.First, this.FirstIndex, buffer, alreadyCopyCount + offset, n);
                    this.FirstIndex += n;
                    alreadyCopyCount += n;
                }
                else
                {
                    Buffer.BlockCopy(this.First, this.FirstIndex, buffer, alreadyCopyCount + offset, CHUNK_SIZE - this.FirstIndex);
                    alreadyCopyCount += CHUNK_SIZE - this.FirstIndex;
                    this.FirstIndex = 0;
                    RemoveFirst();
                }
            }

            return count;
        }

        object writeLock = new object();
        public override void Write(byte[] buffer, int offset, int count)
        {
            lock(writeLock)
            {
                int alreadyCopyCount = 0;
                while (alreadyCopyCount < count)
                {
                    if (this.LastIndex == CHUNK_SIZE)
                    {
                        AddLast();
                        this.LastIndex = 0;
                    }

                    int n = count - alreadyCopyCount;
                    if (CHUNK_SIZE - this.LastIndex > n)
                    {
                        Buffer.BlockCopy(buffer, alreadyCopyCount + offset, this.lastBuffer, this.LastIndex, n);
                        this.LastIndex += count - alreadyCopyCount;
                        alreadyCopyCount += n;
                    }
                    else
                    {
                        Buffer.BlockCopy(buffer, alreadyCopyCount + offset, this.lastBuffer, this.LastIndex, CHUNK_SIZE - this.LastIndex);
                        alreadyCopyCount += CHUNK_SIZE - this.LastIndex;
                        this.LastIndex = CHUNK_SIZE;
                    }
                }
            }
        }

        public override bool CanRead => throw new NotImplementedException();

        public override bool CanSeek => throw new NotImplementedException();

        public override bool CanWrite => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }
}
