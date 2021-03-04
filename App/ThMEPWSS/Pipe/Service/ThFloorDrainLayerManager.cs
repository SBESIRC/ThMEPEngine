using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;

namespace ThMEPWSS.Pipe.Service
{
    public class ThFloorDrainLayerManager
    {
        public static bool IsToiletFloorDrainBlockName(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').Reverse().ToArray();
            if (patterns.Count() < 3)
            {
                return ((patterns[0] == "卫") && (patterns[1] == "地漏"));             
            }
            return (patterns[0] == "4") && (patterns[1] == "DRAIN") && (patterns[2] == "W");
        }
        public static bool IsBalconyFloorDrainBlockName(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').Reverse().ToArray();
            if (patterns.Count() < 3)
            {
                return ((patterns[0] == "卫") && (patterns[1] == "地漏"));
            }
            return (patterns[0] == "3") && (patterns[1] == "DRAIN") && (patterns[2] == "W");
        }
    }
}
