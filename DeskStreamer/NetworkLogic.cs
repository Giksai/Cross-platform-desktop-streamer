using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;

namespace DeskStreamer
{
    static class NetworkLogic
    {
        private static Socket incomingConnection;
        private static Socket outgoingConnection;
        private static Socket listener;
        private static MainWindow main;
        private static Semaphore semaphore = new Semaphore(3, 3);

        private static IPAddress localIP
        {
            get
            {
                foreach (var ip in Dns.GetHostAddresses(Dns.GetHostName()))
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        return ip;
                return null;
            }
        }

        private static string networkIpPart;

        static NetworkLogic()
        {
            string[] sections = localIP.ToString().Split('.');
            networkIpPart = sections[0] + '.' + sections[1] + '.' + sections[2] + '.';
        }

        public static void Search()
        {
            for(int i = 1; i < 255; i++)
            {
                new ParameterizedThreadStart(SearchUnit).Invoke(i);
            }
        }

        private static void SearchUnit(object ipPosition)
        {
            try
            {
                int position = (int)ipPosition;
                semaphore.WaitOne();
                ConsoleLogic.WriteConsole("Accessing " + ipPosition);
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SendTimeout = 2;
                try
                {
                    socket.Connect(new IPEndPoint(IPAddress.Parse(networkIpPart + ipPosition), 6897));
                    socket.Send(Serializer.ObjectToBytes(new SearchRequest()));
                    byte[] data = new byte[socket.ReceiveBufferSize];
                    int bytes = 0;
                    do
                    {
                        bytes = socket.Receive(data);
                    } while (socket.Available > 0);
                    object obj = Serializer.BytesToObj(data, bytes);
                    if(obj is SearchResponse)
                    {
                        SearchResponse sr = obj as SearchResponse;
                        lock(main.ipVBox)
                        {
                            main.ipVBox.Add(new Gtk.Label(sr.IPAdress + "\n" + sr.PCName));
                            main.ShowAll();
                        }
                    }
                }
                catch
                {
                    ConsoleLogic.WriteConsole("Failed accessing " + networkIpPart + ipPosition);
                }
                finally
                {
                    socket.Dispose();
                }
            }
            catch(Exception e)
            {
                ConsoleLogic.WriteConsole("Error at searching", e);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private static void ListenLoop()
        {
            listener = new Socket(AddressFamily.InterNetwork, 
                SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(localIP, 6897));
            listener.Listen(10);

            BinaryFormatter formatter = new BinaryFormatter();
            while(true)
            {
                incomingConnection = listener.Accept();
                int bytes = 0;
                byte[] data = new byte[incomingConnection.ReceiveBufferSize];
                do
                {
                    bytes = incomingConnection.Receive(data);
                } while (incomingConnection.Available > 0);

                object obj = Serializer.BytesToObj(data, bytes);
                if (obj is SearchRequest)
                    incomingConnection.Send(Serializer.ObjectToBytes(new SearchResponse(localIP.ToString(), Dns.GetHostName())));
                if(obj is ConnectionRequest)
                {
                    ConsoleLogic.WriteConsole("Connection request from " +
                        (obj as ConnectionRequest).IPAdress);
                }
            }
        }

        public static void GetIPVBoxRef(MainWindow mainRef) => main = mainRef;

    }


}
