using System;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using ThMEPLighting.Garage.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThCableTrayBuilder : IDisposable
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
        public ThCableTrayBuilder(
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
                this.SplitSides = ThLightSideLineCreator.Create(CenterLines, Width); //切割线槽边线

                // 切割中心线
                this.SplitCenters = CenterLines; // CenterLines在外部已经被Noding过了
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
