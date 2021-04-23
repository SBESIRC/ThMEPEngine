using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;

namespace ThMEPWSS.Pipe.Service
{
    public class ThRoofRainPipeLayerManager
    {
        public static bool IsRoofRainPipeBlockName(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').Reverse().ToArray();
            if (patterns.Count() < 3)
            {
                return false;
            }
            return (patterns[0] == "1") && (patterns[1] == "PIPE") && (patterns[2] == "W");
        }
    }
}