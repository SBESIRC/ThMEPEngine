using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using ThMEPWSS.UndergroundWaterSystem.Command;

namespace ThMEPWSS.UndergroundWaterSystem.Service
{
    public class ThPipeHandleService
    {
        /// <summary>
        /// 整理线
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="mt"></param>
        /// <returns></returns>
        public List<Line> CleanLines(List<Line> lines, Matrix3d mt)
        {
            ///将数据移动到原点附近
            foreach (var l in lines)
            {
                l.TransformBy(mt);
            }
            var retLines = new List<Line>();
            // 处理pipes 1.清除重复线段 ；2.将线在交点处打断
            ThLaneLineCleanService cleanServiec = new ThLaneLineCleanService();
            var allLineColles = cleanServiec.CleanNoding(lines.ToCollection());
            foreach (var l in allLineColles)
            {
                var line = l as Line;
                line.TransformBy(mt.Inverse());
                retLines.Add(line);
            }
            foreach (var l in lines) l.Dispose();
            return retLines;
        }
        /// <summary>
        /// startPt为起点的相连线
        /// </summary>
        /// <param name="startPt"></param>
        /// <returns></returns>
        public List<Line> FindSeriesLine(Point3d startPt, List<Line> allLines)
        {
            //查找到与起点相连的线
            var startLine = ThUndergroundWaterSystemUtils.FindStartLine(startPt, allLines);
            if (startLine == null)
            {
                return null;
            }
            //查找到与startLine相连的一系列线
            var retLines = FindSeriesLine(startLine, ref allLines);
            return retLines;
        }
        private List<Line> FindSeriesLine(Line objectLine, ref List<Line> allLines)
        {
            var retLines = new List<Line>();
            var conLines = FindConnectLine(objectLine, ref allLines);
            retLines.AddRange(conLines);
            foreach (var line in conLines)
            {
                var tlines = FindSeriesLine(line, ref allLines);
                retLines.AddRange(tlines);
            }
            return retLines;
        }
        private List<Line> FindConnectLine(Line objectLine, ref List<Line> lines)
        {
            var retLines = new List<Line>();
            retLines.AddRange(FindDirectLine(objectLine, ref lines));
            retLines.AddRange(FindNearLine(objectLine, ref lines));
            return retLines;
        }
        private bool IsDirectLine(Line objectLine, Line targetLine)
        {
            Point3d objectPt1 = objectLine.StartPoint;
            Point3d objectPt2 = objectLine.EndPoint;
            double distance1 = targetLine.GetClosestPointTo(objectPt1, false).DistanceTo(objectPt1);
            double distance2 = targetLine.GetClosestPointTo(objectPt2, false).DistanceTo(objectPt2);
            if (distance1 < 10.0 || distance2 < 10.0)
            {
                return true;
            }
            return false;
        }
        private bool IsNearLine(Line objectLine, Line targetLine)
        {
            Point3d targetPt1 = targetLine.StartPoint;
            Point3d targetPt2 = targetLine.EndPoint;
            double distance1 = objectLine.GetClosestPointTo(targetPt1, false).DistanceTo(targetPt1);
            double distance2 = objectLine.GetClosestPointTo(targetPt2, false).DistanceTo(targetPt2);
            if (distance1 < 10.0 || distance2 < 10.0)
            {
                return true;
            }
            return false;
        }
        private List<Line> FindDirectLine(Line objectLine, ref List<Line> lines)
        {
            var remLines = new List<Line>();
            var retLines = new List<Line>();
            foreach (var target in lines)
            {
                if (IsDirectLine(objectLine, target))
                {
                    remLines.Add(target);
                    retLines.Add(target);
                }
            }
            lines = lines.Except(remLines).ToList();
            return retLines;
        }
        private List<Line> FindNearLine(Line objectLine, ref List<Line> lines)
        {
            var remLines = new List<Line>();
            var retLines = new List<Line>();
            foreach (var target in lines)
            {
                if (IsNearLine(objectLine, target))
                {
                    retLines.Add(target);
                    remLines.Add(target);
                }
            }
            lines = lines.Except(remLines).ToList();
            return retLines;
        }

    }
}
