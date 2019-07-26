using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DeskStreamer
{
    static class Serializer
    {
        public static byte[] ObjectToBytes(object obj)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static object BytesToObj(byte[] bytes, int size)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                ms.Write(bytes, 0, bytes.Length);
                ms.Seek(0, SeekOrigin.Begin);
                var obj = formatter.Deserialize(ms);
                return obj;
            }
        }
    }
}
