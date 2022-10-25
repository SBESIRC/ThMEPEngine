using System.IO;
using ThMEPEngineCore.IO.JSON;
using System.Collections.Generic;

namespace ThMEPTCH.Services
{
    public class ThIfcStoreyParseTool
    {
        public static Dictionary<string, List<ThEditStoreyInfo>> DeSerialize(string fileName)
        {
            var results = new Dictionary<string, List<ThEditStoreyInfo>>();
            try
            {
                var jsonString = File.ReadAllText(fileName);
                results = JsonHelper.DeserializeJsonToObject<Dictionary<string, List<ThEditStoreyInfo>>>(jsonString);
            }
            catch
            {
                //
            }
            return results;
        }
    }
}
