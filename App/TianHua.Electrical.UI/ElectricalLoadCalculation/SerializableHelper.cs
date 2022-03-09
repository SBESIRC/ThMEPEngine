using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace TianHua.Electrical.UI.ElectricalLoadCalculation
{
    public class SerializableHelper
    {
        public bool Serializable(object data, string filepath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filepath);
                if (!Directory.Exists(fileInfo.DirectoryName))
                {
                    Directory.CreateDirectory(fileInfo.DirectoryName);
                }
                FileStream fs = new FileStream(filepath, FileMode.Create);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, data);
                fs.Close();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public byte[] Serializable(object data)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter binFormat = new BinaryFormatter();
            binFormat.Serialize(stream, data);
            stream.Position = 0;
            return stream.GetBuffer();
        }

        public string SerializableToBase64(object obj)
        {
            byte[] data = Serializable(obj);
            if (data != null && data.Length > 0)
            {
                return Convert.ToBase64String(data);
            }
            else
            {
                return null;
            }
        }
        public object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            stream.Write(data, 0, data.Length);
            stream.Position = 0;
            object obj = bf.Deserialize(stream);
            return obj;
        }
        public object Deserialize(string filepath)
        {
            FileStream fs = new FileStream(filepath, FileMode.Open);
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Binder = new UBinder();
                object data = bf.Deserialize(fs);
                fs.Close();
                return data;
            }
            catch (Exception ex)
            {
                fs.Close();
                return null;
            }
        }
        public object DeserializeFormBase64(string str)
        {
            try
            {
                byte[] data = Convert.FromBase64String(str);
                return Deserialize(data);
            }
            catch
            {
                return null;
            }
        }
    }

    public class UBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            try
            {
                Type type = Type.GetType(typeName);
                if (type.IsNull())
                {
                    throw new Exception("无法找到Type");
                }
                return type;
            }
            catch
            {
                return Assembly.Load(assemblyName).GetType(typeName);
            }
        }

    }
}
