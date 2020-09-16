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
            string fileName = System.Environment.CurrentDirectory + "Beam.json";
            using (StreamWriter file = File.CreateText(@"c:\movie.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, thIfcBeams);
            }
        }
    }
}
