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

        /// <summary>
        /// 用nts的selectCrossing计算是否相交
        /// </summary>
        /// <returns></returns>
        public static bool LineIntersctBySelect(List<Polyline> holes, Polyline line, double bufferWidth)
        {
            foreach (Polyline polyline in line.BufferPL(bufferWidth))
            {
                if (SelelctCrossing(holes, polyline).Count > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
