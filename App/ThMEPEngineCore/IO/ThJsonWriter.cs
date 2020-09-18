using System;
using System.IO;
using Newtonsoft.Json;
using ThMEPEngineCore.Model;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO
{
    public class ThJsonWriter
    {
        public static void OutputBeams(List<ThIfcBeam> thIfcBeams)
        {
            var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Beam.result.json");
            using (StreamWriter file = File.CreateText(fileName))
            {
                JsonTextWriter jsonWriter = new JsonTextWriter(file)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 4,
                    IndentChar = ' '
                };
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(jsonWriter, thIfcBeams);
            }
        }
    }
}
