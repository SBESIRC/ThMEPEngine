using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.UndergroundWaterSystem.Command;
using ThMEPWSS.UndergroundWaterSystem.Engine;

namespace ThMEPWSS.UndergroundWaterSystem.Service
{
    public class ThPipeExtractionService
    {
        public List<Line> GetPipeLines(Point3dCollection pts=null)
        {
            var waterPipeEngine = new ThPipeExtractionEngine();
            //取出所有满足条件的线
            var retList = waterPipeEngine.GetPipeLines(pts);
            return retList;
        }
        public List<Line> GetWaterPipeList(Point3d startPt)
        {
            using (var database = AcadDatabase.Active())
            {
                //先提取到目标元素
                var waterPipeEngine = new ThPipeExtractionEngine();
                //取出所有满足条件的线
                var allLines = waterPipeEngine.GetWaterPipeLines();
                //再使用startPt进行过滤，得到目标线
                var retLines = FindSeriesLine(startPt,allLines);
                return retLines;
            }
        }
        /// <summary>
        /// startPt为起点的相连线
        /// </summary>
        /// <param name="startPt"></param>
        /// <returns></returns>
        public List<Line> FindSeriesLine(Point3d startPt,List<Line> allLines)
        {
            //查找到与起点相连的线
            var startLine = ThUndergroundWaterSystemUtils.FindStartLine(startPt, allLines);
            if(startLine == null)
            {
                return null;
            }
            //查找到与startLine相连的一系列线
            var retLines = FindSeriesLine(startLine,ref allLines);
            return retLines;
        }
        public List<Line> FindSeriesLine(Line objectLine,ref List<Line> allLines)
        {
            var retLines = new List<Line>();
            var conLines = FindConnectLine(objectLine, ref allLines);
            retLines.AddRange(conLines);
            foreach (var line in conLines)
            {
                retLines.AddRange(FindSeriesLine(line, ref allLines));
            }
            return retLines;
        }
        public List<Line> FindConnectLine(Line objectLine, ref List<Line> lines)
        {
            var retLines = new List<Line>();
            retLines.AddRange(FindDirectLine(objectLine, ref lines));
            retLines.AddRange(FindNearLine(objectLine, ref lines));
            return retLines;
        }
        private bool IsDirectLine(Line objectLine , Line targetLine)
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
            if(distance1 < 10.0 || distance2 < 10.0)
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
                if(IsDirectLine(objectLine,target))
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
                if(IsNearLine(objectLine,target))
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
