using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Assistant
{
    public static class GeometryTrans
    {
        public static List<Curve> Polylines2Curves(this List<Polyline> srcPolylines)
        {
            if (srcPolylines == null || srcPolylines.Count == 0)
                return null;
            var curves = new List<Curve>();

            foreach (var polyline in srcPolylines)
            {
                curves.Add(polyline);
            }

            return curves;
        }
    }
}
