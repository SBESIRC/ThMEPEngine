using System;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;

namespace ThMEPWSS.Pipe.Service
{
    public class ThBasintoolLayerManager
    {
        public static bool IsBasintoolBlockName(string name)
        {
            string[] patterns = ThStructureUtils.OriginalFromXref(name).ToUpper().Split('-').Reverse().ToArray();
            if (patterns.Count() < 3)
            {
                if(patterns.Count()==1)
                {
                    return patterns[0] == "厨盆01";
                }
                return false;
            }
            return ((patterns[0] == "4")||( patterns[0] == "9")) && (patterns[1] == "KITCHEN") && (patterns[2] == "A"|| patterns[2].Substring(patterns[2].Length-1,1)== "A");//综合台盆
        }
    }
}
