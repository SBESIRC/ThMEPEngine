using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.Model;
using ThMEPWSS.Service;

namespace ThMEPWSS.Bussiness.BoundaryProtectBussiness
{
    public class BoundaryProtestService
    {
        /// <summary>
        /// 获取边界喷淋
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="sprays"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public Dictionary<Line, List<SprayLayoutData>> GetBoundarySpray(Polyline polyline, List<SprayLayoutData> sprays, double length)
        {
            List<Line> lines = new List<Line>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                lines.Add(new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices)));
            }
            
            Dictionary<Line, List<SprayLayoutData>> sprayDic = new Dictionary<Line, List<SprayLayoutData>>();
            foreach (var line in lines)
            {
                if (line.Length <= 300)
                {
                    continue;
                }

                var linePoly = expandLine(line, length);
                var resSprays = GetSprays(polyline, line, linePoly, sprays);
                if (resSprays.Count > 0)
                {
                    sprayDic.Add(line, resSprays);
                }
            }

            //using (AcadDatabase ad = AcadDatabase.Active())
            //{
            //    var s = sprayDic.SelectMany(x => x.Value).ToList();
            //    foreach (var item in s)
            //    {
            //        ad.ModelSpace.Add(new Line(item.Position, item.Position + 100 * Vector3d.YAxis));
            //    }
            //}

            return sprayDic;
        }

        /// <summary>
        /// 获取附近的喷淋点
        /// </summary>
        /// <param name="polylines"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public List<SprayLayoutData> GetSprays(Polyline polyline, Line line, Polyline linePoly, List<SprayLayoutData> sprays)
        {
            var resPrays = sprays.Where(x => linePoly.IndexedContains(x.Position)).ToList();

            List<SprayLayoutData> closetPolys = new List<SprayLayoutData>();
            var nerstSpray = resPrays.OrderBy(x =>
            {
                var closetPt = line.GetClosestPointTo(x.Position, false);
                return closetPt.DistanceTo(x.Position);
            }).ToList();
            foreach (var nSpray in nerstSpray)
            {
                var closetPt = line.GetClosestPointTo(nSpray.Position, false);
                var moveDir = (nSpray.Position - closetPt).GetNormal();
                var movePt = closetPt + moveDir * 1;
                if (!polyline.Contains(movePt))
                {
                    continue;
                }

                var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                var sprayDir = nSpray.mainDir;
                if (Math.Abs(lineDir.DotProduct(nSpray.mainDir)) < Math.Abs(lineDir.DotProduct(nSpray.otherDir)))
                {
                    sprayDir = nSpray.otherDir;
                }

                var sprayLine = nSpray.GetPolylineByDir(sprayDir);
                closetPolys.AddRange(resPrays.Where(x => x.vLine == sprayLine || x.tLine == sprayLine));
                break;
            }
            return closetPolys;
        }

        /// <summary>
        /// 扩张line成polyline
        /// </summary>
        /// <param name="line"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public Polyline expandLine(Line line, double distance)
        {
            Vector3d lineDir = line.Delta.GetNormal();
            Vector3d moveDir = Vector3d.ZAxis.CrossProduct(lineDir);
            Point3d p1 = line.StartPoint + moveDir * distance;
            Point3d p2 = line.EndPoint + moveDir * distance;
            Point3d p3 = line.EndPoint - moveDir * distance;
            Point3d p4 = line.StartPoint - moveDir * distance;

            Polyline polyline = new Polyline() { Closed = true };
            polyline.AddVertexAt(0, p1.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p2.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p3.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p4.ToPoint2D(), 0, 0, 0);
            return polyline;
        }
    }
}
