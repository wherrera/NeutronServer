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
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Neutron.Server
{
    public class Client {
        
        private TcpClient m_Client;
        private Thread m_HandlerThread;
        private NeutronServer m_Server;
        private DateTime m_Created;

        public Client (NeutronServer server, TcpClient client) {
            m_Server = server;
            m_Client = client;
            m_Created = DateTime.Now;
        }

        public int Id {
            get {
                return ((IPEndPoint)m_Client.Client.RemoteEndPoint).Port;
            }
        }

        void ClientHandler()
        {
            while(m_Server.Running)
            {
                if(!m_Client.Connected) {
                    m_Server.RemoveClient(this);
                    return;
                }
                try
                {
                    if(m_Client.Available > 0)
                    {
                        Packet packet = Packet.Read(m_Client.GetStream());
                        m_Server.ProcessPacket(this, packet);
                    }
                    else {
                        Thread.Sleep(10);
                    }
                } catch(Exception e) {
                    Console.WriteLine("Exception: {0}", e);
                    continue;
                }
            }
        }

        public bool Start() {            
            try {
                m_HandlerThread = new Thread(ClientHandler);
                m_HandlerThread.Start();
                return true;
            } catch(Exception e) {
                Console.WriteLine("Exception: {0}", e);
                return false;
            }
        }

        public void Disconnect()
        {
            try{
                m_Client.Close();
            } finally {
                m_Client = null;
            }
            try {
                m_HandlerThread.Join(1000);
            } finally {
                m_HandlerThread = null;
            }
        }

        public void Send(string eventId, string eventData)
        {
            NetworkStream stream = m_Client.GetStream();
            MemoryStream data = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(data);
            writer.Write(eventId);
            writer.Write(eventData);
            byte[] payload = data.ToArray();
            stream.WriteByte(Packet.MSG_EVENT);
            stream.Write(BitConverter.GetBytes(payload.Length), 0, 4);
            stream.Write(payload, 0, payload.Length);
        }
    }
}