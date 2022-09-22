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
        //标注点
        public int pt { get; set; } = -1;

        //标注点管到的喷淋点（x的管的方y向）不包含虚拟点，如果是散点，本身也会包含pt本身
        public List<int> PtsDimed { get; set; } = new List<int>();

        public ThSprinklerDimGroup(int ipt,List<int> iPtsDimed)
        {
            pt = ipt;
            PtsDimed = iPtsDimed;
        }
    }
}
