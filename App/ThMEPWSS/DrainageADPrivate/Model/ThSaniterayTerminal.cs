using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPWSS.DrainageADPrivate;
using ThMEPWSS.DrainageADPrivate.Service;

namespace ThMEPWSS.DrainageADPrivate.Model
{
    internal class ThSaniterayTerminal
    {
        public Polyline Boundary { get; set; }
        public BlockReference Data { get; set; }
        public string Name { get; set; }
        public ThDrainageADCommon.TerminalType Type { get; set; }

    }
}
