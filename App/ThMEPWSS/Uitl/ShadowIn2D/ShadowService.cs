using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPWSS.Uitl.ShadowIn2D
{
    public class ShadowService
    {
        public List<Polyline> CreateShadow(Point3d basePt, Polyline protectRange, List<Polyline> obstacle)
        {
            var verticePolys = new List<Polyline>(obstacle);
            verticePolys.Add(protectRange);

            var vertices = AllVertices(verticePolys);
            var usefulLines = GetLine(basePt, vertices, verticePolys);
            
            var allTriangle = CalLightTriangle(usefulLines, verticePolys);
            DBObjectCollection dbs = new DBObjectCollection();
            foreach (var triangle in allTriangle)
            {
                using (AcadDatabase db = AcadDatabase.Active())
                {
                    //db.ModelSpace.Add(triangle);
                }
                dbs.Add(triangle);
            }

            return dbs.UnionPolygons().Cast<Polyline>().ToList();
        }

        private List<Polyline> CalLightTriangle(List<Line> usefulLines, List<Polyline> polylines)
        {
            List<Polyline> triangle = new List<Polyline>();
            var firLine = usefulLines.First();
            while (usefulLines.Count > 0)
            {
                double minAngle = double.MaxValue;
                Line matchLine = null;
                foreach(var line in usefulLines.Where(x => x != firLine))
                {
                    var firDir = (firLine.EndPoint - firLine.StartPoint).GetNormal();
                    var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                    var angle = firDir.GetAngleTo(lineDir, Vector3d.ZAxis);
                    if (angle < minAngle)
                    {
                        minAngle = angle;
                        matchLine = line;
                    }
                }
                if (matchLine != null)
                {
                    triangle.Add(CalTriangle(firLine, matchLine, polylines));
                }
                
                firLine = matchLine;
                usefulLines.Remove(matchLine);
            }

            return triangle.Where(x => x != null).ToList();
        }

        private Polyline CalTriangle(Line firLine, Line secondLine, List<Polyline> polylines)
        {
            Point3d midPt = new Point3d((firLine.EndPoint.X + secondLine.EndPoint.X) / 2, (firLine.EndPoint.Y + secondLine.EndPoint.Y) / 2, 0);
            Ray ray = new Ray();
            ray.BasePoint = firLine.StartPoint;
            ray.SecondPoint = midPt;

            double dis = double.MaxValue;
            Polyline polyline = null;
            Point3d intersectPt = Point3d.Origin;
            foreach (var pline in polylines)
            {
                Point3d point = Point3d.Origin;
                if (CalNearestIntersectPtDistance(ray, pline, ref point))
                {
                    var distance = ray.BasePoint.DistanceTo(point);
                    if (distance < dis)
                    {
                        dis = distance;
                        polyline = pline;
                        intersectPt = point;
                    }
                }
            }

            if (polyline != null)
            {
                var intersectLine = GetIntersectLine(polyline, intersectPt);

                Ray firRay = new Ray();
                firRay.BasePoint = firLine.StartPoint;
                firRay.SecondPoint = firLine.EndPoint;
                Point3d firPt = firLine.EndPoint;
                CalNearestIntersectPtDistance(firRay, intersectLine, ref firPt);

                Ray secRay = new Ray();
                secRay.BasePoint = firLine.StartPoint;
                secRay.SecondPoint = secondLine.EndPoint;
                Point3d secPt = secondLine.EndPoint;
                CalNearestIntersectPtDistance(secRay, intersectLine, ref secPt);

                return CreateTriangle(firPt, secPt, ray.BasePoint);
            }
            return null;
        }

        private Line GetIntersectLine(Polyline polyline, Point3d point)
        {
            var allLines = AllSegments(polyline);
            foreach (var line in allLines)
            {
                if (line.ToCurve3d().IsOn(point, new Tolerance(1, 1)))
                {
                    return line;
                }
            }

            return null;
        }

        private Polyline CreateTriangle(Point3d pt1, Point3d pt2,Point3d pt3)
        {
            Polyline polyline = new Polyline() { Closed = true };
            polyline.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, pt2.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, pt3.ToPoint2D(), 0, 0, 0);
            return polyline;
        }

        private bool CalNearestIntersectPtDistance(Ray ray, Curve curve, ref Point3d point)
        {
            var interPts = ray.Intersect(curve, Intersect.OnBothOperands);
            if (interPts.Count > 0)
            {
                point = interPts.OrderBy(x => x.DistanceTo(ray.BasePoint)).First();
                return true;
            }

            return false;
        }

        private List<Line> GetLine(Point3d basePt, List<Point3d> vertices, List<Polyline> obstacle)
        {
            List<Line> rayLine = new List<Line>();
            foreach (var pt in vertices)
            {
                Line line = new Line(basePt, pt);
                var interPts = obstacle.SelectMany(x => line.Intersect(x, Intersect.OnBothOperands)).ToList();
                if (interPts.Count > 1 || (interPts.Count > 0 && !interPts.First().IsEqualTo(pt)))
                {
                    continue;
                }

                if (rayLine.Where(x => x.Overlaps(line)).Count() <= 0)
                {
                    rayLine.Add(line);
                }
            }

            var si = new ThCADCoreNTSSpatialIndex(rayLine.ToCollection());
            return si.Geometries.Values.Cast<Line>().ToList();
        } 

        private List<Point3d> AllVertices(List<Polyline> polylines)
        {
            var vertices = new List<Point3d>();
            foreach (var pline in polylines)
            {
                vertices.AddRange(pline.Vertices().Cast<Point3d>());
            }

            return vertices;
        }

        private List<Line> AllSegments(Polyline polyline)
        {
            var lines = new List<Line>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                lines.Add(new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices)));
            }

            return lines;
        }
    }
}
