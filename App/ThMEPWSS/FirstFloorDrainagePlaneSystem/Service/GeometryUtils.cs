using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Service
{
    public static class GeometryUtils
    {
        /// <summary>
        /// 将长边外扩（或内缩）一定距离。tips：仅支持矩形
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="tol"></param>
        public static Polyline ExtendByLengthLine(this Polyline polyline, double tol)
        {
            var allLines = StructGeoService.GetAllLineByPolyline(polyline).OrderBy(x => x.Length).ToList();
            var firLine = allLines[0];
            var lastLine = allLines[1];
            var firExtendLine = ExtendLine(firLine, tol);
            var lastExtendLine = ExtendLine(lastLine, tol);

            Polyline resPoly = new Polyline() { Closed = true };
            resPoly.AddVertexAt(0, firExtendLine.StartPoint.ToPoint2D(), 0, 0, 0);
            resPoly.AddVertexAt(1, firExtendLine.EndPoint.ToPoint2D(), 0, 0, 0);
            resPoly.AddVertexAt(2, lastExtendLine.EndPoint.ToPoint2D(), 0, 0, 0);
            resPoly.AddVertexAt(3, lastExtendLine.StartPoint.ToPoint2D(), 0, 0, 0);
            return resPoly;
        }

        /// <summary>
        /// 两边延申线
        /// </summary>
        /// <param name="line"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static Line ExtendLine(Line line, double tol)
        {
            var dir = (line.EndPoint - line.StartPoint).GetNormal();
            var sPt = line.StartPoint - dir * tol;
            var ePt = line.EndPoint + dir * tol;
            return new Line(sPt, ePt);
        }

        /// <summary>
        /// 从集合中找到所有连接线
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="line"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static List<Line> GetConenctLine(List<Line> lines, Line line, double tol = 1)
        {
            var resLines = new List<Line>();
            var allLines = new List<Line>(lines);
            var connectLines = lines.Where(x => x.StartPoint.DistanceTo(line.StartPoint) < tol ||
                x.StartPoint.DistanceTo(line.EndPoint) < tol ||
                x.EndPoint.DistanceTo(line.StartPoint) < tol ||
                x.EndPoint.DistanceTo(line.EndPoint) < tol).ToList();
            resLines.AddRange(connectLines);
            allLines = allLines.Except(connectLines).ToList();
            foreach (var rLine in resLines)
            {
                resLines.AddRange(GetConenctLine(allLines, rLine, tol));
            }
            return resLines;
        }
    }
}
