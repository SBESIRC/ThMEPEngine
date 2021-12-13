using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPEngineCore.UCSDivisionService.Utils
{
    public static class GeoUtils
    {
        /// <summary>
        /// 获得德劳内三角的外包框
        /// </summary>
        /// <param name="triangles"></param>
        /// <returns></returns>
        public static Polyline GetLinesOutBox(List<Polyline> triangles)
        {
            var allLines = triangles.SelectMany(x => x.GetLinesByPolyline(500)).ToList();
            var outLines = allLines.Where(x => triangles.Where(y => y.Distance(x.StartPoint) < 1 && y.Distance(x.EndPoint) < 1).Count() < 2).ToList();
            var firLine = outLines.First();
            var sP = firLine.StartPoint;
            var eP = firLine.EndPoint;
            Polyline outBox = new Polyline() { Closed = true };
            int index = 0;
            while (outLines.Count > 0 && !firLine.EndPoint.IsEqualTo(sP, new Tolerance(1, 1)))
            {
                outLines = outLines.Where(x => !x.IsEqualLine(firLine)).ToList();
                firLine = outLines.Where(x => x.StartPoint.IsEqualTo(eP, new Tolerance(1, 1)) || x.EndPoint.IsEqualTo(eP, new Tolerance(1, 1))).FirstOrDefault();
                if (firLine == null) return outBox;

                var firEndPt = firLine.StartPoint.IsEqualTo(eP, new Tolerance(1, 1)) ? firLine.EndPoint : firLine.StartPoint;
                firLine = new Line(eP, firEndPt);
                outBox.AddVertexAt(index, eP.ToPoint2D(), 0, 0, 0);
                index++;
                eP = firEndPt;
            }
            outBox.AddVertexAt(index, sP.ToPoint2D(), 0, 0, 0);

            return outBox;
        }

        /// <summary>
        /// 判断线是否相等
        /// </summary>
        /// <param name="line"></param>
        /// <param name="otherLine"></param>
        /// <returns></returns>
        public static bool IsEqualLine(this Line line, Line otherLine)
        {
            return (line.StartPoint.IsEqualTo(otherLine.StartPoint) && line.EndPoint.IsEqualTo(otherLine.EndPoint)) ||
                (line.StartPoint.IsEqualTo(otherLine.EndPoint) && line.EndPoint.IsEqualTo(otherLine.StartPoint));
        }

        /// <summary>
        /// 将polyline转化成line的集合
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="arcChord"></param>
        /// <returns></returns>
        public static List<Line> GetLinesByPolyline(this Polyline polyline, double arcChord)
        {
            var dbCollec = new DBObjectCollection();
            polyline.Explode(dbCollec);
            return ConvertToLine(dbCollec.Cast<Curve>().ToList(), arcChord);
        }

        /// <summary>
        /// 将curve转成line
        /// </summary>
        /// <param name="girds"></param>
        /// <param name="arcChord"></param>
        /// <returns></returns>
        public static List<Line> ConvertToLine(List<Curve> girds, double arcChord)
        {
            List<Line> resLines = new List<Line>();
            foreach (var grid in girds)
            {
                if (grid is Line line)
                {
                    if (line.Length > 1)
                    {
                        resLines.Add(line);
                    }
                }
                else if (grid is Polyline)
                {
                    var objs = new DBObjectCollection();
                    grid.Explode(objs);
                    resLines.AddRange(objs.Cast<Line>());
                }
                else if (grid is Arc arc)
                {
                    var polyline = arc.TessellateArcWithChord(arcChord);
                    var entitySet = new DBObjectCollection();
                    polyline.Explode(entitySet);
                    foreach (var obj in entitySet)
                    {
                        resLines.Add(obj as Line);
                    }
                }
            }
            return resLines;
        }

        /// <summary>
        /// 延长线的两个端点
        /// </summary>
        /// <param name="line"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Line ExtendLine(this Line line, double length)
        {
            var dir = (line.EndPoint - line.StartPoint).GetNormal();
            var pt1 = line.StartPoint - dir * length;
            var pt2 = line.EndPoint + dir * length;
            return new Line(pt1, pt2);
        }
    }
}
