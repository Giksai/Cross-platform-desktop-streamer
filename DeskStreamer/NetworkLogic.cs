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
        //private static Semaphore semaphore = new Semaphore(100, 100);
        private static List<string> foundIPs = new List<string>();

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
        private static string nodeIpPart;

        static NetworkLogic()
        {
            string[] sections = localIP.ToString().Split('.');
            networkIpPart = sections[0] + '.' + sections[1] + '.' + sections[2] + '.';
            nodeIpPart = sections[3];
        }


        public static void Search() => new Task(InitSearch).Start();
        public static void Listen() => new Task(ListenLoop).Start();

        private static void SearchUnit(object nodeIpNumber)
        {
            try
            {
                main.currIP.Text = networkIpPart + (int)nodeIpNumber;
                main.currIP.Show();
                //semaphore.WaitOne();
                //ConsoleLogic.WriteConsole("Accessing " + position);
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SendTimeout = 2;
                try
                {
                    IAsyncResult result = socket.BeginConnect(new IPEndPoint(
                        IPAddress.Parse(networkIpPart + (int)nodeIpNumber), 6897), null, null);
                    bool connected = result.AsyncWaitHandle.WaitOne(1000, true);
                    //socket.Connect(new IPEndPoint(IPAddress.Parse(networkIpPart + ipPosition), 6897));
                    if (!connected) throw new Exception();
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
                        //ConsoleLogic.WriteConsole("Got search response from " + sr.IPAdress);
                        lock (main.ipVBox)
                        {
                            Gtk.Button connectBtn = new Gtk.Button("Connect to " + sr.PCName + " \n " + sr.IPAdress);
                            connectBtn.Name = sr.IPAdress;
                            connectBtn.Clicked += ConnectTo;
                            foreach(var child in main.ipVBox.AllChildren)
                            {
                                if (((Gtk.Button)child).Name == sr.IPAdress)
                                    throw new Exception("Button already present in list");
                            }
                            main.ipVBox.Add(connectBtn);
                            main.ShowAll();

                        }
                    }
                }
                catch(Exception e)
                {
                    if(e.Message != "Button already present in list")
                    foreach (var child in main.ipVBox.AllChildren)
                        if (((Gtk.Button)child).Name == networkIpPart + (int)nodeIpNumber)
                        {
                            main.ipVBox.Remove((Gtk.Button)child);
                                main.ipVBox.Show();
                            ConsoleLogic.WriteConsole("Lost connection with " + networkIpPart + (int)nodeIpNumber);
                        }
                    //ConsoleLogic.WriteConsole("Failed accessing " + networkIpPart + position);
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
                //semaphore.Release();
            }
        }

        private static void ConnectTo(object sender, EventArgs args)
        {
            string connectIP = ((Gtk.Button)sender).Name;
            ConsoleLogic.WriteConsole("Connecting to " + connectIP);
            Socket connectionSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                IAsyncResult result = connectionSocket.BeginConnect(new IPEndPoint(
                        IPAddress.Parse(connectIP), 6897), null, null);
                bool connected = result.AsyncWaitHandle.WaitOne(3000, true);
                if (!connected) throw new Exception();
                connectionSocket.Send(Serializer.ObjectToBytes(new ConnectionRequest(localIP.ToString())));
            }
            catch(Exception e)
            {
                main.ipVBox.Remove((Gtk.Button)sender);
                main.ipVBox.Show();
                ConsoleLogic.WriteConsole("Lost connection with " + connectIP);
            }
            
        }

        private static void InitSearch()
        {
            while(true)
            {
                for (int i = 1; i < 255; i++)
                {
                    if (i == int.Parse(nodeIpPart)) continue;
                    //Task.Run(() => SearchUnit(i));
                    //Task tsk1 = new Task(() => SearchUnit(i));
                    //tsk1.Start();

                    Thread thr = new Thread(SearchUnit);
                    thr.Name = i.ToString();
                    thr.Start(i);
                    Thread.Sleep(40);
                }
                Thread.Sleep(1000);
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
