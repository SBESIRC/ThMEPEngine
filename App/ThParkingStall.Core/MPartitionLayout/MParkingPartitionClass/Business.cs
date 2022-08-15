using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.MPartitionLayout
{
    public partial class MParkingPartitionPro
    {
        public void ProcessLanes(ref List<Lane> lanes, bool preprocess = false)
        {
            for (int i = 0; i < lanes.Count; i++)
            {
                if (IsConnectedToLaneDouble(lanes[i].Line)) continue;
                if (IsConnectedToLane(lanes[i].Line, false))
                    lanes[i].Line = new LineSegment(lanes[i].Line.P1, lanes[i].Line.P0);
                var endp = lanes[i].Line.P1;
                if (Boundary.ClosestPoint(endp).Distance(endp) < 0.1 && Boundary.ClosestPoint(endp).Distance(endp) > 0)
                {
                    lanes[i].Line.P1 = Boundary.ClosestPoint(endp);
                    continue;
                }
                else if (Boundary.ClosestPoint(endp).Distance(endp) == 0) continue;
                var laneSdl = LineSegmentSDL(endp, Vector(lanes[i].Line).Normalize(), 10000);
                var laneSdlbuffer = laneSdl.Buffer(DisLaneWidth / 2);
                var obscrossed = ObstaclesSpatialIndex.SelectCrossingGeometry(laneSdlbuffer).Cast<Polygon>().ToList();
                var car_crossed= CarSpatialIndex.SelectCrossingGeometry(laneSdlbuffer).Cast<Polygon>().ToList();
                var next_to_obs = false;
                foreach (var cross in obscrossed)
                {
                    if (cross.ClosestPoint(lanes[i].Line.P1).Distance(lanes[i].Line.P1) < 1 || cross.Contains(lanes[i].Line.P1))
                    {
                        next_to_obs = true;
                        break;
                    }
                }
                foreach (var cross in car_crossed)
                {
                    if (cross.ClosestPoint(lanes[i].Line.P1).Distance(lanes[i].Line.P1) < 1 || cross.Contains(lanes[i].Line.P1))
                    {
                        next_to_obs = true;
                        break;
                    }
                }
                if (next_to_obs) continue;
                var points = new List<Coordinate>();
                foreach (var cross in obscrossed)
                {
                    points.AddRange(cross.Coordinates);
                    points.AddRange(cross.IntersectPoint(laneSdlbuffer));
                }
                foreach (var cross in CarSpatialIndex.SelectCrossingGeometry(laneSdlbuffer).Cast<Polygon>())
                {
                    points.AddRange(cross.Coordinates);
                    points.AddRange(cross.IntersectPoint(laneSdlbuffer));
                }
                points = points.Where(p => laneSdlbuffer.Contains(p) || laneSdlbuffer.ClosestPoint(p).Distance(p)<1).ToList();
                points.AddRange(Boundary.IntersectPoint(laneSdlbuffer));
                points = points.Select(p => laneSdl.ClosestPoint(p)).ToList();
                if (preprocess)
                {
                    points = new List<Coordinate>();
                    points.AddRange(Boundary.IntersectPoint(laneSdl.ToLineString()));
                }
                var splits = SplitLine(laneSdl, points).Where(e =>
                {
                    var split_bf = e.Buffer(DisLaneWidth / 2 - 1);
                    foreach (var cross in obscrossed)
                        if (cross.IntersectPoint(split_bf).Count() > 0) return false;
                    foreach (var cross in CarSpatialIndex.SelectCrossingGeometry(laneSdlbuffer).Cast<Polygon>())
                    {
                        if (cross.IntersectPoint(split_bf).Count() > 0) return false;
                    }
                    return true;
                });
                if (splits.Count() > 0)
                {
                    var split = splits.First();
                    if (/*split.Length > 10 && */split.Length < 10000)
                    {
                        lanes[i].Line = new LineSegment(lanes[i].Line.P0, split.P1);
                    }
                }
            }
        }
        public void GenerateCarsOnRestLanes()
        {
            UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
            var vertlanes = GeneratePerpModuleLanes(DisVertCarLengthBackBack + DisLaneWidth / 2, DisVertCarWidth, false, null, true);
            SortLaneByDirection(vertlanes, LayoutMode);
            var align_backback_for_align_rest = false;
            for (int i = 0; i < vertlanes.Count; i++)
            {
                var k = vertlanes[i];
                var vl = k.Line;
                UnifyLaneDirection(ref vl, IniLanes);
                var line = new LineSegment(vl);
                line = line.Translation(k.Vec.Normalize() * DisLaneWidth / 2);
                var line_align_backback_rest = new LineSegment();
                GenerateCarsAndPillarsForEachLane(line, k.Vec, DisVertCarWidth, DisVertCarLength
                    , ref line_align_backback_rest, true, false, false, false,true, true, true, align_backback_for_align_rest, false, true, false,true, false, true);
                align_backback_for_align_rest = false;
                if (line_align_backback_rest.Length > 0)
                {
                    vertlanes.Insert(i + 1, new Lane(line_align_backback_rest, k.Vec));
                    align_backback_for_align_rest = true;
                }
            }

            UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
            vertlanes = GeneratePerpModuleLanes(DisParallelCarWidth + DisLaneWidth / 2, DisParallelCarLength, false);
            SortLaneByDirection(vertlanes, LayoutMode);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                UnifyLaneDirection(ref vl, IniLanes);
                var line = new LineSegment(vl);
                line = line.Translation(k.Vec.Normalize() * DisLaneWidth / 2);
                var line_align_backback_rest = new LineSegment();
                GenerateCarsAndPillarsForEachLane(line, k.Vec, DisParallelCarLength, DisParallelCarWidth
                    ,ref line_align_backback_rest, true, false, false, false, true, true, false);
            }
        }
        public void PostProcess()
        {
            RemoveDuplicateCars();
            RemoveCarsIntersectedWithBoundary();
            if (AccurateCalculate && !QuickCalculate)
            {
                PostProcessPillars();
                ReDefinePillarDimensions();
            }
            InsuredForTheCaseOfoncaveBoundary();
            if (!DisplayFinal && !QuickCalculate)
                ClassifyLanesForLayoutFurther();
        }
    }
}
