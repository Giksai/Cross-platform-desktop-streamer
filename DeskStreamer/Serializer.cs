using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DeskStreamer
{
    static class Serializer
    {
        public static byte[] ObjectToBytes(object obj)
        {
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (var ms = new MemoryStream())
                {
                    formatter.Serialize(ms, obj);
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
                BinaryFormatter formatter = new BinaryFormatter();
                using (var ms = new MemoryStream())
                {
                    ms.Write(bytes, 0, bytes.Length);
                    ms.Seek(0, SeekOrigin.Begin);
                    var obj = formatter.Deserialize(ms);
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
