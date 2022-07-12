using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.SmokeProofSystem.Service
{
    public static class GeoUtils
    {
        /// <summary>
        /// 获取polyline上所有线
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public static List<Line> GetRoomEdges(this Polyline room)
        {
            var allLines = new List<Line>();
            for (int i = 1; i < room.NumberOfVertices; i++)
            {
                allLines.Add(new Line(room.GetPoint3dAt(i - 1), room.GetPoint3dAt(i)));
            }
            return allLines;
        }

        /// <summary>
        /// 获取平行线
        /// </summary>
        /// <param name="line"></param>
        /// <param name="lines"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static List<Line> GetParallelLines(this Line line, List<Line> lines, Tolerance tol)
        {
            var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
            return lines.Where(x =>
            {
                var dir = (x.EndPoint - x.StartPoint).GetNormal();
                return lineDir.IsParallelTo(dir, tol);
            }).ToList();
        }
    }
}
