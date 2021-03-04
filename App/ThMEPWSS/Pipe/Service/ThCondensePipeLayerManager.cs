using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;

namespace ThMEPWSS.Pipe.Service
{
    public class ThCondensePipeLayerManager
    {
        public static bool IsCondensePipeBlockName(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').Reverse().ToArray();
            if (patterns.Count() < 3)
            {
                return false;
            }
            return ((patterns[0] == "S") && (patterns[1] == "P") && (patterns[2] == "H")|| (patterns[0] == "3") && (patterns[1] == "PIPE") && (patterns[2] == "W"));
        }
    }
}
