using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPLighting.FEI.Service
{
    public static class SelectService
    {
        public static List<Polyline> SelelctCrossing(List<Polyline> polylines, Polyline polyline)
        {
            var objs = polylines.ToCollection();
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var resHoles = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();
            
            return resHoles;
        }

        public static List<Line> SelelctCrossing(List<Line> lines, Polyline polyline)
        {
            var objs = lines.ToCollection();
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var res = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Line>().ToList();

            return res;
        }

        /// <summary>
        /// 用nts的selectCrossing计算是否相交
        /// </summary>
        /// <returns></returns>
        public static bool LineIntersctBySelect(List<Polyline> polylines, Polyline line, double bufferWidth)
        {
            DBObjectCollection dBObject = new DBObjectCollection() { line };
            foreach (Polyline polyline in dBObject.Buffer(bufferWidth))
            {
                if (SelelctCrossing(polylines, polyline).Count > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
