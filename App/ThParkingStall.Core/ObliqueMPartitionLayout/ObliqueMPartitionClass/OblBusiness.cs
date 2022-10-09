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
        public void GenerateCarsOnRestLanes()
        {
            CarSpatialIndex = new MNTSSpatialIndex(Cars.Select(e => e.Polyline));
            UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
            var vertlanes = GeneratePerpModuleLanes(DisVertCarLengthBackBack + DisLaneWidth / 2, DisVertCarWidth, false, null, true);
            SortLaneByDirection(vertlanes, LayoutMode, Vector2D.Zero);
            var align_backback_for_align_rest = false;
            for (int i = 0; i < vertlanes.Count; i++)
            {
                var k = vertlanes[i];
                var vl = k.Line;
                UnifyLaneDirection(ref vl, IniLanes);
                var line = new LineSegment(vl);
                line = TranslateReservedConnection(line, k.Vec.Normalize() * DisLaneWidth / 2, false);
                var line_align_backback_rest = new LineSegment();
                GenerateCarsAndPillarsForEachLane(line, k.Vec, DisVertCarWidth, DisVertCarLength
                    , ref line_align_backback_rest, true, false, false, false, true, true, true, align_backback_for_align_rest, false, true, false, true, false, true);
                align_backback_for_align_rest = false;
                if (line_align_backback_rest.Length > 0)
                {
                    vertlanes.Insert(i + 1, new Lane(line_align_backback_rest, k.Vec));
                    align_backback_for_align_rest = true;
                }
            }

            UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
            vertlanes = GeneratePerpModuleLanes(DisParallelCarWidth + DisLaneWidth / 2, DisParallelCarLength, false);
            SortLaneByDirection(vertlanes, LayoutMode, Vector2D.Zero);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                UnifyLaneDirection(ref vl, IniLanes);
                var line = new LineSegment(vl);
                line = TranslateReservedConnection(line, k.Vec.Normalize() * DisLaneWidth / 2, false);
                var line_align_backback_rest = new LineSegment();
                GenerateCarsAndPillarsForEachLane(line, k.Vec, DisParallelCarLength, DisParallelCarWidth
                    , ref line_align_backback_rest, true, false, false, false, true, true, false);
            }
        }
        public void ProcessLanes(ref List<Lane> Lanes, bool preprocess = false)
        {

        }
        public void PostProcess()
        {
            RemoveDuplicateCars();
            RemoveCarsIntersectedWithBoundary();
            InsuredForTheCaseOfoncaveBoundary();
            if (AccurateCalculate && !QuickCalculate)
            {
                PostProcessPillars();
                ReDefinePillarDimensions();
            }
            if (!QuickCalculate)
            {
                ClassifyLanesForLayoutFurther();
            }
        }
        private void ClassifyLanesForLayoutFurther()
        {
            ProcessLanes(ref IniLanes);
            OutEnsuredLanes.AddRange(OriginalLanes);

            var lanes = IniLanes.Select(e => e).ToList();
            var found = false;
            while (true)
            {
                found = false;
                //拿双边连接的车道线
                bool found_connected_double = false;
                while (true)
                {
                    found_connected_double = false;
                    for (int i = 0; i < lanes.Count; i++)
                    {
                        //筛重合
                        var overlap = false;
                        foreach (var lane in OutEnsuredLanes)
                        {
                            var cond_a = lane.ClosestPoint(lanes[i].Line.P0, false).Distance(lanes[i].Line.P0) < 1;
                            var cond_b = lane.ClosestPoint(lanes[i].Line.P1, false).Distance(lanes[i].Line.P1) < 1;
                            if (cond_a && cond_b)
                            {
                                overlap = true;
                                break;
                            }
                        }
                        if (overlap) continue;
                        if (!IsConnectedToLaneDouble(lanes[i].Line,IniLanes)) continue;
                        if (!IsConnectedToLane(lanes[i].Line, true, OutEnsuredLanes)) continue;
                        if (!IsConnectedToLane(lanes[i].Line, false, OutEnsuredLanes)) continue;
                        found_connected_double = true;
                        found = true;
                        OutEnsuredLanes.Add(lanes[i].Line);
                        lanes.RemoveAt(i);
                        break;
                    }
                    if (!found_connected_double) break;
                }
                //拿一端连接墙的车道线
                for (int i = 0; i < lanes.Count; i++)
                {
                    //筛重合
                    var overlap = false;
                    foreach (var lane in OutEnsuredLanes)
                    {
                        var cond_a = lane.ClosestPoint(lanes[i].Line.P0, false).Distance(lanes[i].Line.P0) < 1;
                        var cond_b = lane.ClosestPoint(lanes[i].Line.P1, false).Distance(lanes[i].Line.P1) < 1;
                        if (cond_a && cond_b)
                        {
                            overlap = true;
                            break;
                        }
                    }
                    if (overlap) continue;
                    if (IsConnectedToLane(lanes[i].Line, false, OutEnsuredLanes))
                        lanes[i].Line = new LineSegment(lanes[i].Line.P1, lanes[i].Line.P0);
                    if (!IsConnectedToLane(lanes[i].Line, true, OutEnsuredLanes)) continue;
                    foreach (var wall in Walls)
                    {
                        if (wall.ClosestPoint(lanes[i].Line.P1).Distance(lanes[i].Line.P1) < 1)
                        {
                            OutEnsuredLanes.Add(lanes[i].Line);
                            found = true;
                            lanes.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                    if (!found)
                    {
                        if (OutBoundary.ClosestPoint(lanes[i].Line.P1).Distance(lanes[i].Line.P1) < 1 || !OutBoundary.Contains(lanes[i].Line.P1))
                        {
                            OutEnsuredLanes.Add(lanes[i].Line);
                            found = true;
                            lanes.RemoveAt(i);
                            i--;
                        }
                    }

                }
                if (!found) break;
            }
            OriginalLanes.ForEach(e => OutEnsuredLanes.Remove(e));
        }

    }
}
