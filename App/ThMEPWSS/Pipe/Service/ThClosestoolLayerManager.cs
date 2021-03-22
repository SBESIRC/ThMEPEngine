using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;

namespace ThMEPWSS.Pipe.Service
{
    public class ThClosestoolLayerManager
    {
        public static bool IsClosetoolBlockName(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').Reverse().ToArray();
            if (patterns.Count() < 3)
            {
                if(patterns.Count()==2)
                {
                    return (patterns[0] == "平面") && (patterns[1] == "马桶03");
                }
                return false;
            }
            return (patterns[0] == "5") && (patterns[1] == "TOILET") && (patterns[2] == "A");
        }
    }
}
