using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;

namespace ThMEPHVAC.FloorHeatingCoil
{
    public static class CenterLineUtils
    {
        public static List<Polyline> GetCenterLine(Polyline poly)
        {
            return ThMEPPolygonService.CenterLine(poly.ToNTSPolygon().ToDbMPolygon())
                                      .ToCollection()
                                      .LineMerge()
                                      .OfType<Polyline>().ToList();
        }
    }
}
