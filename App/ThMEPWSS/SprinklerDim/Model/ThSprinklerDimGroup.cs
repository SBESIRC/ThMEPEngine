using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Diagnostics;
using ThMEPWSS.SprinklerDim.Model;
using ThMEPWSS.SprinklerDim.Service;

namespace ThMEPWSS.SprinklerDim.Model
{
    public class ThSprinklerDimGroup
    {
        public int pt { get; set; } = -1;

        public List<int> PtsDimed { get; set; } = new List<int>();

        public ThSprinklerDimGroup(int ipt,List<int> iPtsDimed)
        {
            pt = ipt;
            PtsDimed = iPtsDimed;
        }
    }
}
