using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Data;
using ThMEPWSS.SprinklerConnect.Engine;
using ThMEPWSS.SprinklerConnect.Model;
using ThMEPEngineCore.Service;

namespace ThMEPWSS.SprinklerPiping.Service
{
    public static class SprinklerPipingAssist
    {
        
        public static Polyline MakePolyline(List<Point3d> pts)
        {
            Polyline poly = new Polyline(pts.Count);
            int i = 0;
            for(; i<pts.Count; i++)
            {
                poly.AddVertexAt(i, pts[i].ToPoint2d(), 0, 0, 0);
            }
            poly.Closed = true;
            return poly;
        }

        public static Line ExtendLineSingleDir(this Line line, double distance)
        {
            var direction = line.LineDirection();
            return new Line(line.StartPoint, line.EndPoint + direction * distance);
        }

        public static List<List<Point3d>> GetIntersectPts(Polyline parkingRow, Polyline frame)
        {
            List<List<Point3d>> intersectPts = new List<List<Point3d>>();
            for(int i=0; i<4; i++)
            {
                int nexti = i < 3 ? i + 1 : 0;
                intersectPts.Add(new Line(parkingRow.GetPoint3dAt(i), parkingRow.GetPoint3dAt(nexti)).Intersect(frame, 0));
            }
            return intersectPts;
        }

        public static Polyline NormalizeInterPoly(Polyline poly)
        {
            //var obj = new DBObjectCollection();
            //obj.Add(poly);
            //var simplifier = new ThPolygonalElementSimplifier();
            //return simplifier.Normalize(obj).Cast<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
            //删除重复点
            List<Point3d> pts = new List<Point3d>();
            for (int i=0; i<poly.NumberOfVertices; i++)
            {
                int nexti = i < poly.NumberOfVertices - 1 ? i + 1 : 0;
                int lasti = i > 0 ? i - 1 : poly.NumberOfVertices - 1;
                if (!poly.GetPoint3dAt(i).IsEqualTo(poly.GetPoint3dAt(nexti), new Tolerance(1, 1)))
                {
                    pts.Add(poly.GetPoint3dAt(i));
                }
            }

            //五边形交线
            List<Point3d> retpts = new List<Point3d>();
            for (int i = 0; i < pts.Count; i++)
            {
                int nexti = i < pts.Count - 1 ? i + 1 : 0;
                int lasti = i > 0 ? i - 1 : pts.Count - 1;
                double a = new Line(pts[lasti], pts[nexti]).GetClosestPointTo(pts[i], false).DistanceTo(pts[i]);
                if (new Line(pts[lasti], pts[nexti]).DistanceTo(pts[i], false) >= 50)
                {
                    retpts.Add(pts[i]);
                }
            }

            //多边形交线（比如一个角被切掉了）
            pts = new List<Point3d>();
            for(int i=0; i<retpts.Count; i++)
            {
                int nexti = i < retpts.Count - 1 ? i + 1 : 0;
                int lasti = i > 0 ? i - 1 : retpts.Count - 1;
                int nexti2 = nexti < retpts.Count - 1 ? nexti + 1 : 0;
                if (Math.Abs((retpts[i] - retpts[lasti]).DotProduct(retpts[nexti] - retpts[i])) > 0.001 && (Math.Abs((retpts[i] - retpts[lasti]).DotProduct(retpts[nexti2] - retpts[nexti])) < 0.001))
                {

                    pts.Add(new Line(retpts[lasti], retpts[i]).GetClosestPointTo(retpts[nexti], true));
                    i++;
                    if(i == retpts.Count)
                    {
                        pts.RemoveAt(0);
                    }
                    continue;
                }
                pts.Add(retpts[i]);
            }

            return MakePolyline(pts);
        }

        public static Point3d GetCenterPt(Point3d pt1, Point3d pt2)
        {
            return pt1 + (pt2 - pt1) / 2;
        }
    }
}
