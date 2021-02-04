using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.LaneLine;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service
{
    public class ThCollectCutLinesService
    {
        private List<Line> CutLines { get; set; }
        private ThQueryLineService QueryLineService;  
        /// <summary>
        /// 线槽宽度/2.0
        /// </summary>
        private double Distance;
        private List<Line> centerLines;  
        /// <summary>
        /// 要创建的Cut线的长度
        /// </summary>
        private double Length { get; set; }

        public ThCollectCutLinesService(List<Line> lines,double distance,double length)
        {
            centerLines = Preprocess(lines);
            QueryLineService = ThQueryLineService.Create(centerLines);
            if(length > 0.0)
            {
                Length = length;
            }
            else
            {
                Length = 2.0;
            }
            Distance = distance;
            CutLines = new List<Line>();
        }
        public static List<Line> Collect(List<Line> lines, double width,double length)
        {
            var instance = new ThCollectCutLinesService(lines, width, length);
            instance.Collect();
            return instance.CutLines;
        }
        private void Collect()
        {
            centerLines.ForEach(o =>
            {
                var startCutLine = GetCutLine(o, true);
                var endCutLine = GetCutLine(o, false);
                if(startCutLine.Length>0.0)
                {                  
                    CutLines.Add(startCutLine);
                }
                if (endCutLine.Length>0.0)
                {
                    CutLines.Add(endCutLine);
                }
            });
        }
        private List<Line> Preprocess(List<Line> lines)
        {
            var newLines = new List<Line>();
            lines.ForEach(o => newLines.Add(o.ExtendLine(1.0)));
            var nodedLines = ThLaneLineEngine.Noding(newLines.ToCollection());
            nodedLines = ThLaneLineEngine.CleanZeroCurves(nodedLines);
            return nodedLines.Cast<Line>().ToList();
        }
        private Line GetCutLine (Line line,bool isStart)
        {           
            var portPt = isStart ? line.StartPoint : line.EndPoint;
            var links = QueryLineService.Query(portPt, 2.0, true);
            links.Remove(line);
            bool needExtend = links.Count == 2 &&
            ThGeometryTool.IsCollinearEx(
                links[0].StartPoint, links[0].EndPoint,
                links[1].StartPoint, links[1].EndPoint);
            if(needExtend)
            {
                var vec = line.StartPoint.GetVectorTo(line.EndPoint).GetNormal();
                double distance = CalculateExtendLength(links[0], line, Distance);
                if (isStart)
                {
                    var extendPt = line.StartPoint - vec.MultiplyBy(Distance);
                    var lineSp = extendPt - vec.MultiplyBy(Length/2.0);
                    var lineEp = extendPt + vec.MultiplyBy(Length / 2.0);
                    return new Line(lineSp, lineEp);
                }
                else
                {
                    var extendPt = line.EndPoint + vec.MultiplyBy(Distance);
                    var lineSp = extendPt - vec.MultiplyBy(Length / 2.0);
                    var lineEp = extendPt + vec.MultiplyBy(Length / 2.0);
                    return new Line(lineSp, lineEp);
                }
            }
            else
            {
                return new Line();
            }
        }   
        private double CalculateExtendLength(Line main, Line branch, double gap)
        {
            var mainVec = main.StartPoint.GetVectorTo(main.EndPoint);
            var branchVec = branch.StartPoint.GetVectorTo(branch.EndPoint);
            var ang = mainVec.GetAngleTo(branchVec);
            if (ang > Math.PI)
            {
                ang -= Math.PI;
            }
            return Math.Abs(gap / Math.Sin(ang));
        }
    }
}
