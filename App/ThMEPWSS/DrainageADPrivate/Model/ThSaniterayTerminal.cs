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
        internal Polyline Boundary { get; set; }
        internal BlockReference Data { get; set; }
        internal string Name { get; set; }
        internal ThDrainageADCommon.TerminalType Type { get; set; }

    }
}
