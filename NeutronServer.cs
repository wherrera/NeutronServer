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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Neutron.Server {

    public class NeutronServer
    {
        private bool m_Running;
        private Thread m_Thread;
        private TcpListener m_TcpListener;
        private int m_Port;
        private List<INeutronListener> m_Listeners = new List<INeutronListener>();
        private List<Client> m_Clients = new List<Client>();

        public bool Running {
            get {
                return m_Running;
            }
        }

        public void AddListener(INeutronListener listener) {
            if (!m_Listeners.Contains(listener))
                m_Listeners.Add(listener);
        }

        public void AddClient(Client client) {
            lock(m_Clients) {
                m_Clients.Add(client);
            }
            foreach(INeutronListener listener in m_Listeners) {
                try {
                    listener.OnClientConnected(client);
                } catch(Exception e) {
                    Console.WriteLine("OnClientConnected: " + e.Message);
                }
            }
        }

        public void RemoveClient(Client client) {
            lock(m_Clients) {
                m_Clients.Remove(client);
            }
            foreach(INeutronListener listener in m_Listeners) {
                try{
                    listener.OnClientDisconnected(client);
                } catch(Exception e) {
                    Console.WriteLine("OnClientDisconnected: " + e.Message);
                }
            }
        }

        public void Start(int port) {
            m_Port = port;
            m_TcpListener = new TcpListener(IPAddress.Any, m_Port);
            m_TcpListener.Start();
            m_Thread = new Thread(AcceptHandler);
            m_Thread.Start();
            m_Running = true;
        }

        public void Stop() {
            m_Running = false;
            try {
                if (m_Thread != null && m_Thread.IsAlive)
                    m_Thread.Interrupt();

                m_Thread.Join(1000);
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
            } finally {
                m_Thread = null;
            }
            try {
                if (m_TcpListener != null)
                    m_TcpListener.Stop();
            } catch(Exception e) {
                Console.WriteLine(e.Message);
            } finally {
                m_TcpListener = null;
            }
        }

        public void ProcessPacket(Client client, Packet packet) {
            foreach(INeutronListener listener in m_Listeners) {
                try{
                    listener.OnPacketReceived(client, packet);
                } catch(Exception e) {
                    Console.WriteLine("Exception: {0}", e);
                }
            }
        }

        void RaiseSocketExceptionEvent(SocketException exception)
        {      
            foreach(INeutronListener listener in m_Listeners) {
                try{
                    listener.OnSocketException(exception);
                } catch(Exception e) {
                    Console.WriteLine("Exception: {0}", e);
                }
            }
        }

        void AcceptHandler() {
            while(m_Running)
            {
                TcpClient tcpClient = null;

                try {
                    tcpClient = m_TcpListener.AcceptTcpClient();            
                } catch (SocketException exception) {
                    RaiseSocketExceptionEvent(exception);
                    continue;
                }

                Client client = new Client(this, tcpClient);

                if (client.Start())
                    AddClient(client);
            }
        }
    }
}