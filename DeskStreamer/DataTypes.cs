using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace DeskStreamer
{

    [Serializable]
    class ConnectionRequest
    {
        public string IPAdress;
        public ConnectionRequest(string IPAdress)
        {
            this.IPAdress = IPAdress;
        }
    }

    [Serializable]
    class ConnectionResponse
    {

    }

    [Serializable]
    class SearchRequest
    {

    }

    [Serializable]
    class SearchResponse
    {
        public string IPAdress;
        public string PCName;
        public SearchResponse(string IPAdress, string PCName)
        {
            this.IPAdress = IPAdress;
            this.PCName = PCName;
        }
    }

    [Serializable]
    class ImageStreamPart
    {
        byte[] bitmap;
    }
}
