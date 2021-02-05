using System;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using ThMEPLighting.Garage.Model;
using System.Collections.Generic;
using ThMEPLighting.Garage.Service;
using Autodesk.AutoCAD.DatabaseServices;
using EndCapStyle = NetTopologySuite.Operation.Buffer.EndCapStyle;
using ThMEPEngineCore.LaneLine;
using ThMEPLighting.Common;
using NFox.Cad;

namespace ThMEPLighting.Garage.Engine
{
    public class ThBuildRacewayEngineEx:IDisposable
    {
        /// <summary>
        ///(双排：1号线、2号线、非灯线，单排：中心线、非灯线)
        /// </summary>
        private List<Line> CenterLines { get; set; }
        /// <summary>
        /// 被切割的线槽边线
        /// </summary>
        public List<Line> SplitSides { get; set; }
        /// <summary>
        /// 被切割的中心线
        /// </summary>
        public List<Line> SplitCenters { get; set; }

        public Dictionary<Line, List<Line>> CenterWithSides { get; set; }
        public Dictionary<Line, List<Line>> CenterWithPorts { get; set; }

        /// <summary>
        /// 线槽宽度
        /// </summary>
        private double Width { get; set; }
        public ThBuildRacewayEngineEx(
            List<Line> centerLines,
            double width)
        {
            CenterLines = centerLines;           
            Width = width;
            SplitSides = new List<Line>();
            SplitCenters = new List<Line>();
            CenterWithSides = new Dictionary<Line, List<Line>>();
            CenterWithPorts = new Dictionary<Line, List<Line>>();
        }
        public void Dispose()
        {
        }        
        public void Build()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var handleObjs = Preprocess(); //对传入的中心线处理
                var sideLines = Buffer(handleObjs); //获取Buffer后的线槽边线     
                this.SplitSides = SplitSideLines(sideLines); //切割线槽边线

                // 切割中心线
                this.SplitCenters = SplitCenterLines(handleObjs.Cast<Line>().ToList());
                var sideParameter = new ThFindSideLinesParameter
                {
                    CenterLines = this.SplitCenters,
                    SideLines = this.SplitSides,
                    HalfWidth = Width / 2.0
                };

                //查找合并线buffer后，获取中心线对应的两边线槽线
                var instane = ThFindSideLinesService.Find(sideParameter);
                CenterWithPorts = instane.PortLinesDic;
                CenterWithSides = instane.SideLinesDic;
            }
        }
        private DBObjectCollection Preprocess()
        {
            var lineObjs = ThLaneLineEngine.Explode(CenterLines.ToCollection());
            // 连接
            var objs = ThLaneLineJoinEngine.Join(lineObjs);
            // 合并
            return ThLaneLineMergeExtension.Merge(objs);
        }
        private List<Line> Buffer(DBObjectCollection objs)
        {
            var lineObjs = ThLaneLineEngine.Explode(objs);
            var extends = new DBObjectCollection();
            lineObjs.Cast<Line>().ForEach(o => extends.Add(o.ExtendLine(2.0)));
            var nodes = ThLaneLineEngine.Noding(extends);
            nodes=nodes.Cast<Line>().Where(o => o.Length > 5.0).ToCollection();
            var bufferObjs = nodes.LineMerge().Buffer(Width / 2.0, EndCapStyle.Flat);
            //此处不要在延伸了
            // 获取Buffer后所有Polyline的组成线
            return bufferObjs.GetLines().Where(o=>o.Length>1.0).ToList();
        }
        private List<Line> SplitSideLines(List<Line> sideLines , double cutLineLength = 2.0)
        {
            // 获取要切割线槽的线
            var cutLines = ThCollectCutLinesService.Collect(
                CenterLines, Width / 2.0, cutLineLength);
            var cableObjs = new DBObjectCollection();
            sideLines.ForEach(o => cableObjs.Add(o));
            cutLines.ForEach(o => cableObjs.Add(o));
            var handleObjs = ThLaneLineEngine.Noding(cableObjs);           
            return handleObjs.Cast<Line>().Where(o=>o.Length> (cutLineLength + 1.0)).ToList();
        }

        private List<Line> SplitCenterLines(List<Line> centerLines)
        {
            // 获取要切割线槽的线
            var extendLines = new List<Line>();
            centerLines.ForEach(o => extendLines.Add(o.ExtendLine(2.0)));
            var handleObjs = ThLaneLineEngine.Noding(extendLines.ToCollection());
            return handleObjs.Cast<Line>().Where(o => o.Length > 5.0).ToList();
        }  
        
        public List<Point3d> GetPorts()
        {
            var ports = new List<Point3d>();
            CenterWithPorts.ForEach(m =>
            {
                var pts = new List<Point3d>();
                var normalize = ThGarageLightUtils.NormalizeLaneLine(m.Key);
                m.Value.ForEach(n => pts.Add(ThGeometryTool.GetMidPt(n.StartPoint, n.EndPoint)));
                ports.AddRange(pts.OrderBy(p => normalize.StartPoint.DistanceTo(p)));
            });
            return ports;
        }
    }
}
