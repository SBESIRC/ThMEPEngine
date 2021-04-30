using System.Linq;
using ThCADCore.NTS;
using THMEPCore3D.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

namespace THMEPCore3D.Utils
{
    public static class ThUtils
    {
        public static List<ThDb3ElementRawData> Filter(this List<ThDb3ElementRawData> datas, Point3dCollection pts)
        {
            if (pts.Count > 0)
            {
                var dbObjs = new DBObjectCollection();
                datas.ForEach(o => dbObjs.Add(o.Geometry));
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                var filterObjs = spatialIndex.SelectCrossingPolygon(pts);
                return datas.Where(o => filterObjs.Contains(o.Geometry)).ToList();
            }
            else
            {
                return datas;
            }
        }
        public static string OriginalFromXref(string xrefLayer)
        {
            // 已绑定外参
            if (xrefLayer.Matches("*`$#`$*"))
            {
                return xrefLayer.Substring(xrefLayer.LastIndexOf('$') + 1);
            }

            // 未绑定外参
            if (xrefLayer.Matches("*|*"))
            {
                return xrefLayer.Substring(xrefLayer.LastIndexOf('|') + 1);
            }

            // 其他非外参
            return xrefLayer;
        }

    }
}
