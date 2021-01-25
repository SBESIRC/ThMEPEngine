using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPElectrical.Broadcast.Service
{
    public class CheckService
    {
        public List<Polyline> FilterColumns(List<Polyline> columns, Line line, Polyline frame, Point3d sPt, Point3d ePt)
        {
            Vector3d xDir = (line.EndPoint - line.StartPoint).GetNormal();
            Vector3d yDir = Vector3d.ZAxis.CrossProduct(xDir);
            Vector3d zDir = Vector3d.ZAxis;
            Matrix3d matrix = new Matrix3d(
                new double[] {
                    xDir.X, yDir.X, zDir.X, line.StartPoint.X,
                    xDir.Y, yDir.Y, zDir.Y, line.StartPoint.Y,
                    xDir.Z, yDir.Z, zDir.Z, line.StartPoint.Z,
                    0.0, 0.0, 0.0, 1.0
                });

            if (columns.Count > 0)
            {
                var orderColumns = columns.OrderBy(x => StructUtils.GetStructCenter(x).TransformBy(matrix).X).ToList();
                if (!IsUsefulColumn(frame, orderColumns.First(), sPt, xDir))
                {
                    columns.Remove(orderColumns.First());
                }
                if (!IsUsefulColumn(frame, orderColumns.Last(), ePt, xDir))
                {
                    columns.Remove(orderColumns.Last());
                }
            }
            
            return columns;
        }

        private bool IsUsefulColumn(Polyline frame, Polyline polyline, Point3d pt, Vector3d dir)
        {
            var newPoly = polyline.Buffer(200)[0] as Polyline;
            Line layoutLine = IsLayoutColumn(newPoly, pt, dir);
            Point3dCollection pts = new Point3dCollection();
            layoutLine.IntersectWith(frame, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
            if (pts.Count > 0)            {
                return false;
            }

            return true;
        }

        private Line IsLayoutColumn(Polyline polyline, Point3d pt, Vector3d dir)
        {
            var closetPt = polyline.GetClosestPointTo(pt, false);
            List<Line> lines = new List<Line>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                lines.Add(new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices)));
            }

            Vector3d otherDir = Vector3d.ZAxis.CrossProduct(dir);
            var layoutLine = lines.Where(x => x.ToCurve3d().IsOn(closetPt, new Tolerance(1, 1)))
                .Where(x =>
                {
                    var xDir = (x.EndPoint - x.StartPoint).GetNormal();
                    return Math.Abs(otherDir.DotProduct(xDir)) < Math.Abs(dir.DotProduct(xDir));
                }).FirstOrDefault();

            return layoutLine;
        }

        public List<Polyline> FilterWalls(List<Polyline> walls, List<Line> lines, double expandLength, double tol)
        {
            var wallCollections = walls.ToCollection();
            var resPolys = lines.SelectMany(x =>
            {
                var linePoly = StructUtils.ExpandLine(x, expandLength, tol);
                return linePoly.Intersection(wallCollections).Cast<Polyline>().ToList();
            }).ToList();

            return resPolys;
        }
    }
}
