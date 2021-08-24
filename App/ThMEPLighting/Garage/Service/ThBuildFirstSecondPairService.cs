using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThBuildFirstSecondPairService
    {
        //1号线来源于图纸中已编号的线,需要Noding后传入
        //2号线来源于图纸中已编号的线,需要Merge后传入,偏于后期精确布点

        public Dictionary<Line, List<Line>> Pairs { get; private set; }
        private ThCADCoreNTSSpatialIndex SecondSpatialIndex { get; set; }
        private List<Line> FirstLines { get; set; }
        /// <summary>
        /// 双排线槽间距
        /// </summary>
        private double RacywaySpace { get; set; } 
        public ThBuildFirstSecondPairService(
            List<Line> firstLines, 
            List<Line> secondLines,
            double racywaySpace)
        {
            RacywaySpace = racywaySpace;
            FirstLines = firstLines;
            Pairs = new Dictionary<Line, List<Line>>();
            SecondSpatialIndex = ThGarageLightUtils.BuildSpatialIndex(secondLines);
        }
        public void Build()
        {
            FirstLines.ForEach(o =>
            {
                var newLine = ThGarageLightUtils.NormalizeLaneLine(o);
                //1.2线偏移逻辑采用汤永靖的偏移逻辑
                var length = ThOffsetLineService.CalOffsetLength(newLine, RacywaySpace);
                var objs = newLine.GetOffsetCurves(-length);
                var offsetLine = objs[0] as Line;
                //var vec = newLine.StartPoint.GetVectorTo(newLine.EndPoint).GetNormal();
                //var perpendVec = vec.GetPerpendicularVector();
                //var sp = newLine.StartPoint - perpendVec.MultiplyBy(RacywaySpace);
                //var ep = newLine.EndPoint - perpendVec.MultiplyBy(RacywaySpace);
                var secondlines = Filter(offsetLine.StartPoint, offsetLine.EndPoint);
                if(secondlines.Count==0)
                {
                    objs = newLine.GetOffsetCurves(length);
                    offsetLine = objs[0] as Line;
                    secondlines = Filter(offsetLine.StartPoint, offsetLine.EndPoint);
                }
                Pairs.Add(o, secondlines);
            });
        }
        private List<Line> Filter(Point3d sp,Point3d ep)
        {
            //找到与first平行
            //且有共同部分的线
            var line = new Line(sp, ep);
            var rectangle = ThDrawTool.ToRectangle(sp, ep, 2.0);
            var objs = SecondSpatialIndex.SelectCrossingPolygon(rectangle);
            var vec = sp.GetVectorTo(ep);
            return objs
                .Cast<Line>()
                .Where(o => vec.IsParallelToEx(o.StartPoint.GetVectorTo(o.EndPoint)))
                .Where(o=> line.HasCommon(o))
                .ToList();
        }
    }
}
