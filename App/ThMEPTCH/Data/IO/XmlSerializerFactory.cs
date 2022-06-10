using System.IO;
using System.Xml.Serialization;

namespace ThMEPTCH.Data.IO
{
    public class XmlSerializerFactory<T>
    {
        public T Load(string strFileName)
        {
            using (Stream reader = new FileStream(strFileName, FileMode.Open, FileAccess.Read))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                return (T)xmlSerializer.Deserialize(reader);
            }
        }
        public T LoadFromString(string builder)
        {
            using (StringReader reader = new StringReader(builder))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                return (T)xmlSerializer.Deserialize(reader);
            }
        }
        public string Save(T targe)
        {
            using (StringWriter writer = new StringWriter())
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(writer, targe);
                return writer.ToString();
            }
        }

        public void Save(T targe, string strFileName)
        {
            using (Stream writer = new FileStream(strFileName, FileMode.Create, FileAccess.Write))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(writer, targe);
            }
        }
    }
}
