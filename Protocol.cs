/*
MIT License

Copyright (c) 2019 William Herrera

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.IO;

namespace Neutron.Server
{
    public class Protocol
    {    
        private object m_LockObject = new object();
        private Stream m_Stream;

        public const byte MSG_CONNECT = 0x0F;
        public const byte MSG_DISCONNECT = 0x0E;
        public const byte MSG_EVENT = 0x1F;

        public Protocol(Stream stream) {
            m_Stream = stream;
        }

        public void Send(Packet packet)
        {
            lock(m_LockObject) {
                m_Stream.WriteByte(packet.type);
                m_Stream.Write(BitConverter.GetBytes(packet.payload.Length), 0, 4);
                m_Stream.Write(packet.payload, 0, packet.payload.Length);
            }
        }

        public Packet Read()
        {
            byte[] bytes = new byte[5];

            if (m_Stream.Read(bytes, 0, 5) != 5)
                throw new Exception("read packet");

            byte id = bytes[0];

            int size = BitConverter.ToInt32(bytes, 1);

            if (size < 0)
                throw new Exception("read packet");

            byte[] data = new byte[size];
            int downloaded = 0;

            while (downloaded < size)
            {
                int i = m_Stream.Read(data, downloaded, size - downloaded);

                if (i <= 0)
                    throw new Exception("read packet");

                downloaded += i;
            }

            Packet p = new Packet()
            {
                type = id,
                payload = data
            };

            return p;
        }
    }
}