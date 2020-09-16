using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.IO
{
    public class ThJsonWriter
    {
        public static void OutputBeams(List<ThIfcBeam> thIfcBeams)
        {
            // serialize JSON directly to a file
            string time = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
            string fileName = System.Environment.CurrentDirectory + "\\Beam"+ time+".json";
            using (StreamWriter file = File.CreateText(fileName))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, thIfcBeams);
            }
        }
    }
}
