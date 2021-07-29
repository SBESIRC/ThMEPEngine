﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDADDataExchange
    {
        public List<ThTerminalToilet> toiletList { get; set; }
        public List<Line> pipes { get; set; }
        public List<ThDrainageSDADValve> valveList { get; set; }
        public List<Circle> stackList { get; set; }
        public Point3d startPt { get; set; }

    }
}
