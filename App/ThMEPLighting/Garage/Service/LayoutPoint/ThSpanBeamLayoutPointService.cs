using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service.LayoutPoint
{
    public class ThSpanBeamLayoutPointService : ThLayoutPointService
    {
        public double HalfLampLength { get; set; } = 600.0;
        private const double SpanBeamMargin = 200.0;
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public ThSpanBeamLayoutPointService(DBObjectCollection beams)
        {
            SpatialIndex= new ThCADCoreNTSSpatialIndex(beams);
        }
        public override List<Point3d> Layout(List<Line> dxLines)
        {
            var results = new List<Point3d>();

            // 计算在梁和不在梁内的线
            var splitLines = Calculate(dxLines);

            // 布置点
            results.AddRange(LinearDistribute(splitLines.Item1, this.Margin, this.Interval));
            results.AddRange(LinearDistribute(splitLines.Item2, SpanBeamMargin, this.Interval));
            return results;
        }

        private Tuple<List<Line>, List<Line>> Calculate(List<Line> dxLines)
        {
            var nonBeamIntervalLinesCollector = new List<Line>(); // 不在梁间隔内的线
            var beamIntervalLinesCollector = new List<Line>(); // 在梁间隔内的线
            dxLines.ForEach(l =>
            {
                // 把梁沿着其所在线的方向左、右偏移
                var offsetBeams = Build(l);

                // 收集左右偏移的梁
                var lrBeams = GetLRBeams(offsetBeams);

                // 找出所有被梁分割的线
                var layoutLines = l.Calculate(lrBeams);

                // 找出在梁间隙内的线
                var beamIntervalLines = FindBeamIntervalLines(layoutLines, offsetBeams);
                beamIntervalLinesCollector.AddRange(beamIntervalLines);

                // 找出不在梁间隙内的线
                var nonBeamIntervalLines = layoutLines.Where(o => !beamIntervalLines.Contains(o)).ToList();
                nonBeamIntervalLinesCollector.AddRange(nonBeamIntervalLines);
            });
            return Tuple.Create(nonBeamIntervalLinesCollector, beamIntervalLinesCollector);
        }

        private List<Line> FindBeamIntervalLines(List<Line> lines,List<Tuple<Entity,Entity,Entity>> offsetBeams)
        {
            var beamSpatialIndex = new ThCADCoreNTSSpatialIndex(GetLRBeams(offsetBeams));

            Func<Line, bool> Query = (Line line) =>
             {
                 var newLine = line.ExtendLine(1.0);
                 var rec = ThDrawTool.ToRectangle(newLine.StartPoint, newLine.EndPoint, 1.0);
                 var objs = beamSpatialIndex.SelectCrossingPolygon(rec);
                 objs = objs.Distinct();
                 if(objs.Count==2)
                 {
                     return offsetBeams.Where(b => objs.Contains(b.Item2) && objs.Contains(b.Item3)).Any();
                 }
                 return false;
             };

            return lines.Where(l => Query(l)).ToList();
        }

        private DBObjectCollection GetLRBeams(List<Tuple<Entity, Entity, Entity>> offsetBeams)
        {
            var lrBeams = new DBObjectCollection();
            offsetBeams.ForEach(e =>
            {
                lrBeams.Add(e.Item2);
                lrBeams.Add(e.Item3);
            });
            return lrBeams;
        }

        public override List<Point3d> Layout(List<Line> L1Lines, List<Line> L2Lines)
        {
            var results  = new List<Point3d>(); 
            // nonBeamIntervalLines,beamIntervalLines
            var l1SplitLines = Calculate(L1Lines); // L1被梁分割的线
            var l2SplitLines = Calculate(L2Lines); // L2被梁分割的线

            var nonBeamIntervalRes = CalculatePubExclusiveLines(l1SplitLines.Item1, l2SplitLines.Item1);            
            var l1PubLayoutPoints = LinearDistribute(nonBeamIntervalRes.L1Pubs, this.Margin, this.Interval);
            var l2PubLayoutPoints = GetL2LayoutPointByPass(l1PubLayoutPoints, L1Lines, L2Lines);
            var l1ExclusiveLayoutPoints = LinearDistribute(nonBeamIntervalRes.L1Exclusives, this.Margin, this.Interval);
            var l2ExclusiveLayoutPoints = LinearDistribute(nonBeamIntervalRes.L2Exclusives, this.Margin, this.Interval);

            //var beamIntervalRes = CalculatePubExclusiveLines(l1SplitLines.Item2, l2SplitLines.Item2);
            var l1BeamIntervalLayoutPoints = LinearDistribute(l1SplitLines.Item2, SpanBeamMargin, this.Interval);
            var l2BeamIntervalLayoutPoints = LinearDistribute(l2SplitLines.Item2, SpanBeamMargin, this.Interval);

            results.AddRange(l1PubLayoutPoints);
            results.AddRange(l2PubLayoutPoints);
            results.AddRange(l1ExclusiveLayoutPoints);
            results.AddRange(l2ExclusiveLayoutPoints);
            results.AddRange(l1BeamIntervalLayoutPoints);
            results.AddRange(l2BeamIntervalLayoutPoints);
            return results;
        }

        private List<Tuple<Entity, Entity, Entity>> Build(Line line)
        {
            var dir = line.LineDirection().MultiplyBy(HalfLampLength);
            var objs = Query(line);
            var rightMt = Matrix3d.Displacement(dir);
            var leftMt = Matrix3d.Displacement(dir.Negate());
            return objs
                .OfType<Entity>()
                .Select(e => Tuple.Create(e, e.GetTransformedCopy(leftMt), e.GetTransformedCopy(rightMt)))
                .ToList();
        }

        private DBObjectCollection Query(Line line)
        {
            var rec = line.Buffer(1.0);
            return SpatialIndex.SelectCrossingPolygon(rec);
        }
    }
}
