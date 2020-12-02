using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.Bussiness
{
    public class LayoutSprayByLineService
    {
        public List<Point3d> LayoutSprayByLine(List<Line> lines, double distance)
        {
            Dictionary<Line, bool> usefulLines = lines.ToDictionary(x => x, y => true);
            var line = usefulLines.First().Key;
            var layoutPt = CalSprayPoint(line, usefulLines, line.StartPoint, 0, distance);

            return layoutPt;
        }

        /// <summary>
        /// 计算布置点
        /// </summary>
        /// <param name="line"></param>
        /// <param name="usefulLines"></param>
        /// <param name="sPt"></param>
        /// <param name="useLength"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        private List<Point3d> CalSprayPoint(Line line, Dictionary<Line, bool> usefulLines, Point3d sPt, double useLength, double distance)
        {
            usefulLines[line] = false;
            var startPt = line.StartPoint;
            var endPt = line.EndPoint;
            if (line.EndPoint.IsEqualTo(sPt))
            {
                startPt = line.EndPoint;
                endPt = line.StartPoint;
            }
            
            var dir = (endPt - startPt).GetNormal();
            double length = startPt.DistanceTo(endPt) + useLength;
            int num = Convert.ToInt32(Math.Floor(length / distance));
            startPt = startPt - dir * useLength;
            List<Point3d> pts = new List<Point3d>();
            for (int i = 0; i < num; i++)
            {
                startPt = startPt + distance * dir;
                pts.Add(startPt);
            }

            useLength = length - num * distance;
            var lines = usefulLines.Where(x =>
            {
               return x.Value == true && (x.Key.StartPoint.IsEqualTo(endPt) || x.Key.EndPoint.IsEqualTo(endPt));
            }).Select(x => x.Key).ToList();
            foreach (var lineInfo in lines)
            {
                if (usefulLines[lineInfo])
                {
                    pts.AddRange(CalSprayPoint(lineInfo, usefulLines, endPt, useLength, distance));
                }
            }

            return pts;
        }
    }
}
