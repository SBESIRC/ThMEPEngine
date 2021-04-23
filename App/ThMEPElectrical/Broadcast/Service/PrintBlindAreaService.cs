using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;

namespace ThMEPElectrical.Broadcast.Service
{
    public class PrintBlindAreaService
    {
        public void PrintBlindArea(List<Point3d> layoutPts, KeyValuePair<Polyline, List<Polyline>> polyInfo, double protectRange, ThMEPOriginTransformer originTransformer)
        {
            var blindAreas = CalProtectBlindArea(layoutPts, polyInfo, protectRange);
            var objs = blindAreas.ToCollection();
            originTransformer.Reset(objs);
            InsertBlindArea(objs.Cast<Polyline>().ToList());
        }

        /// <summary>
        /// 打印盲区
        /// </summary>
        /// <param name="blindArea"></param>
        public void InsertBlindArea(List<Polyline> blindArea)
        {
            using (var db = AcadDatabase.Active())
            {
                var layerId = LayerTools.AddLayer(db.Database, ThMEPCommon.BlindAreaLayer);
                db.Database.UnLockLayer(ThMEPCommon.BlindAreaLayer);
                db.Database.UnFrozenLayer(ThMEPCommon.BlindAreaLayer);
                db.Database.UnPrintLayer(ThMEPCommon.BlindAreaLayer);

                foreach (var area in blindArea.Where(x => x.Area > 1))
                {
                    area.Layer = ThMEPCommon.BlindAreaLayer;
                    area.ColorIndex = 5;
                    area.ConstantWidth = 50;
                    db.ModelSpace.Add(area);
                }
            }
        }

        /// <summary>
        /// 计算保护盲区
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="roomPoly"></param>
        /// <returns></returns>
        public List<Polyline> CalProtectBlindArea(List<Point3d> layoutPts, KeyValuePair<Polyline, List<Polyline>> polyInfo, double protectRange)
        {
            var objs = new DBObjectCollection();
            foreach (var pt in layoutPts)
            {
                var circle = new Circle(pt, Vector3d.ZAxis, protectRange);
                foreach (var poly in circle.ToNTSPolygon(20).ToDbPolylines())
                {
                    objs.Add(poly);
                }
            }

            var blindAreas = polyInfo.Key.Difference(objs);
            var holes = new DBObjectCollection();
            foreach (var hole in polyInfo.Value)
            {
                holes.Add(hole);
            }

            List<Polyline> areas = new List<Polyline>();
            foreach (Polyline bArea in blindAreas)
            {
                areas.AddRange(bArea.Difference(holes).Cast<Polyline>().ToList());
            }

            var pFrame = polyInfo.Key.Buffer(-5)[0] as Polyline;
            List<Polyline> frames = new List<Polyline>() { pFrame };
            frames.AddRange(polyInfo.Value.Select(x => x.Buffer(5)[0] as Polyline));
            List<Polyline> blindAreaLst = new List<Polyline>();
            foreach (Polyline area in areas)
            {
                foreach (var frame in frames)
                {
                    Point3dCollection pts = new Point3dCollection();
                    frame.IntersectWith(area, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    if (pts.Count > 0)
                    {
                        blindAreaLst.Add(area);
                        break;
                    }
                }
            }

            return blindAreaLst;
        }
    }
}
