using System.Linq;
using ThCADCore.NTS;
using ThMEPElectrical.Geometry;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.Business.Procedure
{
    public class SplitWallWorker
    {
        // 墙数据
        private List<Curve> m_wallCurves = null;

        private List<Curve> m_unClosedCurves = new List<Curve>();// 非闭合数据

        private List<Polyline> m_polys = new List<Polyline>();

        public static List<Polyline> MakeSplitWallProfiles(List<Curve> srcCurves)
        {
            if (srcCurves == null)
                return new List<Polyline>();

            var wallDataPicker = new SplitWallWorker(srcCurves);
            wallDataPicker.Do();
            return wallDataPicker.m_polys;
        }

        public SplitWallWorker(List<Curve> wallCurves)
        {
            m_wallCurves = wallCurves;
        }

        private List<Polyline> CalculateClosedPolys()
        {
            var polys = new List<Polyline>();
            foreach (var curve in m_wallCurves)
            {
                if (curve is Polyline poly && poly.Closed)
                    polys.Add(poly);
                else
                    m_unClosedCurves.Add(curve);
            }

            return polys;
        }

        public void Do()
        {
            // 闭合数据
            var polys = CalculateClosedPolys();
            if (polys.Count == 0)
                return;

            // 清洗外框线（MakeValid)
            var frames = polys.Select(o => ThMEPFrameService.Normalize(o));

            // 非闭合数据处理
            foreach (var poly in frames)
            {
                var polygonCurves = new List<Curve>();
                polygonCurves.Add(poly);
                polygonCurves.AddRange(m_unClosedCurves);
                var objs = GeomUtils.Curves2DBCollection(polygonCurves);
                var obLst = objs.Polygons();
                var resPolys = new List<Polyline>();
                for (int i = 0; i < obLst.Count; i++)
                {
                    if (obLst[i] is Polyline resPoly)
                        resPolys.Add(resPoly);
                }

                var predicateCurves = GeomUtils.CalculateCanBufferPolys(resPolys, ThMEPCommon.WallProfileShrinkDistance);
                m_polys.AddRange(predicateCurves);
            }
        }
    }
}
