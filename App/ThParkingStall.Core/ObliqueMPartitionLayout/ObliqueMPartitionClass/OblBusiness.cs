using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.LaneDeformation;
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
            if (!QuickCalculate && AllowLaneDeformation)
            {
                ConstructConversionDatas();
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

        void ConstructConversionDatas()
        {
            var pureLaneForConversionDatas = ConstructPureLaneForConversionDatas();
            VehicleLanes.AddRange(pureLaneForConversionDatas.Select(e => new VehicleLane(e.Line, e.Line.Buffer(DisLaneWidth / 2),e.Vec.Normalize())));
            foreach (var parkingPlaceBlock in ParkingPlaceBlocks)
            {
                VehicleLanes= VehicleLanes.OrderBy(e => e.CenterLine.MidPoint.Distance(parkingPlaceBlock.SourceLane.MidPoint)).ToList();
                foreach (var vehicle in VehicleLanes)
                {
                    if ((vehicle.Vec - parkingPlaceBlock.BlockDir).Length() > 0.001) continue;
                    if(BelongToLine(parkingPlaceBlock.SourceLane,vehicle.CenterLine))
                    {
                        vehicle.ParkingPlaceBlockList.Add(parkingPlaceBlock);
                        break;
                    }
                }
            }
        }

        List<PureLaneForConversionDatas> ConstructPureLaneForConversionDatas()
        {
            var pureLaneForConversionDatas = new List<PureLaneForConversionDatas>();
            foreach (var lane in IniLanes)
                pureLaneForConversionDatas.Add(new PureLaneForConversionDatas(lane.Line, lane.Vec.Normalize()));
            if (pureLaneForConversionDatas.Count > 1)
            {
                for (int i = 0; i < pureLaneForConversionDatas.Count - 1; i++)
                {
                    for (int j = i + 1; j < pureLaneForConversionDatas.Count; j++)
                    {
                        if ((pureLaneForConversionDatas[i].Vec - pureLaneForConversionDatas[j].Vec).Length() > 0.001) continue;
                        if (HasPartialOverlay(pureLaneForConversionDatas[i].Line, pureLaneForConversionDatas[j].Line))
                        {
                            pureLaneForConversionDatas[i].Line = MergePartialOverlayLine(pureLaneForConversionDatas[i].Line, pureLaneForConversionDatas[j].Line);
                            pureLaneForConversionDatas.RemoveAt(j);
                            j--;
                        }
                    }
                }
            }
            pureLaneForConversionDatas = pureLaneForConversionDatas.Where(e => e.Line.Length > 100).ToList();
            return pureLaneForConversionDatas;
        }
        bool BelongToLine(LineSegment son, LineSegment parent)
        {
            double tol = 0.001;
            if (parent.ClosestPoint(son.P0).Distance(son.P0) < tol && parent.ClosestPoint(son.P1).Distance(son.P1) < tol)
                return true;
            return false;
        }
        class PureLaneForConversionDatas
        {
            public PureLaneForConversionDatas(LineSegment line, Vector2D vec)
            {
                Line=line;
                Vec = vec;
            }
            public LineSegment Line;
            public Vector2D Vec;
        }
        public static bool HasPartialOverlay(LineSegment a, LineSegment b)
        {
            double tol = 0.001;
            if (Vector(a).Dot(Vector(b).GetPerpendicularVector()) >0.001) return false;
            if (a.ClosestPoint(b.P0).Distance(b.P0) < tol || a.ClosestPoint(b.P1).Distance(b.P1) < tol) return true;
            if (b.ClosestPoint(a.P0).Distance(a.P0) < tol || b.ClosestPoint(a.P1).Distance(a.P1) < tol) return true;
            return false;
        }
        public static LineSegment MergePartialOverlayLine(LineSegment a, LineSegment b)
        {
            var coords = new List<Coordinate>() { a.P0, a.P1, b.P0, b.P1 };
            var max = double.NegativeInfinity;
            Coordinate ps = new Coordinate(), pe = new Coordinate();
            for (int i = 0; i < coords.Count - 1; i++)
            {
                for (int j = i + 1; j < coords.Count; j++)
                {
                    var dist = coords[i].Distance(coords[j]);
                    if (dist > max)
                    {
                        max = dist;
                        ps = coords[i];
                        pe = coords[j];
                    }
                }
            }
            return new LineSegment(ps, pe);
        }
    }
}
