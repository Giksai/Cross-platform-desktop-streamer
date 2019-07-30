using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DeskStreamer
{
    static class Serializer
    {
        private static BinaryFormatter formatterSer = new BinaryFormatter();
        private static BinaryFormatter formatterDeSer = new BinaryFormatter();
        public static byte[] ObjectToBytes(object obj)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    formatterSer.Serialize(ms, obj);
                    return ms.ToArray();
                }
            }
            catch(Exception e)
            {
                ConsoleLogic.WriteConsole("Error at serializing", e);
                return null;
            }
        }

        public static object BytesToObj(byte[] bytes, int size)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    ms.Write(bytes, 0, bytes.Length);
                    ms.Seek(0, SeekOrigin.Begin);
                    var obj = formatterDeSer.Deserialize(ms);
                    return obj;
                }
            }
            catch(Exception e)
            {
                ConsoleLogic.WriteConsole("Error at deserealizing", e);
                return null;
            }
            
        }
    }
}
