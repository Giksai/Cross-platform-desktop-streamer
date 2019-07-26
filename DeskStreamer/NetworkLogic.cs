using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                    incomingConnection.Send(Serializer.ObjectToBytes(new SearchResponse(localIP.ToString(), )));
                if(obj is ConnectionRequest)
                {
                    ConsoleLogic.WriteConsole("Connection request from " + 
                        (obj as ConnectionRequest).IPAdress)
                }
            }
        }

    }


}
