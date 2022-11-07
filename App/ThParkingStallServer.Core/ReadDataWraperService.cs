using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.OInterProcess;

namespace ThParkingStallServer.Core
{
    public class ReadDataWraperService
    {
        public ReadDataWraperService(string path)
        {
            Path = path;
        }
        private string Path { get; set; }
        public DataWraper Read()
        {
            var fileStream = new FileStream(Path, FileMode.Open);
            var formatter = new BinaryFormatter
            {
                Binder = new UBinder()
            };
            var readWraper = (DataWraper)formatter.Deserialize(fileStream);
            fileStream.Close();
            return readWraper;
        }
    }
    public class UBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            Type typeToDeserialize = null;
            typeToDeserialize = Type.GetType(String.Format("{0}, {1}",
                typeName, assemblyName));
            return typeToDeserialize;
        }
    }
}
