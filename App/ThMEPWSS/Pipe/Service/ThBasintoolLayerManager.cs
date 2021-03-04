﻿using System;
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
                return false;
            }
            return ((patterns[0] == "4")||( patterns[0] == "9")) && (patterns[1] == "KITCHEN") && (patterns[2] == "A");//综合台盆
        }
    }
}
