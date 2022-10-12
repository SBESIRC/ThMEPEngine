using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.ObliqueMPartitionLayout
{
    public partial class ObliqueMPartition 
    {
        public void CompactPro()
        {
            RemoveDuplicatedLanes(IniLanes);
            var compactedLanesgroup = CompactForGenerateLanesPro(IniLanes, OutBoundary, Boundary, ObstaclesSpatialIndex, CarSpatialIndex, Cars, InitialLanes, MaxLength);
            compactedLanesgroup= compactedLanesgroup.Where(e => e.CompactedLanes[0].MoveableDistance > 0).ToList();
            if (compactedLanesgroup.Count > 0)
            {
                hasCompactedLane = true;
                AdjustLanes(compactedLanesgroup);
                AdjustSeriesBoxes();
                ClearParas();
                ReLayout();
            }
        }
        void ReLayout()
        {
            GenerateCarsInModules();
            //ProcessLanes(ref IniLanes);
            GenerateCarsOnRestLanes();
            PostProcess();
        }
        void ClearParas()
        {
            ParentDir = Vector2D.Zero;
            OutputLanes = new List<LineSegment>();
            OutEnsuredLanes = new List<LineSegment>();
            OutUnsuredLanes = new List<LineSegment>();
            CarSpatialIndex = new MNTSSpatialIndex(new List<Geometry>());
            CarSpots = new List<Polygon>();
            Pillars = new List<Polygon>();
            Cars = new List<InfoCar>();
            IniPillar = new List<Polygon>();
            //LaneBufferSpatialIndex = new MNTSSpatialIndex(new List<Geometry>());
            //LaneBoxes = new List<Polygon>();
            //LaneSpatialIndex = new MNTSSpatialIndex(new List<Geometry>());

        }
        void AdjustSeriesBoxes()
        {
            LaneBufferSpatialIndex = new MNTSSpatialIndex(IniLanes.Select(e => e.Line.Buffer(DisLaneWidth / 2)));
            LaneBoxes = IniLanes.Select(e => e.Line.Buffer(DisLaneWidth / 2)).ToList();
            LaneSpatialIndex = new MNTSSpatialIndex(IniLanes.Select(e => PolyFromLine(e.Line)));

            CarBoxes = new List<Polygon>();
            CarBoxesPlus = new List<CarBoxPlus>();
            //CarModules = new List<CarModule>();
            CarBoxesSpatialIndex = new MNTSSpatialIndex(CarBoxes);
            CarBoxesSpatialIndex.Update(IniLanes.Where(e => Boundary.ClosestPoint(e.Line.MidPoint).Distance(e.Line.MidPoint) > 10).
                Select(e => PolyFromLines(e.Line, e.Line.Translation(e.Vec.Normalize() * DisCarAndHalfLaneBackBack))).ToArray(), new List<Polygon>());

            foreach (var ln in IniLanes)
            {
                ln.CanExtend = true;
                ln.CanBeMoved = true;
                //ln.IsGeneratedForLoopThrough = false;
                //ln.IsAdjLaneForProcessLoopThroughEnd = false;
                ln.GEndAdjLine = false;
                ln.GStartAdjLine = false;
            }

        }
        public void AdjustLanes(List<CompactedLaneGroup> compactedLanesgroup)
        {
            var targetedLanes = new List<Lane>();
            foreach (var group in compactedLanesgroup)
                foreach (var lane in group.CompactedLanes)
                {
                    var _lane = new Lane(lane.Lane)
                    {
                        MoveableDistanceForCompacted = lane.MoveableDistance,
                        VecforCompacted = group.Vector
                    };
                    targetedLanes.Add(_lane);
                }
            MoveCompactedLanes(targetedLanes);
            AdjustRestLanes();

            MoveCompactedCarModules(targetedLanes);
            AdjustRestCarModules();
        }
        void AdjustRestCarModules()
        {
            var compactedModules = CarModules.Where(e => e.AdjustedForCompacted).ToList();
            CarModules = CarModules.Except(compactedModules).ToList();
            for (int i = 0; i < CarModules.Count; i++)
            {
                foreach (var line in compactedModules.Select(e => e.Line))
                {
                    if (CarModules[i].Line.IntersectPoint(line.ToLineString()).Count() < 0)
                        continue;
                    var splits = SplitLine(line, new List<Coordinate>() { CarModules[i].Line.P0, CarModules[i].Line.P1 }).Where(e => e.Length > 1).ToList();
                    if (splits.Count <= 1)
                        continue;
                    splits = SplitLine(CarModules[i].Line, new List<Coordinate>() { line.P0, line.P1 }).Where(e => e.Length > 1).ToList();
                    if (splits.Count > 1)
                    {
                        var newLine = splits.OrderByDescending(e => e.Length).First();
                        CarModules[i].Line = newLine;
                        CarModules[i].Box = PolyFromLines(newLine, newLine.Translation(CarModules[i].Vec.Normalize() * DisCarAndHalfLaneBackBack));
                    }
                }
            }
            CarModules.AddRange(compactedModules);
        }
        void AdjustRestLanes()
        {
            var compactedLanes = IniLanes.Where(e => e.AdjustedForCompacted).ToList();
            IniLanes= IniLanes.Except(compactedLanes).ToList();
            for (int i = 0; i < IniLanes.Count; i++)
            {
                foreach (var line in compactedLanes.Select(e => e.Line))
                {
                    if(IniLanes[i].Line.IntersectPoint(line.ToLineString()).Count() < 0)
                        continue;
                    var splits= SplitLine(line, new List<Coordinate>() { IniLanes[i].Line.P0, IniLanes[i].Line.P1 }).Where(e => e.Length > 1).ToList();
                    if (splits.Count <= 1)
                        continue;
                    splits = SplitLine(IniLanes[i].Line, new List<Coordinate>() { line.P0, line.P1 }).Where(e => e.Length > 1).ToList();
                    if (splits.Count > 1)
                    {
                        var newLine = splits.OrderByDescending(e => e.Length).First();
                        IniLanes[i].Line = newLine;
                    }
                }
            }
            IniLanes.AddRange(compactedLanes);
        }
        void MoveCompactedCarModules(List<Lane> targetedLanes)
        {
            for (int i = 0; i < CarModules.Count; i++)
            {

                for (int j = 0; j < targetedLanes.Count; j++)
                {
                    if (HasOverlay(CarModules[i].Line, targetedLanes[j].Line))
                    {
                        var line = new LineSegment(CarModules[i].Line);
                        line = CarModules[i].Line.Translation(targetedLanes[j].VecforCompacted.Normalize() * targetedLanes[j].MoveableDistanceForCompacted);
                        var buffer = line.Buffer(DisLaneWidth / 2);
                        buffer = buffer.Scale(ScareFactorForCollisionCheck);
                        var obscrossed = ObstaclesSpatialIndex.SelectCrossingGeometry(buffer).Cast<Polygon>().ToList();
                        var points = new List<Coordinate>();
                        foreach (var obs in obscrossed)
                        {
                            points.AddRange(obs.IntersectPoint(line.Buffer(DisLaneWidth / 2)));
                            points.AddRange(obs.Coordinates.Where(p => buffer.Contains(p)));
                        }
                        points = points.Select(p => line.ClosestPoint(p)).ToList();
                        var splits = SplitLine(line, points).Where(e => e.Length > 1)
                            .Where(e => !IsInAnyPolys(e.MidPoint, obscrossed)).ToList();
                        if (splits.Count > 0)
                        {
                            if (IsConnectedToLane(line, false, IniLanes) && !IsConnectedToLane(line, true, IniLanes))
                            {
                                CarModules[i].Line = splits.Last();
                                CarModules[i].AdjustedForCompacted = true;
                                CarModules[i].Box = PolyFromLines(CarModules[i].Line, CarModules[i].Line.Translation(CarModules[i].Vec.Normalize() * DisCarAndHalfLaneBackBack));
                            }
                            else if (IsConnectedToLaneDouble(IniLanes[i].Line, IniLanes) && splits.Count > 1)
                            {
                                CarModules[i].Line = splits.First();
                                CarModules[i].Box = PolyFromLines(CarModules[i].Line, CarModules[i].Line.Translation(CarModules[i].Vec.Normalize() * DisCarAndHalfLaneBackBack));
                                CarModules[i].AdjustedForCompacted = true;
                                CarModules.Add(new CarModule(
                                    PolyFromLines(splits.Last(), splits.Last().Translation(CarModules[i].Vec.Normalize() * DisCarAndHalfLaneBackBack)),
                                    splits.Last(), CarModules[i].Vec)
                                {
                                    IsInBackBackModule = CarModules[i].IsInBackBackModule,
                                    IsInVertUnsureModule = CarModules[i].IsInVertUnsureModule,
                                    IsSingleModule = CarModules[i].IsSingleModule,
                                });
                                CarModules[CarModules.Count-1].AdjustedForCompacted = true;
                            }
                            else
                            {
                                CarModules[i].Line = splits.First();
                                CarModules[i].AdjustedForCompacted = true;
                                CarModules[i].Box = PolyFromLines(CarModules[i].Line, CarModules[i].Line.Translation(CarModules[i].Vec.Normalize() * DisCarAndHalfLaneBackBack));
                            }
                        }
                    }
                }
            }
        }
        void MoveCompactedLanes(List<Lane> targetedLanes)
        {
            //与收缩线重合，根据收缩线的位置调整
            for (int i = 0; i < IniLanes.Count; i++)
            {
                for (int j = 0; j < targetedLanes.Count; j++)
                {
                    if (HasOverlay(IniLanes[i].Line, targetedLanes[j].Line))
                    {
                        var line = new LineSegment(IniLanes[i].Line);
                        line = IniLanes[i].Line.Translation(targetedLanes[j].VecforCompacted.Normalize() * targetedLanes[j].MoveableDistanceForCompacted);
                        var buffer = line.Buffer(DisLaneWidth / 2);
                        buffer = buffer.Scale(ScareFactorForCollisionCheck);
                        var obscrossed = ObstaclesSpatialIndex.SelectCrossingGeometry(buffer).Cast<Polygon>().ToList();
                        var points = new List<Coordinate>();
                        foreach (var obs in obscrossed)
                        {
                            points.AddRange(obs.IntersectPoint(line.Buffer(DisLaneWidth / 2)));
                            points.AddRange(obs.Coordinates.Where(p => buffer.Contains(p)));
                        }
                        points = points.Select(p => line.ClosestPoint(p)).ToList();
                        var splits = SplitLine(line, points).Where(e => e.Length > 1)
                            .Where(e => !IsInAnyPolys(e.MidPoint, obscrossed)).ToList();
                        if (splits.Count > 0)
                        {
                            if (IsConnectedToLane(line, false, IniLanes) && !IsConnectedToLane(line, true, IniLanes))
                            {
                                IniLanes[i].Line = splits.Last();
                                IniLanes[i].AdjustedForCompacted = true;
                            }
                            else if (IsConnectedToLaneDouble(IniLanes[i].Line, IniLanes) && splits.Count > 1)
                            {
                                IniLanes[i].Line = splits.First();
                                IniLanes[i].AdjustedForCompacted = true;
                                IniLanes.Add(new Lane(IniLanes[i]) { Line = splits.Last() });
                                IniLanes[IniLanes.Count - 1].AdjustedForCompacted = true;
                            }
                            else
                            {
                                IniLanes[i].Line = splits.First();
                                IniLanes[i].AdjustedForCompacted = true;
                            }
                        }
                    }
                }
            }
        }

    }
}
