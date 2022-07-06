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
        public override List<Tuple<Point3d,Vector3d>> Layout(List<Line> dxLines)
        {
            var results = new List<Tuple<Point3d, Vector3d>>();
            // 计算在梁和不在梁内的线
            var newBeams = CalculateOffsetBeams(dxLines);

            // 计算布置的点
            var newDxLines = ThMergeLightLineService.Merge(dxLines);
            newDxLines.ForEach(link =>
            {
                var unLayoutLines = link.CalculateUnLayoutParts(newBeams);
                var path = link.ToPolyline();
                var pts = PolylineDistribute(path, unLayoutLines, this.Interval, this.Margin,0.0);
                results.AddRange(DistributeLaytoutPoints(pts, link));
                path.Dispose();
                unLayoutLines.ForEach(l => l.Dispose());
            });
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
                var layoutLines = new List<Line> {l}.CalculateLayoutParts(lrBeams);

                // 找出在梁间隙内的线
                var beamIntervalLines = FindBeamIntervalLines(layoutLines, offsetBeams);
                beamIntervalLinesCollector.AddRange(beamIntervalLines);

                // 找出不在梁间隙内的线
                var nonBeamIntervalLines = layoutLines.Where(o => !beamIntervalLines.Contains(o)).ToList();
                nonBeamIntervalLinesCollector.AddRange(nonBeamIntervalLines);
            });
            return Tuple.Create(nonBeamIntervalLinesCollector, beamIntervalLinesCollector);
        }

        private DBObjectCollection CalculateOffsetBeams(List<Line> dxLines)
        {
            var results = new DBObjectCollection();
            dxLines.ForEach(l =>
            {
                // 把梁沿着其所在线的方向左、右偏移
                var offsetBeams = Build(l);

                // 收集左右偏移的梁
                var lrBeams = GetLRBeams(offsetBeams);

                // 添加到结果集
                results = results.Union(lrBeams);
            });
            return results;
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

        public override List<Tuple<Point3d, Vector3d>> Layout(List<Line> L1Lines, List<Line> L2Lines)
        {
            var results  = new List<Tuple<Point3d, Vector3d>>();
            // 计算在梁和不在梁内的线
            var l1Beams = CalculateOffsetBeams(L1Lines);
            var l2Beams = CalculateOffsetBeams(L2Lines);

            // 计算L1,L2不可布区域
            var l1UnLayoutLines = L1Lines.CalculateUnLayoutParts(l1Beams);
            var l2UnLayoutLines = L2Lines.CalculateUnLayoutParts(l2Beams);

            // 计算L1上布置的点
            var newL1Lines = ThMergeLightLineService.Merge(L1Lines);
            // 把L2不可布区域投影到L1上
            var l1NewUnLayoutLines = GetProjectionLinesByPass(l2UnLayoutLines, L2Lines, L1Lines);
            var l1UnLayoutQuery = ThQueryLineService.Create(l1UnLayoutLines.Union(l1NewUnLayoutLines).ToList());
            var l1LayoutPoints = new List<Tuple<Point3d, Vector3d>>();
            newL1Lines.ForEach(link =>
            {
                var unLayoutLines = link.SelectMany(o => l1UnLayoutQuery.QueryCollinearLines(o.StartPoint, o.EndPoint)).ToList();
                var path = link.ToPolyline();
                var pts = PolylineDistribute(path, unLayoutLines, this.Interval, this.Margin,0.0);
                l1LayoutPoints.AddRange(DistributeLaytoutPoints(pts,link));
                path.Dispose();
            });

            // 把L1上布置的点投影到L2上
            var l2LayoutPoints = GetL2LayoutPointByPass(l1LayoutPoints, L1Lines, L2Lines);
            var l1LayoutLines = L1Lines.CalculateLayoutParts(l1Beams);
            var l2LayoutLines = L2Lines.CalculateLayoutParts(l2Beams);
            var l1l2PubExclusiveInfo = CalculatePubExclusiveLines(l1LayoutLines, l2LayoutLines);
            var newL2Exclusives = ThMergeLightLineService.Merge(l1l2PubExclusiveInfo.L2Exclusives);
            var l2UnLayoutQuery = ThQueryLineService.Create(l2UnLayoutLines);
            newL2Exclusives.ForEach(link =>
            {
                var unLayoutLines = link.SelectMany(o => l2UnLayoutQuery.QueryCollinearLines(o.StartPoint, o.EndPoint)).ToList();
                var path = link.ToPolyline();
                var pts = PolylineDistribute(path, unLayoutLines, this.Interval, this.Margin,0.0);
                l2LayoutPoints.AddRange(DistributeLaytoutPoints(pts,link));
                path.Dispose();
            });

            results.AddRange(l1LayoutPoints);
            results.AddRange(l2LayoutPoints);
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
