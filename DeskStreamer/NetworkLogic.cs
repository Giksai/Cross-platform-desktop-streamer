using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Drawing;

namespace DeskStreamer
{
    static class NetworkLogic
    {
        private static Socket incomingConnection;
        private static Socket pipe;
        private static Socket listener;
        private static MainWindow main;
        private static bool performSearch = true;
        private static bool disconnectRequest = false;
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

        private static Thread searchTask = new Thread(InitSearch);
        public static void Search() => searchTask.Start();
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
                            connectBtn.Clicked += (sender, args)=>new Thread(()=>ConnectTo(sender, args)).Start();
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
                                main.ShowAll();
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
            try
            {
               
                ConsoleLogic.WriteConsole("Connecting to " + connectIP);
                Socket connectionSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                IAsyncResult result = connectionSocket.BeginConnect(new IPEndPoint(
                        IPAddress.Parse(connectIP), 6897), null, null);
                bool connected = result.AsyncWaitHandle.WaitOne(3000, true);
                if (!connected) throw new Exception();
                connectionSocket.Send(Serializer.ObjectToBytes(new ConnectionRequest(localIP.ToString())));
                //listen
                byte[] data = new byte[connectionSocket.ReceiveBufferSize];
                int bytes = 0;
                do
                {
                    bytes = connectionSocket.Receive(data);
                } while (connectionSocket.Available > 0);
                object obj = Serializer.BytesToObj(data, bytes);
                if (obj is ConnectionResponse) InitScreen();
            }
            catch(Exception e)
            {
                main.ipVBox.Remove((Gtk.Button)sender);
                main.ShowAll();
                ConsoleLogic.WriteConsole("Lost connection with " + connectIP);
            }
            
        }
        private static void InitScreen()
        {
            try
            {
                performSearch = false;
                Gtk.Button discBtn = new Gtk.Button("Disconnect");
                discBtn.Clicked += (sender, args) =>
                {
                    disconnectRequest = true;
                };
                main.rightSide.Add(discBtn);
                main.ShowAll();
                  
                //searchTask.Abort();
                //searchTask.Join();
                //Thread.Sleep(2000);
                Socket pipeListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                pipeListener.Bind(new IPEndPoint(localIP, 4578));
                pipeListener.Listen(10);
                pipe = pipeListener.Accept();
                StreamingWindow strWin = new StreamingWindow();
                new Task(() =>
                {
                    strWin.Show();
                }).Start();
                
                while (true)
                {
                    if(disconnectRequest)
                    {
                        pipe.Disconnect(true);
                        pipe.Dispose();
                        disconnectRequest = false;
                        searchTask.Start();
                        return;
                    }
                    int bytes = 0;
                    byte[] data = new byte[100000];
                    do
                    {
                        bytes = pipe.Receive(data);
                    } while (pipe.Available > 0);
                    try
                    {
                        object imgData = Serializer.BytesToObj(data, bytes);
                        if (!(imgData is ImageStreamPart)) throw new Exception("Wrong data!");
                        data = (imgData as ImageStreamPart).bitmap;
                        //Bitmap img = (Bitmap)new ImageConverter().ConvertTo(data, typeof(Bitmap));
                        Image bmp;
                        using (Stream ms = new MemoryStream(data))
                        {
                            bmp = Image.FromStream(ms);
                        }
                        new Task(() =>
                        {
                            strWin.img.Pixbuf = new Gdk.Pixbuf(data);
                            strWin.ShowAll();
                        }).Start();
                        
                        //bmp.Save("img.bmp");
                        //using (FileStream str = new FileStream("img.bmp", FileMode.Create))
                        //{
                        //    str.Write(data, 0, data.Length);
                        //    str.Flush();
                        //}
                        
                    }
                    catch(Exception e1)
                    {
                        ConsoleLogic.WriteConsole("Error at converting received data", e1);
                    }
                }
            }
            catch(Exception e)
            {
                ConsoleLogic.WriteConsole("Error at getting stream", e);
                InitScreen();
            }
            
        }


        private static void InitSearch()
        {
            try
            {
                while (true)
                {
                    for (int i = 1; i < 255; i++)
                    {
                        if (!performSearch) return;
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
                    main.ShowAll();
                }
            }
            catch(Exception e)
            {
                ConsoleLogic.WriteConsole("Error at initSearch", e);
            }
        }

        private static void ListenLoop()
        {
            try
            {
                listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(localIP, 6897));
                listener.Listen(10);

                BinaryFormatter formatter = new BinaryFormatter();
                while (true)
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
                    if (obj is ConnectionRequest)
                    {
                        //searchTask.Abort();
                        //searchTask.Join();
                        
                        ConsoleLogic.WriteConsole("Connection request from " +
                            (obj as ConnectionRequest).IPAdress);
                        incomingConnection.Send(Serializer.ObjectToBytes(new ConnectionResponse()));
                        Thread thr1 = new Thread(()=>Stream((obj as ConnectionRequest).IPAdress));
                        thr1.Start();
                    }
                }
            }
            catch(Exception e)
            {
                ConsoleLogic.WriteConsole("error at listenLoop", e);
            }
            
        }

        private static void Stream(string ip)
        {
            Thread.Sleep(1000);
            pipe = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            bool connected = false;
            while(!connected)
            {
                try
                {
                    IAsyncResult result = pipe.BeginConnect(new IPEndPoint(
                        IPAddress.Parse(ip), 4578), null, null);
                    connected = result.AsyncWaitHandle.WaitOne(3000, true);
                }
                catch(Exception e)
                {

                }
            }
            performSearch = false;
            while(true)
            {
                try
                {
                    if (!pipe.Connected)
                    {

                        searchTask.Start();
                        return;
                    }
                    int sqrSize = int.Parse(main.strImgSize.Text);
                    Bitmap memoryImage = new Bitmap(sqrSize, sqrSize);
                    Size s = new Size(sqrSize, sqrSize);
                    Graphics memoryGraphics = Graphics.FromImage(memoryImage);
                    memoryGraphics.CopyFromScreen(0, 0, 0, 0, s);
                    byte[] dataToSend;
                    using (var stream = new MemoryStream())
                    {
                        memoryImage.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                        dataToSend = stream.ToArray();
                        main.dataAmount.Text = stream.Length.ToString();
                        pipe.Send(Serializer.ObjectToBytes(new ImageStreamPart(dataToSend)));
                    }
                    memoryImage.Dispose();
                    memoryGraphics.Dispose();
                    Thread.Sleep(30);
                    //Fix large bitmap size
                    //Serializer.ObjectToBytes(
                    //    new ImageConverter().ConvertTo(
                    //        memoryImage,
                    //        typeof(byte[])))
                }
                catch(Exception e)
                {
                    ConsoleLogic.WriteConsole("Error at sending image", e);
                }
            }
        }

        public static void ConnectionCheckLoop()
        {
            while(true)
            {
                if(pipe != null)
                {
                    if(pipe.Connected)
                    {
                        main.connectionStatus.Pixbuf = new Gdk.Pixbuf("green.jpg");
                    }
                    else
                    {
                        main.connectionStatus.Pixbuf = new Gdk.Pixbuf("red.jpg");
                    }
                }
            }
        }

        public static void GetIPVBoxRef(MainWindow mainRef) => main = mainRef;

    }


}
