using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPElectrical.Broadcast
{
    public class LayoutWithParkingLineService
    {
        readonly double protectRange = 27000;
        readonly double oneProtect = 21000;
        readonly double tol = 5000;

        public void LayoutBraodCast(List<List<Line>> mainLines, List<List<Line>> otherLines, Polyline roomPoly, List<Polyline> columns, List<Polyline> walls)
        {
            foreach (var lines in mainLines)
            {
                var usefulColumns = GetStruct(lines, columns);
                var usefulWalls = GetStruct(lines, walls);
            }
            
        }

        private void GetLayoutLinePoint(List<Line> lines, List<Polyline> columns, List<Polyline> walls)
        {
            List<Point3d> allPts = lines.SelectMany(x => new List<Point3d>() { x.StartPoint, x.EndPoint }).ToList();
            Point3d sPt = allPts.OrderBy(x => x.X).First();
            Point3d ePt = allPts.OrderByDescending(x => x.X).First();

            List<Point3d> layoutPts = new List<Point3d>();
            double lineLength = sPt.DistanceTo(ePt);
            if (lineLength < oneProtect)
            {
                layoutPts.Add(new Point3d((sPt.X + ePt.X) / 2, (sPt.Y + ePt.Y) / 2, 0));
            }
            else
            {
                
            }
        }

        private void GetLayoutStructPt(Point3d pt, List<Polyline> columns, List<Polyline> walls)
        {

        }

        /// <summary>
        /// 获取停车线周边构建信息
        /// </summary>
        /// <param name="polylines"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public List<Polyline> GetStruct(List<Line> lines, List<Polyline> polys)
        {
            List<Polyline> resPolys = new List<Polyline>();
            foreach (var line in lines)
            {
                var linePoly = expandLine(line, tol);
                resPolys.AddRange(polys.Where(x => linePoly.Intersects(x) || linePoly.Contains(x)).ToList());
            }

            return resPolys;
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
            Point3d p1 = line.StartPoint - lineDir * tol + moveDir * distance;
            Point3d p2 = line.EndPoint + lineDir * tol + moveDir * distance;
            Point3d p3 = line.EndPoint + lineDir * tol - moveDir * distance;
            Point3d p4 = line.StartPoint - lineDir * tol - moveDir * distance;

            Polyline polyline = new Polyline() { Closed = true };
            polyline.AddVertexAt(0, p1.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p2.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p3.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p4.ToPoint2D(), 0, 0, 0);
            return polyline;
        }
    }
}
