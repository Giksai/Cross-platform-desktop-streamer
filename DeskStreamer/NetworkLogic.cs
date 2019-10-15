using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.Timers;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;

namespace DeskStreamer
{
    static class NetworkLogic
    {
        private static Socket incomingConnection;
        private static Socket pipe;
        private static Socket listener;
        private static MainWindow main;
        //private static bool performSearch = true;
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
            Task dataListenLoopThr = new Task(DataListenLoop);
            dataListenLoopThr.Start();
            Thread commandListenLoopThr = new Thread(CommandListenLoop);
            commandListenLoopThr.Start();
            Thread connectionCheckLoopThr = new Thread(ConnectionCheckLoop);
            connectionCheckLoopThr.Start();
        }

        //private static Thread searchTask = new Thread(InitSearch);
        //public static void Search() => searchTask.Start();

        public static void ConnectBtnPressed(object sender, EventArgs args)
        {
            ConnectTo(main.connectIP.Text);
        }
        private static void ConnectTo(string Ip)
        {
            try
            {
               
                ConsoleLogic.WriteConsole("Connecting to " + Ip);
                Socket connectionSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                IAsyncResult result = connectionSocket.BeginConnect(new IPEndPoint(
                        IPAddress.Parse(Ip), 6897), null, null);
                bool connected = result.AsyncWaitHandle.WaitOne(1000, true);
                if (!connected) throw new Exception();
                InitScreen();
                connectionSocket.Send(Serializer.ObjectToBytes(new ConnectionRequest(localIP.ToString())));
                //listen
                byte[] data = new byte[connectionSocket.ReceiveBufferSize];
                int bytes = 0;
                do
                {
                    bytes = connectionSocket.Receive(data);
                } while (connectionSocket.Available > 0);
                object obj = Serializer.BytesToObj(data, bytes);
                if (obj is ConnectionResponse)
                {

                }
            }
            catch(Exception e)
            {
                //main.ipVBox.Remove((Gtk.Button)sender);
                //main.ShowAll();
                ConsoleLogic.WriteConsole("Lost connection with " + Ip, e);
            }
            
        }
        static StreamingWindow strWin;
        static Gtk.Button discBtn;
        private static void InitScreen()
        {
            //performSearch = false;
            discBtn = new Gtk.Button("Disconnect");
            discBtn.Clicked += (sender, args) => disconnectRequest = true;
            main.rightSide.Add(discBtn);
            main.ShowAll();
            strWin = new StreamingWindow();
            strWin.Show();
        }
        private static void DataListenLoop()
        {
            try
            {
                int cyclesCount = 0;
                System.Timers.Timer timer = new System.Timers.Timer(1000);
                timer.Elapsed += (sender, args) =>
                {
                    main.cycleSpeedReceive.Text = cyclesCount.ToString();
                    cyclesCount = 0;
                };
                timer.Enabled = true;
                while (true)
                {
                    
                    Socket pipeListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    pipeListener.Bind(new IPEndPoint(localIP, 4578));
                    pipeListener.Listen(10);
                    pipe = pipeListener.Accept();
                    //int startTime = DateTime.Now.Millisecond;
                    while (pipe.Connected)
                    {
                        cyclesCount++;
                        //main.cycleSpeedReceive.Text = (DateTime.Now.Millisecond - startTime).ToString();
                        //startTime = DateTime.Now.Millisecond;
                        //Thread.Sleep(10);
                        int bytes = 0;
                        byte[] data = new byte[30000];
                        do
                        {
                            bytes = pipe.Receive(data);
                        } while (pipe.Available > 0);
                        try
                        {
                            object imgData = Serializer.BytesToObj(data, bytes);
                            if (!(imgData is ImageStreamPart)) throw new Exception("Wrong data!");
                            data = (imgData as ImageStreamPart).bitmap;
                            //decompressing
                            byte[] decompressedImage = new byte[(imgData as ImageStreamPart).originalSize];
                            LZ4Codec.Decode(data, decompressedImage);
                            
                                strWin.img.Pixbuf = new Gdk.Pixbuf(decompressedImage);

                            if (disconnectRequest)
                            {
                                pipe.Disconnect(true);
                                pipe.Dispose();
                                pipe = null;
                                discBtn.Hide();
                                discBtn.Destroy();
                                discBtn.Dispose();
                                disconnectRequest = false;
                                strWin.Hide();
                                strWin.Dispose();
                                strWin = null;
                                break;
                                //searchTask.Start();
                            }
                        }
                        catch (Exception e1)
                        {
                            ConsoleLogic.WriteConsole("Error at converting received data");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ConsoleLogic.WriteConsole("error at DataListenLoop");
            }
        }

        private static void CommandListenLoop()
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
                    Thread.Sleep(10);
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
                        ConsoleLogic.WriteConsole("Connection request from " +
                            (obj as ConnectionRequest).IPAdress);
                        incomingConnection.Send(Serializer.ObjectToBytes(new ConnectionResponse()));
                        Task thr1 = new Task(()=>Stream((obj as ConnectionRequest).IPAdress));
                        thr1.Start();
                    }
                }
            }
            catch(Exception e)
            {
                ConsoleLogic.WriteConsole("error at CommandListenLoop", e);
            }
            
        }

        

        private static void Stream(string ip)
        {
            Thread.Sleep(300);
            int miliseconds = DateTime.Now.Millisecond;
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
                    ConsoleLogic.WriteConsole("error at connecting back to receiver");
                }
            }
            //performSearch = false;
            int cyclesCount = 0;
            int fps = 0;
            int sleepTime = 0;

            System.Timers.Timer timer = new System.Timers.Timer(1000);
            timer.Elapsed += (sender, args)=>
                {
                    main.cycleSpeedSend.Text = cyclesCount.ToString();
                    fps = cyclesCount;
                    sleepTime = (int)(1000 / (((main.FPS.Value - fps) / 60) * 1000));
                    main.thrSleepTime.Text = sleepTime.ToString();
                    cyclesCount = 0;
                };
            timer.Enabled = true;
            Bitmap prevImage = null;
            while(true)
            {
                try
                {
                    cyclesCount++;
                    //while((DateTime.Now.Millisecond - miliseconds) <= 1000/main.FPS.Value)
                    //{
                    //    Thread.Sleep(1);
                    //}
                    //miliseconds = DateTime.Now.Millisecond;
                    if(fps < main.FPS.Value)
                    {
                        if(main.FPS.Value - fps > 20)
                        {
                        }
                        else
                        {
                            Thread.Sleep((int)(1000 / (((main.FPS.Value - fps)/ 60) * 1000)));
                        }
                    }
                    else { }
                    if (!pipe.Connected)
                    {
                        //searchTask.Start();
                        return;
                    }
                    int sqrSize = (int)main.strImgSize.Value;
                    
                    Bitmap memoryImage = new Bitmap(sqrSize, sqrSize/*, PixelFormat.Format8bppIndexed*/);
                    Size s = new Size(sqrSize, sqrSize);
                    Graphics memoryGraphics = Graphics.FromImage(memoryImage);
                    memoryGraphics.CopyFromScreen(0, 0, 0, 0, s);
                    //Comparison

                    if (prevImage != null)
                    {
                        if (prevImage.Height == memoryImage.Height)
                        {

                            Bitmap prevHolder = memoryImage.Clone() as Bitmap;
                            memoryImage = GetDifferences(prevImage, memoryImage, Color.Pink);
                            prevImage.Dispose();
                            prevImage = prevHolder;
                            //memoryImage.MakeTransparent(Color.Pink);
                        }
                        else
                            prevImage = memoryImage.Clone() as Bitmap;
                    }
                    else
                        prevImage = memoryImage.Clone() as Bitmap;
                    //Compress
                    using (var stream = new MemoryStream())
                    {
                        EncoderParameter qualityParam = new EncoderParameter(Encoder.Quality, (long)main.strImgCompression.Value);
                        EncoderParameter colorDepthParam = new EncoderParameter(Encoder.ColorDepth, (long)main.strImgCD.Value);
                        EncoderParameter compressionParam = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);
                        ImageCodecInfo imageCodec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(o => o.FormatID == ImageFormat.Jpeg.Guid);
                        EncoderParameters parameters = new EncoderParameters(3);
                        
                        parameters.Param[0] = qualityParam;
                        parameters.Param[1] = colorDepthParam;
                        parameters.Param[2] = compressionParam;
                        //
                        memoryImage.Save(stream, imageCodec, parameters);

                        byte[] compressedData = new byte[LZ4Codec.MaximumOutputSize((int)stream.Length)];

                        LZ4Codec.Encode(stream.GetBuffer(), compressedData, LZ4Level.L12_MAX);

                        main.dataAmount.Text = stream.Length.ToString();
                        main.compressedDataAmount.Text = compressedData.Length.ToString();
                        if (compressedData.Length > 15000) continue;
                        pipe.Send(Serializer.ObjectToBytes(new ImageStreamPart(compressedData, stream.Length)));
                    }
                    //using (var stream = new MemoryStream())
                    //{
                    //    compressedImg.Save(stream, ImageFormat.Jpeg);
                        
                    //}

                    memoryImage.Dispose();
                    memoryGraphics.Dispose();
                    //Thread.Sleep(30);
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
        
        private static unsafe Bitmap GetDifferences(Bitmap image1, Bitmap image2, Color matchColor)
        {
            if (image1 == null | image2 == null)
                return null;

            if (image1.Height != image2.Height || image1.Width != image2.Width)
                return null;

            Bitmap diffImage = image2.Clone() as Bitmap;

            int height = image1.Height;
            int width = image1.Width;

            BitmapData data1 = image1.LockBits(new Rectangle(0, 0, width, height),
                                               ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData data2 = image2.LockBits(new Rectangle(0, 0, width, height),
                                               ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData diffData = diffImage.LockBits(new Rectangle(0, 0, width, height),
                                                   ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            byte* data1Ptr = (byte*)data1.Scan0;
            byte* data2Ptr = (byte*)data2.Scan0;
            byte* diffPtr = (byte*)diffData.Scan0;

            byte[] swapColor = new byte[3];
            swapColor[0] = matchColor.B;
            swapColor[1] = matchColor.G;
            swapColor[2] = matchColor.R;

            int rowPadding = data1.Stride - (image1.Width * 3);

            // iterate over height (rows)
            for (int i = 0; i < height; i++)
            {
                // iterate over width (columns)
                for (int j = 0; j < width; j++)
                {
                    int same = 0;

                    byte[] tmp = new byte[3];

                    // compare pixels and copy new values into temporary array
                    for (int x = 0; x < 3; x++)
                    {
                        tmp[x] = data2Ptr[0];
                        if (data1Ptr[0] == data2Ptr[0])
                        {
                            same++;
                        }
                        data1Ptr++; // advance image1 ptr
                        data2Ptr++; // advance image2 ptr
                    }

                    // swap color or add new values
                    for (int x = 0; x < 3; x++)
                    {
                        diffPtr[0] = (same == 3) ? swapColor[x] : tmp[x];
                        diffPtr++; // advance diff image ptr
                    }
                }

                // at the end of each column, skip extra padding
                if (rowPadding > 0)
                {
                    data1Ptr += rowPadding;
                    data2Ptr += rowPadding;
                    diffPtr += rowPadding;
                }
            }

            image1.UnlockBits(data1);
            image2.UnlockBits(data2);
            diffImage.UnlockBits(diffData);

            return diffImage;
        }

        private static void ConnectionCheckLoop()
        {
            //main.connectionStatus.Pixbuf = new Gdk.Pixbuf("red.jpg");
            //while (pipe == null)
            //{
            //    Thread.Sleep(100);
            //}
            //bool status = false;
            //status = pipe.Connected;
            //if (pipe.Connected)
            //    main.connectionStatus.Pixbuf = new Gdk.Pixbuf("green.jpg");
            //else
            //    main.connectionStatus.Pixbuf = new Gdk.Pixbuf("red.jpg");
            //while (true)
            //{
            //    if(pipe != null)
            //    {
            //        if (pipe.Connected && status == false)
            //        {
            //            main.connectionStatus.Pixbuf = new Gdk.Pixbuf("green.jpg");
            //            status = true;
            //        }
            //        else if (!pipe.Connected && status == true)
            //        {
            //            main.connectionStatus.Pixbuf = new Gdk.Pixbuf("red.jpg");
            //            status = false;
            //            if (discBtn != null)
            //                HideConnectionControls();
            //        }
            //    }
            //    else if(pipe == null && status == true)
            //    {
            //        status = false;
            //        main.connectionStatus.Pixbuf = new Gdk.Pixbuf("red.jpg");
            //        if (discBtn != null)
            //            HideConnectionControls();
            //    }
            //    Thread.Sleep(50);
            //}
        }

        private static void HideConnectionControls()
        {
            discBtn.Hide();
            discBtn.Destroy();
            discBtn.Dispose();

            strWin.Hide();
            strWin.Destroy();
            strWin.Dispose();
        }

        public static void GetIPVBoxRef(MainWindow mainRef) => main = mainRef;

    }


}


//Мусор кода






//private static void InitSearch()
//{
//    try
//    {
//        while (true)
//        {
//            for (int i = 1; i < 255; i++)
//            {
//                if (!performSearch) return;
//                if (i == int.Parse(nodeIpPart)) continue;
//                //Task.Run(() => SearchUnit(i));
//                //Task tsk1 = new Task(() => SearchUnit(i));
//                //tsk1.Start();

//                Thread thr = new Thread(SearchUnit);
//                thr.Name = i.ToString();
//                thr.Start(i);
//                Thread.Sleep(40);
//            }
//            Thread.Sleep(1000);
//            main.ShowAll();
//        }
//    }
//    catch(Exception e)
//    {
//        ConsoleLogic.WriteConsole("Error at initSearch", e);
//    }
//}

//private static void SearchUnit(object nodeIpNumber)
//{
//    try
//    {
//        main.currIP.Text = networkIpPart + (int)nodeIpNumber;
//        main.currIP.Show();
//        //semaphore.WaitOne();
//        //ConsoleLogic.WriteConsole("Accessing " + position);
//        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//        socket.SendTimeout = 2;
//        try
//        {
//            IAsyncResult result = socket.BeginConnect(new IPEndPoint(
//                IPAddress.Parse(networkIpPart + (int)nodeIpNumber), 6897), null, null);
//            bool connected = result.AsyncWaitHandle.WaitOne(1000, true);
//            //socket.Connect(new IPEndPoint(IPAddress.Parse(networkIpPart + ipPosition), 6897));
//            if (!connected) throw new Exception();
//            socket.Send(Serializer.ObjectToBytes(new SearchRequest()));
//            byte[] data = new byte[socket.ReceiveBufferSize];
//            int bytes = 0;
//            do
//            {
//                bytes = socket.Receive(data);
//            } while (socket.Available > 0);
//            object obj = Serializer.BytesToObj(data, bytes);
//            if(obj is SearchResponse)
//            {
//                SearchResponse sr = obj as SearchResponse;
//                //ConsoleLogic.WriteConsole("Got search response from " + sr.IPAdress);
//                lock (main.ipVBox)
//                {
//                    Gtk.Button connectBtn = new Gtk.Button("Connect to " + sr.PCName + " \n " + sr.IPAdress);
//                    connectBtn.Name = sr.IPAdress;
//                    connectBtn.Clicked += (sender, args)=>new Thread(()=>ConnectTo(sender, args)).Start();
//                    foreach(var child in main.ipVBox.AllChildren)
//                    {
//                        if (((Gtk.Button)child).Name == sr.IPAdress)
//                            throw new Exception("Button already present in list");
//                    }
//                    main.ipVBox.Add(connectBtn);
//                    main.ShowAll();

//                }
//            }
//        }
//        catch(Exception e)
//        {
//            if(e.Message != "Button already present in list")

//            foreach (var child in main.ipVBox.AllChildren)
//                if (((Gtk.Button)child).Name == networkIpPart + (int)nodeIpNumber)
//                {
//                    main.ipVBox.Remove((Gtk.Button)child);
//                        main.ShowAll();
//                    ConsoleLogic.WriteConsole("Lost connection with " + networkIpPart + (int)nodeIpNumber);
//                }
//            //ConsoleLogic.WriteConsole("Failed accessing " + networkIpPart + position);
//        }
//        finally
//        {
//            socket.Dispose();
//        }
//    }
//    catch(Exception e)
//    {
//        ConsoleLogic.WriteConsole("Error at searching", e);
//    }
//    finally
//    {
//        //semaphore.Release();
//    }
//}