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
        public class CompactedLaneGroup
        {
            public List<CompactedLane> CompactedLanes = new List<CompactedLane>();
            public bool IsReversedVector = false;//车道线方向朝外，没有朝内的车道线
            public Vector2D Vector { get; set; }//朝内的移动方向
        }
        public class CompactedLane
        {
            public CompactedLane()
            {

            }
            public CompactedLane(Lane lane, double dis = 0)
            {
                Lane = lane;
                MoveableDistance = dis;
            }
            public Lane Lane { get; set; }
            public double MoveableDistance = 0;
            public bool IsRestrictLane = false;//有背靠背，移动依赖于子车道
        }
        private void CompactLane()
        {
            var newlanes = new List<Lane>();
            var eldlanes=new List<Lane>();
            var compacted = false;
            RemoveDuplicatedLanes(IniLanes);
            CompactForGenerateLanes(IniLanes,OutBoundary,Boundary,ObstaclesSpatialIndex,CarSpatialIndex,Cars,InitialLanes,MaxLength,
                ref newlanes,ref eldlanes,ref compacted);
            if (compacted)
            {
                Compact(newlanes,eldlanes);
            }
        }
        public static List<CompactedLaneGroup> CompactForGenerateLanesPro(List<Lane> IniLanes, Polygon OutBoundary, Polygon Boundary,
    MNTSSpatialIndex ObstaclesSpatialIndex, MNTSSpatialIndex CarSpatialIndex, List<InfoCar> Cars, List<Lane> InitialLanes, double maxLength)
        {
            //找出靠边界的车道线:车道方向与边界相交、不予车道相交
            var lanespacialindex = new MNTSSpatialIndex(IniLanes.Select(e => e.Line.ToLineString()));
            RemoveDuplicatedLanes(IniLanes);
            var adjLanes = new List<Lane>();
            foreach (var lane in IniLanes.Where(e => !e.ISCopiedFromCarmodelus))
            {
                var line = lane.Line;
                var segs = new List<LineSegment>();
                DivideCurveByLength(line, line.Length / 10, ref segs);
                var count_non_intersect_lanes = 0;
                var count_intersected_bound = 0;
                foreach (var seg in segs)
                {
                    var rec = PolyFromLines(seg, seg.Translation(lane.Vec.Normalize() * DisBackBackModulus));
                    rec = rec.Scale(ScareFactorForCollisionCheck);
                    if (rec.IntersectPoint(OutBoundary).Count() > 0)
                        count_intersected_bound++;
                    if (lanespacialindex.SelectCrossingGeometry(rec).Count() == 0)
                        count_non_intersect_lanes++;

                }
                if (count_intersected_bound > 5 && count_non_intersect_lanes > 5)
                    adjLanes.Add(lane);
            }
            //找出方向朝内，位置相同的边界线
            var oriLanes = new List<Lane>();
            var oriLanes_reversedVectorIndexes = new List<int>();
            foreach (var lane in adjLanes)
            {
                var found = false;
                foreach (var iniLane in IniLanes.Where(e => !e.ISCopiedFromCarmodelus))
                {
                    if (HasOverlay(lane.Line, iniLane.Line) && IsAdverseVector(lane.Vec, iniLane.Vec))
                    {
                        found = true;
                        oriLanes.Add(iniLane);
                        break;
                    }
                }
                if (!found)
                {
                    //方向朝外的边界线，没有朝内的重叠车道线-如：尽端环通
                    oriLanes.Add(lane);
                    oriLanes_reversedVectorIndexes.Add(oriLanes.Count - 1);
                }
            }
            var compactedLanesgroup = new List<CompactedLaneGroup>();
            for (int i = 0; i < oriLanes.Count; i++)
            {
                var lane = oriLanes[i];
                if (lane.ISCopiedFromCarmodelus) continue;
                CompactedLaneGroup group = new CompactedLaneGroup();
                group.CompactedLanes.Add(new CompactedLane(lane));
                if (oriLanes_reversedVectorIndexes.Contains(i))
                {
                    group.Vector = -lane.Vec;
                    group.IsReversedVector = true;
                }
                else
                    group.Vector = lane.Vec;
                compactedLanesgroup.Add(group);
            }
            //找出关联车道线
            foreach (var group in compactedLanesgroup)
            {
                if (group.IsReversedVector) continue;
                var group_vec = group.Vector;
                var oriLane = group.CompactedLanes[0].Lane;
                var oriLine = oriLane.Line;
                oriLine = oriLine.Scale(ScareFactorForCollisionCheck);
                var rec = PolyFromLines(oriLine, oriLine.Translation(group_vec.Normalize() * maxLength));
                rec = rec.Scale(ScareFactorForCollisionCheck);
                var intersected_lanes = IniLanes.Where(e => !e.ISCopiedFromCarmodelus).Where(e => e.Line.IntersectPoint(rec).Count() > 0);
                intersected_lanes = intersected_lanes.Where(e => e.Vec.Normalize() == group_vec.Normalize());
                intersected_lanes = intersected_lanes.Where(e => Boundary.ClosestPoint(e.Line.MidPoint).Distance(e.Line.MidPoint) > 10);
                //相连在同一根车道上
                intersected_lanes = intersected_lanes.Where(e =>
                {
                    var connected_e = GetConnectedLanes(e, IniLanes);
                    var connected_ori = GetConnectedLanes(oriLane, IniLanes);
                    if (connected_e.Except(connected_ori).Count() == connected_e.Count())
                        return false;
                    else return true;
                });
                intersected_lanes = intersected_lanes.Where(e =>
                {
                    var pl = PolyFromLines(e.Line, new LineSegment(oriLine.ClosestPoint(e.Line.P0), oriLine.ClosestPoint(e.Line.P1)));
                    var pl_c = PolyFromLines(oriLine, new LineSegment(e.Line.ClosestPoint(oriLine.P0), e.Line.ClosestPoint(oriLine.P1)));
                    pl = pl.Area <= pl_c.Area ? pl : pl_c;
                    pl = pl.Scale(ScareFactorForCollisionCheck);
                    var crossed = ObstaclesSpatialIndex.SelectCrossingGeometry(pl).Count() > 0;
                    if (crossed) return false;
                    else return true;
                });

                group.CompactedLanes.AddRange(intersected_lanes.Select(e => new CompactedLane(e))
                    .OrderBy(e => e.Lane.Line.ClosestPoint(oriLane.Line.MidPoint, true).Distance(oriLane.Line.MidPoint)));
            }
            //计算每根车道线可移动距离
            CarSpatialIndex = new MNTSSpatialIndex(Cars.Select(e => e.Polyline));
            for (int i = 0; i < compactedLanesgroup.Count; i++)
            {
                double moveableDist = 0;
                var group = compactedLanesgroup[i];
                foreach (var lane in group.CompactedLanes)
                {
                    var isRestrictLane = false;
                    var dist = CalculateMoveableDistance(lane, group.Vector, ref isRestrictLane, Cars, InitialLanes, CarSpatialIndex, ObstaclesSpatialIndex,
                        IniLanes, Boundary, maxLength);
                    lane.MoveableDistance = dist;
                    lane.IsRestrictLane = isRestrictLane;
                }
                AdjustMoveableDistanceBySonLane(ref group.CompactedLanes, ref moveableDist);
                if (moveableDist == 0)
                {
                    compactedLanesgroup.RemoveAt(i);
                    i--;
                }
            }
            return compactedLanesgroup;
        }
        public static void CompactForGenerateLanes(List<Lane> IniLanes,Polygon OutBoundary,Polygon Boundary,
            MNTSSpatialIndex ObstaclesSpatialIndex, MNTSSpatialIndex CarSpatialIndex,List<InfoCar> Cars,List<Lane> InitialLanes,double maxLength,
            ref List<Lane> newlanes, ref List<Lane> eldlanes,ref bool compacted)
        {
            //找出靠边界的车道线:车道方向与边界相交、不予车道相交
            var lanespacialindex = new MNTSSpatialIndex(IniLanes.Select(e => e.Line.ToLineString()));
            RemoveDuplicatedLanes(IniLanes);
            var adjLanes = new List<Lane>();
            foreach (var lane in IniLanes.Where(e => !e.ISCopiedFromCarmodelus))
            {
                var line = lane.Line;
                var segs = new List<LineSegment>();
                DivideCurveByLength(line, line.Length / 10, ref segs);
                var count_non_intersect_lanes = 0;
                var count_intersected_bound = 0;
                foreach (var seg in segs)
                {
                    var rec = PolyFromLines(seg, seg.Translation(lane.Vec.Normalize() * DisBackBackModulus));
                    rec = rec.Scale(ScareFactorForCollisionCheck);
                    if (rec.IntersectPoint(OutBoundary).Count() > 0)
                        count_intersected_bound++;
                    if (lanespacialindex.SelectCrossingGeometry(rec).Count() == 0)
                        count_non_intersect_lanes++;

                }
                if (count_intersected_bound > 5 && count_non_intersect_lanes > 5)
                    adjLanes.Add(lane);
            }
            //找出方向朝内，位置相同的边界线
            var oriLanes = new List<Lane>();
            var oriLanes_reversedVectorIndexes = new List<int>();
            foreach (var lane in adjLanes)
            {
                var found = false;
                foreach (var iniLane in IniLanes.Where(e => !e.ISCopiedFromCarmodelus))
                {
                    if (HasOverlay(lane.Line, iniLane.Line) && IsAdverseVector(lane.Vec, iniLane.Vec))
                    {
                        found = true;
                        oriLanes.Add(iniLane);
                        break;
                    }
                }
                if (!found)
                {
                    //方向朝外的边界线，没有朝内的重叠车道线-如：尽端环通
                    oriLanes.Add(lane);
                    oriLanes_reversedVectorIndexes.Add(oriLanes.Count - 1);
                }
            }
            var compactedLanesgroup = new List<CompactedLaneGroup>();
            for (int i = 0; i < oriLanes.Count; i++)
            {
                var lane = oriLanes[i];
                if (lane.ISCopiedFromCarmodelus) continue;
                CompactedLaneGroup group = new CompactedLaneGroup();
                group.CompactedLanes.Add(new CompactedLane(lane));
                if (oriLanes_reversedVectorIndexes.Contains(i))
                {
                    group.Vector = -lane.Vec;
                    group.IsReversedVector = true;
                }
                else
                    group.Vector = lane.Vec;
                compactedLanesgroup.Add(group);
            }
            //找出关联车道线
            foreach (var group in compactedLanesgroup)
            {
                if (group.IsReversedVector) continue;
                var group_vec = group.Vector;
                var oriLane = group.CompactedLanes[0].Lane;
                var oriLine = oriLane.Line;
                oriLine = oriLine.Scale(ScareFactorForCollisionCheck);
                var rec = PolyFromLines(oriLine, oriLine.Translation(group_vec.Normalize() * maxLength));
                rec = rec.Scale(ScareFactorForCollisionCheck);
                var intersected_lanes = IniLanes.Where(e => !e.ISCopiedFromCarmodelus).Where(e => e.Line.IntersectPoint(rec).Count() > 0);
                intersected_lanes = intersected_lanes.Where(e => e.Vec.Normalize() == group_vec.Normalize());
                intersected_lanes = intersected_lanes.Where(e => Boundary.ClosestPoint(e.Line.MidPoint).Distance(e.Line.MidPoint) > 10);
                //相连在同一根车道上
                intersected_lanes = intersected_lanes.Where(e =>
                {
                    var connected_e = GetConnectedLanes(e,IniLanes);
                    var connected_ori = GetConnectedLanes(oriLane,IniLanes);
                    if (connected_e.Except(connected_ori).Count() == connected_e.Count())
                        return false;
                    else return true;
                });
                intersected_lanes = intersected_lanes.Where(e =>
                {
                    var pl = PolyFromLines(e.Line, new LineSegment(oriLine.ClosestPoint(e.Line.P0), oriLine.ClosestPoint(e.Line.P1)));
                    var pl_c = PolyFromLines(oriLine, new LineSegment(e.Line.ClosestPoint(oriLine.P0), e.Line.ClosestPoint(oriLine.P1)));
                    pl = pl.Area <= pl_c.Area ? pl : pl_c;
                    pl = pl.Scale(ScareFactorForCollisionCheck);
                    var crossed = ObstaclesSpatialIndex.SelectCrossingGeometry(pl).Count() > 0;
                    if (crossed) return false;
                    else return true;
                });

                group.CompactedLanes.AddRange(intersected_lanes.Select(e => new CompactedLane(e))
                    .OrderBy(e => e.Lane.Line.ClosestPoint(oriLane.Line.MidPoint, true).Distance(oriLane.Line.MidPoint)));
            }
            //计算每根车道线可移动距离
            CarSpatialIndex = new MNTSSpatialIndex(Cars.Select(e => e.Polyline));
            for (int i = 0; i < compactedLanesgroup.Count; i++)
            {
                double moveableDist = 0;
                var group = compactedLanesgroup[i];
                foreach (var lane in group.CompactedLanes)
                {
                    var isRestrictLane = false;
                    var dist = CalculateMoveableDistance(lane, group.Vector, ref isRestrictLane, Cars, InitialLanes, CarSpatialIndex, ObstaclesSpatialIndex,
                        IniLanes, Boundary, maxLength);
                    lane.MoveableDistance = dist;
                    lane.IsRestrictLane = isRestrictLane;
                }
                AdjustMoveableDistanceBySonLane(ref group.CompactedLanes, ref moveableDist);
                if (moveableDist == 0)
                {
                    compactedLanesgroup.RemoveAt(i);
                    i--;
                }
            }
            //移动目标车道线
            var addlanes = new List<Lane>();
            eldlanes = new List<Lane>();
            compactedLanesgroup = compactedLanesgroup.Where(e => e.CompactedLanes[0].MoveableDistance > 0).ToList();
            foreach (var group in compactedLanesgroup)
            {
                foreach (var lane in group.CompactedLanes)
                {
                    if (group.IsReversedVector)
                        lane.Lane.NotCopyReverseForLaneCompaction = true;
                    eldlanes.Add(lane.Lane.Clone());
                    lane.Lane.Line = lane.Lane.Line.Translation(group.Vector.Normalize() * lane.MoveableDistance);
                    addlanes.Add(lane.Lane);
                }
            }
            //判断每根车道是否连接边界，若不是，找出连接边界的父车道
            var _addlanes = new List<Lane>();
            var _add_eldlanes = new List<Lane>();
            for (int i = 0; i < addlanes.Count; i++)
            {
                var lane = addlanes[i];
                if (!IsConnectedToLane(lane.Line, true, IniLanes))
                    lane.Line = new LineSegment(lane.Line.P1, lane.Line.P0);
                //与障碍物做相交判断处理
                var buffer = lane.Line.Buffer(DisLaneWidth / 2);
                var buffer_sc = buffer.Scale(ScareFactorForCollisionCheck);
                var obs_crossed = ObstaclesSpatialIndex.SelectCrossingGeometry(buffer_sc);
                var points = new List<Coordinate>();
                foreach (var obs in obs_crossed)
                {
                    points.AddRange(obs.Coordinates);
                    points.AddRange(obs.IntersectPoint(buffer_sc));
                }
                points = points.Where(p => buffer.Contains(p)).Select(p => lane.Line.ClosestPoint(p)).ToList();
                var splits = SplitLine(lane.Line, points);
                if (IsConnectedToLaneDouble(lane.Line,IniLanes) && splits.Count > 1)
                {
                    var adlane = new Lane(splits.Last(), lane.Vec);
                    adlane.Copy(lane);
                    addlanes.Add(adlane);
                    eldlanes.Add(lane.Clone());
                }
                addlanes[i].Line = splits.First();
                lane = addlanes[i];
                //判断是否直接连接到边界上
                if (!IsConnectToBoundaryLanes(lane,Boundary, IniLanes))
                {
                    GetParentLanesConnectToBoundaryCount = 0;
                    var parentLanes = GetParentLanesConnectToBoundary(lane,Boundary,IniLanes);
                    _addlanes.AddRange(parentLanes);
                }
            }
            addlanes.AddRange(_addlanes);
            eldlanes.AddRange(_addlanes);
            _addlanes.Clear();
            foreach (var lane in addlanes)
            {
                if (lane.NotCopyReverseForLaneCompaction)
                    continue;
                if (lane.IsGeneratedForLoopThrough) continue;
                var reverse_lane = new Lane(lane.Line, -lane.Vec);
                reverse_lane.Copy(lane);
                _addlanes.Add(reverse_lane);
            }
            foreach (var lane in eldlanes)
            {
                if (lane.NotCopyReverseForLaneCompaction)
                    continue;
                if (lane.IsGeneratedForLoopThrough) continue;
                var reverse_lane = new Lane(lane.Line, -lane.Vec);
                reverse_lane.Copy(lane);
                _add_eldlanes.Add(reverse_lane);
            }
            addlanes.AddRange(_addlanes);
            eldlanes.AddRange(_add_eldlanes);
            if (addlanes.Count > 0)
            {
                compacted = true;
                newlanes = new List<Lane>();
                newlanes.AddRange(addlanes);
                newlanes.AddRange(InitialLanes);
                eldlanes.AddRange(InitialLanes);

            }
        }
        void Compact(List<Lane > newlanes,List<Lane> eldlanes)
        {
            ClearNecessaryElements();
            Update(newlanes, eldlanes);
            //IniLanes = IniLanes.Where(e => !e.GeneratedForLaneCompactionTestForCaseEndThroughLoop).ToList();
            hasCompactedLane = true;
            GenerateParkingSpaces();
        }

        private static bool IsConnectToBoundaryLanes(Lane lane,Polygon Boundary,List<Lane> IniLanes)
        {
            if (Boundary.ClosestPoint(lane.Line.P0).Distance(lane.Line.P0) < 1 && IsConnectedToLane(lane.Line,true,IniLanes))
                return true;
            else if (Boundary.ClosestPoint(lane.Line.P1).Distance(lane.Line.P1) < 1 && IsConnectedToLane(lane.Line, false, IniLanes))
                return true;
            return false;
        }

        private static int GetParentLanesConnectToBoundaryCount = 0;
        private static List<Lane> GetParentLanesConnectToBoundary(Lane lane,Polygon Boundary,List<Lane> IniLanes)
        {
            var result = new List<Lane>();
            GetParentLanesConnectToBoundaryCount++;
            if (GetParentLanesConnectToBoundaryCount > 50)
                return result;
            if (IsConnectToBoundaryLanes(lane,Boundary, IniLanes))
                return new List<Lane> { new Lane(new LineSegment(),Vector2D.Zero) };
            else
            {
                var lanesLists = new List<List<Lane>>();
                var connecteds = GetConnectedLanes(lane, IniLanes);
                foreach (var connectedLane in connecteds)
                {
                    lanesLists.Add(new List<Lane>());
                    lanesLists[lanesLists.Count - 1].Add(connectedLane);
                    lanesLists[lanesLists.Count - 1].AddRange(GetParentLanesConnectToBoundary(connectedLane,Boundary,IniLanes));
                }
                lanesLists = lanesLists.Where(e => e.Count > 0 && e[e.Count - 1].Line.Length == 0 && e[e.Count - 1].Vec == Vector2D.Zero).OrderBy(e => e.Count).ToList();
                if (lanesLists.Count > 0)
                    return lanesLists.First();
                else return result;
            }
        }
        private static List<Lane> GetConnectedLanes(Lane lane,List<Lane> IniLanes)
        {
            var lanes = IniLanes.Where(e => !HasOverlay(e.Line, lane.Line))
                .Where(e => e.Line.ClosestPoint(lane.Line.P0).Distance(lane.Line.P0) < 1
                || e.Line.ClosestPoint(lane.Line.P1).Distance(lane.Line.P1) < 1).ToList();
            lanes = IniLanes.Where(e => !HasOverlay(e.Line, lane.Line)).ToList();
            lanes = lanes.Where(e => e.Line.ClosestPoint(lane.Line.P0).Distance(lane.Line.P0) < 10
                 || e.Line.ClosestPoint(lane.Line.P1).Distance(lane.Line.P1) < 10).ToList();
            return lanes;
        }
        private static double CalculateMoveableDistance(CompactedLane lane, Vector2D vec, ref bool isRestrictLane,
            List<InfoCar> Cars,List<Lane> InitialLanes,MNTSSpatialIndex CarSpatialIndex, MNTSSpatialIndex ObstaclesSpatialIndex,
            List<Lane>IniLanes,Polygon Boundary,double maxLength)
        {
            var cars = Cars.Select(e => e.Polyline).ToList();
            int nonCal = 0;
            //判断是否本身在边界上
            foreach (var initialLane in InitialLanes)
            {
                if (HasOverlay(initialLane.Line, lane.Lane.Line))
                {
                    return 0;
                }
            }
            //判断是否有背靠背车位
            var line = lane.Lane.Line;
            var rec = PolyFromLines(line, line.Translation(vec.Normalize() * DisBackBackModulus / 2));
            rec = rec.Scale(ScareFactorForCollisionCheck);
            var intersected_cars = CarSpatialIndex.SelectCrossingGeometry(rec).Cast<Polygon>().ToList();
            var intersected_car_infos = intersected_cars.Select(e => Cars[cars.IndexOf(e)]).ToList();
            var intersected_back_car_infos = intersected_car_infos.Where(e =>
            {
                var cond = e.Vector.Normalize() == vec.Normalize();
                cond = cond && e.CarLayoutMode == ((int)CarLayoutMode.VERTBACKBACK);
                return cond;
            });
            if (intersected_back_car_infos.Any() && vec.Normalize() == lane.Lane.Vec.Normalize())
                isRestrictLane = true;

            double factor = (double)2 / 3;
            var dis_vert_obstacle = double.PositiveInfinity;
            var dis_lane_obstacle = double.PositiveInfinity;
            #region 2/3车位碰障碍物的最大移动距离
            //车位
            var intersected_vert_car_infos = intersected_car_infos.Where(e =>
            {
                var cond = e.Vector.Normalize() == vec.Normalize();
                cond = cond && e.CarLayoutMode == ((int)CarLayoutMode.VERT);
                return cond;
            }).ToList();
            var intersected_vert_cars = intersected_vert_car_infos.Select(e => e.Polyline).ToList();
            rec = PolyFromLines(line, line.Translation(vec.Normalize() * maxLength));
            rec = rec.Scale(ScareFactorForCollisionCheck);
            var crossed_obs = ObstaclesSpatialIndex.SelectCrossingGeometry(rec).Cast<Polygon>().Where(o => Boundary.Contains(o.Centroid.Coordinate));
            var coord_obs = new List<Coordinate>();
            foreach (var coos in crossed_obs.Select(e => e.Coordinates))
                coord_obs.AddRange(coos);
            coord_obs = coord_obs.OrderBy(p => line.ClosestPoint(p).Distance(p)).ToList();
            var dists = coord_obs.Select(p => line.ClosestPoint(p).Distance(p) - DisCarAndHalfLane).Where(e => e > 0).ToList();
            dists = dists.Distinct().ToList();
            for (int i = 0; i < dists.Count; i++)
            {
                var dist = dists[i];
                var test_vert_cars = intersected_vert_cars.Select(e => e.Translation(vec.Normalize() * dist).Scale(ScareFactorForCollisionCheck));
                int intersected_count = 0;
                foreach (var pl in test_vert_cars)
                {
                    if (ObstaclesSpatialIndex.SelectCrossingGeometry(pl).Count() > 0)
                        intersected_count++;
                }
                if (intersected_count > test_vert_cars.Count() * factor)
                {
                    dis_vert_obstacle = dists[i];
                    break;
                }
            }
            //车道
            var offset_line_buffer = PolyFromLines(line, line.Translation(vec.Normalize() * maxLength));
            offset_line_buffer = offset_line_buffer.Scale(ScareFactorForCollisionCheck);
            var crossed_lane_obs = ObstaclesSpatialIndex.SelectCrossingGeometry(offset_line_buffer).Where(o => Boundary.Contains(o.Centroid.Coordinate));
            if (crossed_lane_obs.Count() > 0)
            {
                var linetest = new LineSegment(line);
                if (!IsConnectedToLaneDouble(line,IniLanes) && IsConnectedToLane(line, IniLanes))
                    linetest = LineSegmentSDL(line.P0, Vector(line).Normalize(), line.Length * factor);
                else if (!IsConnectedToLaneDouble(line,IniLanes) && IsConnectedToLane(line, false,IniLanes))
                    linetest = LineSegmentSDL(line.P1, -Vector(line).Normalize(), line.Length * factor);
                offset_line_buffer = PolyFromLines(linetest, linetest.Translation(vec.Normalize() * maxLength));
                crossed_lane_obs = ObstaclesSpatialIndex.SelectCrossingGeometry(offset_line_buffer);
                var points_in_offset_line_buffer = new List<Coordinate>();
                foreach (var obs in crossed_lane_obs)
                {
                    points_in_offset_line_buffer.AddRange(obs.Coordinates);
                    points_in_offset_line_buffer.AddRange(obs.IntersectPoint(offset_line_buffer));
                }
                points_in_offset_line_buffer = points_in_offset_line_buffer.Where(p => offset_line_buffer.Contains(p) || offset_line_buffer.ClosestPoint(p).Distance(p) < 1).ToList();
                points_in_offset_line_buffer = points_in_offset_line_buffer.OrderBy(p => linetest.ClosestPoint(p).Distance(p)).ToList();
                if (points_in_offset_line_buffer.Count > 0)
                {
                    var point_in_offset_line_buffer = points_in_offset_line_buffer.First();
                    var dist = line.ClosestPoint(point_in_offset_line_buffer).Distance(point_in_offset_line_buffer);
                    dist = dist - DisLaneWidth / 2;
                    dis_lane_obstacle = dist > 0 ? dist : double.NegativeInfinity;
                }
            }
            #endregion

            var dis_lane_modules = double.PositiveInfinity;
            #region 碰15700背靠背车道的距离
            if (!isRestrictLane)
            {
                rec = PolyFromLines(line, line.Translation(vec.Normalize() * maxLength));
                rec = rec.Scale(ScareFactorForCollisionCheck);
                var intersected_lanes = IniLanes.Where(e => e.Line.ToLineString().IntersectPoint(rec).Count() > 0
                  || rec.Contains(e.Line.MidPoint)).ToList();
                intersected_lanes = intersected_lanes.Where(e => e.Vec.Normalize() == vec.Normalize() || e.Vec.Normalize() == -vec.Normalize()).ToList();
                intersected_lanes = intersected_lanes.OrderBy(e => line.ClosestPoint(e.Line.MidPoint).Distance(e.Line.MidPoint)).ToList();
                if (intersected_lanes.Count > 0)
                {
                    var intersected_lane = intersected_lanes.First();
                    var dist = line.ClosestPoint(intersected_lane.Line.MidPoint, true).Distance(intersected_lane.Line.MidPoint);
                    dis_lane_modules = dist - DisBackBackModulus >= 0 ? dist - DisBackBackModulus : dis_lane_modules;
                }
            }
            else
            {
                rec = PolyFromLines(line, line.Translation(vec.Normalize() * maxLength));
                rec = rec.Scale(ScareFactorForCollisionCheck);
                var intersected_lanes = IniLanes.Where(e => e.Line.ToLineString().IntersectPoint(rec).Count() > 0
                  || rec.Contains(e.Line.MidPoint)).ToList();
                intersected_lanes = intersected_lanes.Where(e => e.Vec.Normalize() == vec.Normalize() || e.Vec.Normalize() == -vec.Normalize()).ToList();
                intersected_lanes = intersected_lanes.OrderBy(e => line.ClosestPoint(e.Line.MidPoint).Distance(e.Line.MidPoint)).ToList();
                if (intersected_lanes.Count > 0)
                {
                    var intersected_lane = intersected_lanes.First();
                    if (Boundary.ClosestPoint(intersected_lane.Line.MidPoint).Distance(intersected_lane.Line.MidPoint) < 1)
                        return 0;
                }
            }
            #endregion

            var dis_parallel_car = double.PositiveInfinity;
            #region 碰平行式车位

            //rec = PolyFromLines(line.Scale(0.8), line.Scale(0.8).Translation(lane.Lane.Vec.Normalize() * DisBackBackModulus / 2));
            //rec = rec.Scale(ScareFactorForCollisionCheck);
            //intersected_cars = CarSpatialIndex.SelectCrossingGeometry(rec).Cast<Polygon>().ToList();
            //intersected_car_infos = intersected_cars.Select(e => Cars[cars.IndexOf(e)]).ToList();
            var intersected_parallel_car_infos = intersected_car_infos.Where(e =>
            {
                //var cond = e.CarLayoutMode == ((int)CarLayoutMode.PARALLEL);
                //var pt = e.Polyline.Coordinates.OrderBy(p => line.ClosestPoint(p).Distance(p)).First();
                //cond = cond || line.ClosestPoint(pt).Distance(pt) - 1 > DisLaneWidth / 2;
                var edge = e.Polyline.GetEdges().OrderByDescending(x => x.Length).First();
                var cond = !IsPerpLine(edge, line);
                return cond;
            });
            var intersected_parallel_cars = intersected_parallel_car_infos.Select(e => e.Polyline).ToList();
            var intersected_parallel_car_points = new List<Coordinate>();
            foreach (var car in intersected_parallel_cars)
                intersected_parallel_car_points.AddRange(car.Coordinates);
            intersected_parallel_car_points = intersected_parallel_car_points.OrderBy(p => line.ClosestPoint(p).Distance(p)).ToList();
            if (intersected_parallel_car_points.Count > 0)
            {
                var intersected_parallel_car_point = intersected_parallel_car_points.First();
                var dist = line.ClosestPoint(intersected_parallel_car_point).Distance(intersected_parallel_car_point);
                dist = dist - DisPillarLength - DisLaneWidth / 2;
                if (Math.Abs(dist) < 0.0001)
                    dist = 0;
                if (dist > 0)
                    dis_parallel_car = dist;
                else
                    dis_parallel_car = double.NegativeInfinity;

                //new Case test
                //if (dist == 0)
                //{
                //    var dir_pl = PolyFromLines(line, line.Translation(lane.Lane.Vec.Normalize() * maxLength));
                //    rec = PolyFromLines(line, line.Translation(vec.Normalize() * maxLength));
                //    rec = rec.Scale(ScareFactorForCollisionCheck);
                //    var intersected_lanes = IniLanes.Where(e => line.ClosestPoint(e.Line.P0).Distance(e.Line.P0) <0.1 
                //    || line.ClosestPoint(e.Line.P1).Distance(e.Line.P1) < 0.1).ToList();
                //    intersected_lanes= intersected_lanes.Where(e => rec.Contains(e.Line.MidPoint)).ToList();
                //    intersected_lanes = intersected_lanes.Where(e => !IsConnectedToLaneDouble(e.Line, IniLanes)).ToList();
                //    for (int j = 0; j < intersected_lanes.Count; j++)
                //    {
                //        if (IsConnectedToLane(intersected_lanes[j].Line, false, IniLanes))
                //        {
                //            intersected_lanes[j].Line = new LineSegment(intersected_lanes[j].Line.P1, intersected_lanes[j].Line.P0);
                //        }
                //    }
                //    var connected_lane_tests = intersected_lanes.Select(e => e.Line).ToList();
                //    var movable_dist = new Dictionary<LineSegment, double>();
                //    var _testcars = CarSpatialIndex.SelectCrossingGeometry(rec).Cast<Polygon>();
                //    foreach (var intersected_lane in connected_lane_tests)
                //    {
                //        var _buffer = intersected_lane.Buffer(3000);
                //        var _crossed_cars = _testcars.Where(e => e.IntersectPoint(_buffer).Count() > 0);
                //        var _crossed_cars_points = new List<Coordinate>();
                //        foreach (var _crossed_car in _crossed_cars)
                //            _crossed_cars_points.AddRange(_crossed_car.Coordinates);
                //        _crossed_cars_points = _crossed_cars_points.Select(p => intersected_lane.ClosestPoint(p)).ToList();
                //        _crossed_cars_points = _crossed_cars_points.OrderBy(p => p.Distance(intersected_lane.P1)).ToList();
                //        if (_crossed_cars_points.Count > 0)
                //        {
                //            var _crossed_cars_point = _crossed_cars_points.First();
                //            var _crossed_cars_point_dist_end = _crossed_cars_point.Distance(intersected_lane.P1);
                //            movable_dist.Add(intersected_lane, _crossed_cars_point_dist_end);
                //        }
                //    }
                //    var min_movable_dist= movable_dist.Values.Min();
                //    if (min_movable_dist > 0)
                //    {
                //        dis_parallel_car = min_movable_dist;
                //    }
                //}
            }
            #endregion

            double res = 0;
            var min = new double[] { dis_vert_obstacle, dis_lane_obstacle, dis_lane_modules, dis_parallel_car }.Min();
            if (min != double.PositiveInfinity && min != double.NegativeInfinity)
                res = min;
            else if (min == double.PositiveInfinity)
            {
                //var testLine = line.Translation(vec.Normalize() * DisBackBackModulus);
                //if (Boundary.Contains(testLine.MidPoint))
                //    res = DisBackBackModulus;
                //else
                //    res = 0;
                res = 0;
            }
            if (res > 0)
            {
                line = line;
            }
            return res;
        }

        private static void AdjustMoveableDistanceBySonLane(ref List<CompactedLane> compactedLanes,ref double moveableDist)
        {
            moveableDist = 0;
            if (compactedLanes.Count > 0)
                moveableDist = compactedLanes[0].MoveableDistance;
            if (compactedLanes.Count > 1)
            {
                for (int i = compactedLanes.Count - 2; i > -1; i--)
                {
                    if (compactedLanes[i].IsRestrictLane)
                    {
                        if (compactedLanes[i].MoveableDistance > compactedLanes[i + 1].MoveableDistance)
                        {
                            compactedLanes[i].MoveableDistance = compactedLanes[i + 1].MoveableDistance;
                            moveableDist= compactedLanes[i + 1].MoveableDistance;
                        }
                    }
                }
            }
        }
    }
}
