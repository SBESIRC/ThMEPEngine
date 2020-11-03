using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.Geometry;

namespace ThMEPElectrical.Business.Procedure
{
    /// <summary>
    /// 墙数据提取器
    /// </summary>
    public class WallDataPicker
    {
        // 墙数据
        private List<Curve> m_wallCurves = null;

        private List<Polyline> m_polys;

        public static List<Polyline> MakeWallPickProfiles(List<Curve> srcCurves)
        {
            var wallDataPicker = new WallDataPicker(srcCurves);
            wallDataPicker.Do();
            return wallDataPicker.m_polys;
        }

        public WallDataPicker(List<Curve> wallCurves)
        {
            m_wallCurves = wallCurves;
        }

        public void Do()
        {
            var objs = new DBObjectCollection();
            foreach (var entity in m_wallCurves)
            {
                objs.Add(entity);
            }

            var obLst = objs.Polygons();
            var resPolys = new List<Polyline>();
            for (int i = 0; i < obLst.Count; i++)
            {
                if (obLst[i] is Polyline poly)
                    resPolys.Add(poly);
            }

            m_polys = GeomUtils.CalculateCanBufferPolys(resPolys, ThMEPCommon.WallProfileShrinkDistance);
        }
    }
}
