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
using ThParkingStall.Core.OInterProcess;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.ObliqueMPartitionLayout.OPostProcess
{
    public partial class OPostProcessEntry
    {
        public void OGenerateCarsOntheEndofLanesByRemoveUnnecessaryLanes(ref List<InfoCar> cars, ref List<Polygon> pillars, ref List<LineSegment> lanes
    , List<LineString> Walls, MNTSSpatialIndex obspacialindex, Polygon boundary)
        {
            ORemoveDuplicatedAndInvalidLanes(ref lanes);
            var carspacialindex = new MNTSSpatialIndex(cars.Select(e => e.Polyline).ToList());
            bool found = false;
            while (true)
            {
                found = false;
                for (int i = 0; i < lanes.Count; i++)
                {
                    var lane = lanes[i];
                    if (boundary.ClosestPoint(lane.P0).Distance(lane.P0) < 10)
                    {
                        var point = lane.P0;
                        var pointest = point.Translation(Vector(lane).Normalize() * 2000);
                        var vec = Vector(lane).Normalize().GetPerpendicularVector();
                        var carLine = new LineSegment();
                        var align = true;
                        if (OCanAddCarSpots(lane.P0, lane, Vector(lane).Normalize(), pointest, vec, carspacialindex, ref carLine, ref align))
                        {
                            found = true;
                            var split_p = boundary.ClosestPoint(carLine.P0).Distance(carLine.P0) < boundary.ClosestPoint(carLine.P1).Distance(carLine.P1) ?
                                carLine.P1 : carLine.P0;
                            split_p = split_p.Translation(Vector(lane).Normalize() * MParkingPartitionPro.DisLaneWidth / 2);
                            split_p = lanes[i].ClosestPoint(split_p);
                            var splits = SplitLine(lanes[i], new List<Coordinate>() { split_p }).OrderByDescending(e => e.MidPoint.Distance(point));
                            lanes[i] = splits.First();
                            //lane = splits.First();
                            int carscount = cars.Count;
                            Ogenerate_cars(Walls, lanes, lane, lane.P0, carLine, vec, boundary, obspacialindex, carspacialindex, ref cars, ref pillars);
                            if (carscount == cars.Count) found = false;
                            //var pe = lane.ClosestPoint(carLine.P0);
                            //var ps = pe.Translation(-Vector(carLine).Normalize() * MParkingPartitionPro.DisLaneWidth / 2);
                            //var l = SplitLine(lane, new List<Coordinate>() { ps }).OrderByDescending(p => p.MidPoint.Distance(carLine.MidPoint)).First();
                            lane = lanes[i];
                            //lanes[i] = l;
                        }
                    }
                    if (boundary.ClosestPoint(lane.P1).Distance(lane.P1) < 10)
                    {
                        var point = lane.P1;
                        var pointest = point.Translation(-Vector(lane).Normalize() * 2000);
                        var vec = Vector(lane).Normalize().GetPerpendicularVector();
                        var carLine = new LineSegment();
                        var align = true;
                        if (OCanAddCarSpots(lane.P1, lane, -Vector(lane).Normalize(), pointest, vec, carspacialindex, ref carLine, ref align))
                        {
                            found = true;
                            var split_p = boundary.ClosestPoint(carLine.P0).Distance(carLine.P0) < boundary.ClosestPoint(carLine.P1).Distance(carLine.P1) ?
                                carLine.P1 : carLine.P0;
                            split_p = split_p.Translation(-Vector(lane).Normalize() * MParkingPartitionPro.DisLaneWidth / 2);
                            split_p = lanes[i].ClosestPoint(split_p);
                            var splits = SplitLine(lanes[i], new List<Coordinate>() { split_p }).OrderByDescending(e => e.MidPoint.Distance(point));
                            lanes[i] = splits.First();
                            //lane = splits.First();
                            int carscount = cars.Count;
                            Ogenerate_cars(Walls, lanes, lane, lane.P1, carLine, vec, boundary, obspacialindex, carspacialindex, ref cars, ref pillars, false);
                            if (carscount == cars.Count) found = false;
                            //var pe = lane.ClosestPoint(carLine.P0);
                            //var ps = pe.Translation(-Vector(carLine).Normalize() * MParkingPartitionPro.DisLaneWidth / 2);
                            //var l = SplitLine(lane, new List<Coordinate>() { ps }).OrderByDescending(p => p.MidPoint.Distance(carLine.MidPoint)).First();
                            //lane = l;
                            lane = lanes[i];
                            //lanes[i] = l;
                        }
                    }
                    //var lane_endpoints = new List<Coordinate>() { lane.P0, lane.P1 };
                    //for (int j = 0; j < lane_endpoints.Count; j++)
                    //{
                    //    var endpoint = lane_endpoints[j];
                    //    var lane_vec_inner_to_wall = j == 0 ? -Vector(lane) : Vector(lane);
                    //    lane_vec_inner_to_wall = lane_vec_inner_to_wall.Normalize();
                    //    if (boundary.ClosestPoint(endpoint).Distance(endpoint) < 10)
                    //    {
                    //        var pointtest = endpoint.Translation(-lane_vec_inner_to_wall * 5000);
                    //        var perp_vec = Vector(lane).Normalize().GetPerpendicularVector();
                    //        var carLine = new LineSegment();
                    //        var succeed_line = new LineSegment();
                    //        var vecmove = new Vector2D();
                    //        if (CanAddCarSpotsParallelCase(lanes, lane_vec_inner_to_wall, pointtest, perp_vec, ref carspacialindex, ref cars, ref carLine, ref succeed_line))
                    //        {
                    //            found = true;
                    //            var splits = SplitLine(lanes[i], new List<LineSegment>() { succeed_line }).OrderByDescending(e => e.MidPoint.Distance(endpoint));
                    //            if (splits.Count() > 1) lanes[i] = splits.First();
                    //            else
                    //            {
                    //                lanes.RemoveAt(i);
                    //                i--;
                    //            }
                    //            int carscount = cars.Count;
                    //            generate_cars_parallel_case(carLine, succeed_line, ref lanes, ref lane_vec_inner_to_wall, ref cars, ref pillars, Walls, boundary, obspacialindex);
                    //            if (carscount == cars.Count) found = false;
                    //            break;
                    //        }
                    //    }
                    //}
                }
                if (!found) break;
            }

            var removed_cars_group = new List<List<Polygon>>();
            var removed_infocar_group = new List<List<InfoCar>>();
            var added_infocar_group = new List<List<InfoCar>>();
            var car_lanes = new List<LineSegment>();
            var succeed_lanes = new List<LineSegment>();
            var lanes_index = new List<int>();
            var endpoints = new List<Coordinate>();
            var vecs = new List<Vector2D>();
            for (int i = 0; i < lanes.Count; i++)
            {
                var lane = lanes[i];
                var lane_endpoints = new List<Coordinate>() { lane.P0, lane.P1 };
                for (int j = 0; j < lane_endpoints.Count; j++)
                {
                    var endpoint = lane_endpoints[j];
                    var lane_vec_inner_to_wall = j == 0 ? -Vector(lane) : Vector(lane);
                    lane_vec_inner_to_wall = lane_vec_inner_to_wall.Normalize();
                    if (boundary.ClosestPoint(endpoint).Distance(endpoint) < 10)
                    {
                        var pointtest = endpoint.Translation(-lane_vec_inner_to_wall * 5000);
                        var perp_vec = Vector(lane).Normalize().GetPerpendicularVector();
                        var carLine = new LineSegment();
                        var succeed_line = new LineSegment();
                        var vecmove = new Vector2D();
                        var removed_cars = new List<Polygon>();
                        var removed_infocars = new List<InfoCar>();
                        var add_infocars = new List<InfoCar>();
                        if (OCanAddCarSpotsParallelCase(lanes, lane_vec_inner_to_wall, pointtest, perp_vec, carspacialindex, cars, ref carLine, ref succeed_line, ref removed_infocars, ref add_infocars, ref removed_cars))
                        {
                            removed_cars_group.Add(removed_cars);
                            removed_infocar_group.Add(removed_infocars);
                            added_infocar_group.Add(add_infocars);
                            car_lanes.Add(carLine);
                            succeed_lanes.Add(succeed_line);
                            lanes_index.Add(i);
                            endpoints.Add(endpoint);
                            vecs.Add(lane_vec_inner_to_wall);

                            //var splits = SplitLine(lanes[i], new List<LineSegment>() { succeed_line }).OrderByDescending(e => e.MidPoint.Distance(endpoint));
                            //if (splits.Count() > 1) lanes[i] = splits.First();
                            //else
                            //{
                            //    lanes.RemoveAt(i);
                            //    i--;
                            //}
                            //int carscount = cars.Count;
                            //generate_cars_parallel_case(carLine, succeed_line, ref lanes, ref lane_vec_inner_to_wall, ref cars, ref pillars, Walls, boundary, obspacialindex);
                        }
                    }
                }
            }
            //删除背靠背两边均是转角的情况
            if (removed_cars_group.Count > 1)
            {
                for (int i = 0; i < removed_cars_group.Count - 1; i++)
                {
                    for (int j = i + 1; j < removed_cars_group.Count; j++)
                    {
                        var isparallel_carlane = IsParallelLine(car_lanes[i], car_lanes[j]);
                        var dis_carlane = Math.Abs(ClosestDistanceBetweenTwoParallelLines(car_lanes[i], car_lanes[j]) - MParkingPartitionPro.DisVertCarLength * 2) < 10;
                        var carlane_on_i = new LineSegment(car_lanes[i].ClosestPoint(car_lanes[j].P0, true), car_lanes[i].ClosestPoint(car_lanes[j].P1, true));
                        if (car_lanes[i].ClosestPoint(car_lanes[j].P0, true).Distance(car_lanes[j].P0) < MParkingPartitionPro.DisVertCarWidth)
                        {
                            dis_carlane = dis_carlane || Math.Abs(ClosestDistanceBetweenTwoParallelLines(car_lanes[i], carlane_on_i) - MParkingPartitionPro.DisVertCarLength * 2) < 10;
                        }
                        var isparallel_lanes = IsParallelLine(lanes[lanes_index[i]], lanes[lanes_index[j]]);
                        var dis_parallel_lanes = Math.Abs(ClosestDistanceBetweenTwoParallelLines(lanes[lanes_index[i]], lanes[lanes_index[j]]) - MParkingPartitionPro.DisModulus) < 10;
                        if (isparallel_carlane && dis_carlane && isparallel_lanes && dis_parallel_lanes)
                        {
                            cars.AddRange(removed_infocar_group[i]);
                            cars.AddRange(removed_infocar_group[j]);
                            foreach (var car in added_infocar_group[i])
                                cars.Add(car);
                            foreach (var car in added_infocar_group[j])
                                cars.Add(car);
                            removed_cars_group.RemoveAt(j);
                            removed_infocar_group.RemoveAt(j);
                            added_infocar_group.RemoveAt(j);
                            car_lanes.RemoveAt(j);
                            succeed_lanes.RemoveAt(j);
                            lanes_index.RemoveAt(j);
                            endpoints.RemoveAt(j);
                            vecs.RemoveAt(j);
                            removed_cars_group.RemoveAt(i);
                            removed_infocar_group.RemoveAt(i);
                            added_infocar_group.RemoveAt(i);
                            car_lanes.RemoveAt(i);
                            succeed_lanes.RemoveAt(i);
                            lanes_index.RemoveAt(i);
                            endpoints.RemoveAt(i);
                            vecs.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                }
            }
            //背靠背单边转角的情况
            for (int i = 0; i < removed_cars_group.Count; i++)
            {
                bool isbackbackmodule = true;
                foreach (var pl in removed_cars_group[i])
                {
                    if (Math.Abs(pl.Area - MParkingPartitionPro.DisVertCarWidth * MParkingPartitionPro.DisVertCarLengthBackBack) > 1)
                    {
                        isbackbackmodule = false;
                        break;
                    }
                }
                if (isbackbackmodule)
                {
                    cars.AddRange(removed_infocar_group[i]);
                    foreach (var car in added_infocar_group[i])
                        cars.Add(car);
                    removed_cars_group.RemoveAt(i);
                    removed_infocar_group.RemoveAt(i);
                    added_infocar_group.RemoveAt(i);
                    car_lanes.RemoveAt(i);
                    succeed_lanes.RemoveAt(i);
                    lanes_index.RemoveAt(i);
                    endpoints.RemoveAt(i);
                    vecs.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < removed_cars_group.Count; i++)
            {
                var index = lanes_index[i];
                var splits = SplitLine(lanes[index], new List<LineSegment>() { succeed_lanes[i] }).OrderByDescending(e => e.MidPoint.Distance(endpoints[i]));
                if (splits.Count() > 1)
                {
                    for (int j = 0; j < succeed_lanes.Count; j++)
                    {
                        if (succeed_lanes[j].P0.Distance(lanes[index].P0) < 1 && succeed_lanes[j].P1.Distance(lanes[index].P1) < 1)
                        {
                            succeed_lanes[j] = splits.First();
                        }
                    }
                    lanes[index] = splits.First();
                }
                else
                {
                    continue;
                    //下面的情况理论上不会出现，没有具体的case来调试确保正确性
                    var delete = true;
                    for (int j = i + 1; j < lanes_index.Count(); j++)
                    {
                        if (lanes_index[j] > index) lanes_index[j]--;
                        if (lanes_index[j] == index)
                        {
                            delete = false;
                        }
                    }
                    if (delete)
                    {
                        lanes.RemoveAt(index);
                    }
                }
                carspacialindex.Update(new List<Polygon>() { }, removed_cars_group[i].ToArray());
                foreach (var e in removed_infocar_group[i])
                {
                    cars.Remove(e);
                }
                cars.AddRange(added_infocar_group[i]);
                int carscount = cars.Count;
                var update_succeedlane = succeed_lanes[i];
                Ogenerate_cars_parallel_case(car_lanes[i], ref update_succeedlane, ref lanes, vecs[i], ref cars, ref pillars, Walls, boundary, obspacialindex);
                for (int j = i + 1; j < lanes_index.Count(); j++)
                {
                    if (succeed_lanes[i].Length == succeed_lanes[j].Length &&
                        succeed_lanes[i].ClosestPoint(succeed_lanes[j].P0).Distance(succeed_lanes[j].P0) < 1
                        && succeed_lanes[i].ClosestPoint(succeed_lanes[j].P1).Distance(succeed_lanes[j].P1) < 1)
                        succeed_lanes[j] = update_succeedlane;
                }
                succeed_lanes[i] = update_succeedlane;
            }
        }
        private void Ogenerate_cars_parallel_case(LineSegment carLine, ref LineSegment succeedLine, ref List<LineSegment> lanes, Vector2D vecmove,
            ref List<InfoCar> cars, ref List<Polygon> pillars, List<LineString> Walls, Polygon boundary, MNTSSpatialIndex obspacialindex)
        {
            var joinded_lanes = new List<LineSegment>() { carLine, succeedLine };
            var tlanes = JoinCurves(new List<LineString>(), joinded_lanes).OrderByDescending(e => e.Length);
            var lane = new LineSegment();
            if (tlanes.Count() >= 1) lane = new LineSegment(tlanes.First().StartPoint.Coordinate, tlanes.First().EndPoint.Coordinate);
            if (lane.Length > 1)
            {
                var removed_index = lanes.IndexOf(succeedLine);
                //lanes.Remove(succeedLine);
                if (removed_index < 0)
                {
                    for (int k = 0; k < lanes.Count; k++)
                    {
                        if (lanes[k].ClosestPoint(succeedLine.P0).Distance(succeedLine.P0) < 1
                            && lanes[k].ClosestPoint(succeedLine.P1).Distance(succeedLine.P1) < 1
                            && lanes[k].Length - succeedLine.Length < MParkingPartitionPro.DisLaneWidth)
                        {
                            removed_index = k;
                        }
                    }
                }
                lanes.RemoveAt(removed_index);
                lanes.Insert(removed_index, lane);
                succeedLine = lane;
                var tlane_depth = lane.Translation(vecmove.Normalize() * (MParkingPartitionPro.DisVertCarLength + MParkingPartitionPro.DisLaneWidth / 2));
                var tlane_rec = PolyFromLines(lane, tlane_depth);

                //0620modified:
                tlane_rec = tlane_rec.Scale(MParkingPartitionPro.ScareFactorForCollisionCheck);
                var prepsplit_lanes = lanes.Where(e => e.IntersectPoint(tlane_rec).Count() > 0).Where(e => IsPerpLine(e, lane))
                    .Where(e => e.ToLineString().IntersectPoint(lane.ToLineString()).Count() > 0).ToList();
                var lanes_split = SplitLine(lane, prepsplit_lanes);
                if (lanes_split.Count() > 0)
                {
                    lane = lanes_split[0];
                    tlane_depth = lane.Translation(vecmove.Normalize() * (MParkingPartitionPro.DisVertCarLength + MParkingPartitionPro.DisLaneWidth / 2));
                    tlane_rec = PolyFromLines(lane, tlane_depth);
                    var inside_cars = cars.Where(e => tlane_rec.Contains(e.Polyline.Envelope.Centroid)).Where(e => e.CarLayoutMode == ((int)CarLayoutMode.PARALLEL)).ToList();
                    if (inside_cars.Count > 0)
                    {
                        var inside_end_ps = new List<Coordinate>();
                        inside_cars.ForEach(car => inside_end_ps.AddRange(car.Polyline.Coordinates));
                        inside_end_ps = inside_end_ps.Select(p => lane.ClosestPoint(p, false)).ToList();
                        var inside_end_p = inside_end_ps.OrderBy(p => p.Distance(lane.P0)).First();
                        lane = new LineSegment(lane.P0, inside_end_p);
                        tlane_depth = lane.Translation(vecmove.Normalize() * (MParkingPartitionPro.DisVertCarLength + MParkingPartitionPro.DisLaneWidth / 2));
                        tlane_rec = PolyFromLines(lane, tlane_depth);
                    }
                    cars = cars.Where(e => !tlane_rec.Contains(e.Polyline.Envelope.Centroid)).ToList();
                    pillars = pillars.Where(e => !tlane_rec.Contains(e.Envelope.Centroid)).ToList();
                    var partitionpro = new MParkingPartitionPro();
                    partitionpro.Walls = Walls;
                    partitionpro.Boundary = boundary;
                    partitionpro.ObstaclesSpatialIndex = obspacialindex;
                    partitionpro.Obstacles = obspacialindex.SelectAll().Cast<Polygon>().ToList();
                    partitionpro.IniLanes.Add(new Lane(lane, vecmove.Normalize()));
                    partitionpro.IniLanes.AddRange(lanes.Select(e => new Lane(e, Vector2D.Zero)));
                    partitionpro.UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
                    var firstlane = partitionpro.IniLanes[0];
                    partitionpro.IniLanes = new List<Lane>() { firstlane };
                    var vertlanes = partitionpro.GeneratePerpModuleLanes(VMStock.RoadWidth / 2 + (VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotLength : VMStock.VerticalSpotWidth),
                   VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotWidth : VMStock.VerticalSpotLength, false, null, true);
                    foreach (var k in vertlanes)
                    {
                        var vl = k.Line;
                        if (ClosestPointInVertLines(vl.P1, vl, lanes.ToArray()) < 10) lane = new LineSegment(lane.P1, lane.P0);
                        //if (ClosestPointInVertLines(vl.P0, vl, lanes.ToArray()) < 10)
                        //{
                        //    vl.P0 = vl.P0.Translation(Vector(vl).Normalize() * MParkingPartitionPro.DisLaneWidth / 2);
                        //}
                        //if (ClosestPointInVertLines(vl.P1, vl, lanes.ToArray()) < 10)
                        //{
                        //    vl.P1 = vl.P1.Translation(-Vector(vl).Normalize() * MParkingPartitionPro.DisLaneWidth / 2);
                        //}
                        var line = new LineSegment(vl);
                        line = line.Translation(k.Vec.Normalize() * VMStock.RoadWidth / 2);
                        var line_align_backback_rest = new LineSegment();
                        partitionpro.GenerateCarsAndPillarsForEachLane(line, k.Vec.Normalize(), VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotWidth : VMStock.VerticalSpotLength,
                            VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotLength : VMStock.VerticalSpotWidth
                            , ref line_align_backback_rest, true, false, false, false, true, true, false, false, false, true, false, false, false, true);
                    }
                    vertlanes = partitionpro.GeneratePerpModuleLanes(VMStock.ParallelSpotLength > VMStock.ParallelSpotWidth ? VMStock.ParallelSpotWidth + VMStock.RoadWidth / 2 : VMStock.ParallelSpotLength
                        + VMStock.RoadWidth / 2,
                        VMStock.ParallelSpotLength > VMStock.ParallelSpotWidth ? VMStock.ParallelSpotLength : VMStock.ParallelSpotWidth,
                        false);
                    foreach (var k in vertlanes)
                    {
                        var vl = k.Line;
                        if (ClosestPointInVertLines(vl.P1, vl, lanes.ToArray()) < 10) lane = new LineSegment(lane.P1, lane.P0);
                        //if (ClosestPointInVertLines(vl.P0, vl, lanes.ToArray()) < 10)
                        //{
                        //    vl.P0 = vl.P0.Translation(Vector(vl).Normalize() * MParkingPartitionPro.DisLaneWidth / 2);
                        //}
                        //if (ClosestPointInVertLines(vl.P1, vl, lanes.ToArray()) < 10)
                        //{
                        //    vl.P1 = vl.P1.Translation(-Vector(vl).Normalize() * MParkingPartitionPro.DisLaneWidth / 2);
                        //}
                        var line = new LineSegment(vl);
                        line = line.Translation(k.Vec.Normalize() * 2750);
                        var line_align_backback_rest = new LineSegment();
                        partitionpro.GenerateCarsAndPillarsForEachLane(line, k.Vec,
                            VMStock.ParallelSpotLength > VMStock.ParallelSpotWidth ? VMStock.ParallelSpotLength : VMStock.ParallelSpotWidth,
                            VMStock.ParallelSpotLength > VMStock.ParallelSpotWidth ? VMStock.ParallelSpotWidth : VMStock.ParallelSpotLength
                            , ref line_align_backback_rest, true, false, false, false, true, true, false);
                    }
                    partitionpro.ReDefinePillarDimensions();
                    cars.AddRange(partitionpro.Cars);
                    pillars.AddRange(partitionpro.Pillars);
                }
            }
            else return;

        }
        private bool OCanAddCarSpotsParallelCase(List<LineSegment> lanes, Vector2D lane_vec_inner_to_wall, Coordinate pt, Vector2D perpvec, MNTSSpatialIndex _carspacialindex, List<InfoCar> _cars,
            ref LineSegment carLine, ref LineSegment succeed_line, ref List<InfoCar> removed_infocars, ref List<InfoCar> added_infocars,
            ref List<Polygon> removed_cars)
        {
            var cars = _cars.Select(e => e).ToList();
            var carspacialindex = new MNTSSpatialIndex(_carspacialindex.SelectAll());
            var ptwall = pt.Translation(lane_vec_inner_to_wall * 5000);
            pt = ptwall.Translation(-lane_vec_inner_to_wall * (MParkingPartitionPro.DisVertCarLength - 10));
            var iline = new LineSegment(pt, ptwall);
            var iline_a = iline.Translation(perpvec * 6000);
            var iline_b = iline.Translation(-perpvec * 6000);
            var pla = PolyFromLines(iline, iline_a);
            var plb = PolyFromLines(iline, iline_b);
            var cars_a = carspacialindex.SelectCrossingGeometry(pla).Cast<Polygon>();
            var cars_b = carspacialindex.SelectCrossingGeometry(plb).Cast<Polygon>();
            if (cars_a.Count() > 0 && cars_b.Count() > 0)
            {
                var car_a = cars_a.OrderBy(e =>
                {
                    var edge = e.GetEdges().OrderBy(seg => iline.ClosestPoint(seg.MidPoint).Distance(seg.MidPoint)).First();
                    return edge.Distance(ptwall);
                }).First();
                var edge_a = car_a.GetEdges().OrderBy(seg => iline.ClosestPoint(seg.MidPoint).Distance(seg.MidPoint)).First();
                var car_b = cars_b.OrderBy(e =>
                {
                    var edge = e.GetEdges().OrderBy(seg => iline.ClosestPoint(seg.MidPoint).Distance(seg.MidPoint)).First();
                    return edge.Distance(ptwall);
                }).First();
                var edge_b = car_b.GetEdges().OrderBy(seg => iline.ClosestPoint(seg.MidPoint).Distance(seg.MidPoint)).First();
                var iaVert_a = Math.Abs(edge_a.Length - MParkingPartitionPro.DisVertCarLength) < 1;
                var iaVert_b = Math.Abs(edge_b.Length - MParkingPartitionPro.DisVertCarLength) < 1;
                if (iaVert_a && !iaVert_b)
                {
                    var added_cars = new List<InfoCar>();
                    cars_b = cars_b.Where(e =>
                    {
                        var curseg = e.GetEdges().OrderBy(seg => iline.ClosestPoint(seg.MidPoint).Distance(seg.MidPoint)).First();
                        if (Math.Abs(curseg.Length - MParkingPartitionPro.DisVertCarLength) < 1) return false;
                        return true;
                    }).ToList();
                    //carspacialindex.Update(new List<Polygon>(), cars_b);
                    removed_cars.AddRange(cars_b);
                    for (int k = 0; k < cars.Count; k++)
                    {
                        foreach (var car in cars_b)
                        {
                            if (cars[k].Polyline.Centroid.Coordinate.Distance(car.Centroid.Coordinate) < 1)
                            {
                                added_cars.Add(cars[k]);
                                removed_infocars.Add(cars[k]);
                                cars.RemoveAt(k);
                                k--;
                                break;
                            }
                        }
                    }
                    var another_edges_a = car_a.GetEdges().OrderBy(seg => iline.ClosestPoint(seg.MidPoint).Distance(seg.MidPoint)).ToList();
                    another_edges_a.RemoveAt(0);
                    var another_edge_a = another_edges_a.Take(2).OrderByDescending(seg => ptwall.Distance(seg.MidPoint)).First();
                    var another_edge_a_mid = another_edge_a.MidPoint;
                    var a_correspond_lane = lanes.OrderBy(e => e.ClosestPoint(another_edge_a_mid).Distance(another_edge_a_mid)).First();
                    var p_s = iline.ClosestPoint(a_correspond_lane.P0, true);
                    var p_s_on_lane = a_correspond_lane.ClosestPoint(p_s);
                    var p_e = p_s_on_lane.Translation(-perpvec * MParkingPartitionPro.DisLaneWidth / 2);
                    carLine = new LineSegment(p_s_on_lane, p_e);
                    succeed_line = a_correspond_lane;
                    if (IsPerpLine(carLine, succeed_line))
                    {
                        //一边垂直，一边背靠背的平行情况暂不支持
                        cars.AddRange(added_cars);
                        added_infocars.AddRange(added_cars);
                        return false;
                    }
                    if (succeed_line.ClosestPoint(carLine.P0).Distance(carLine.P0) < 1 && succeed_line.ClosestPoint(carLine.P1).Distance(carLine.P1) < 1)
                    {
                        cars.AddRange(added_cars);
                        added_infocars.AddRange(added_cars);
                        return false;
                    }
                    //_carspacialindex = carspacialindex;
                    return true;
                }
                else if (!iaVert_a && iaVert_b)
                {
                    var added_cars = new List<InfoCar>();
                    cars_a = cars_a.Where(e =>
                    {
                        var curseg = e.GetEdges().OrderBy(seg => iline.ClosestPoint(seg.MidPoint).Distance(seg.MidPoint)).First();
                        if (Math.Abs(curseg.Length - MParkingPartitionPro.DisVertCarLength) < 1) return false;
                        return true;
                    }).ToList();
                    //carspacialindex.Update(new List<Polygon>(), cars_a);
                    removed_cars.AddRange(cars_a);
                    for (int k = 0; k < cars.Count; k++)
                    {
                        foreach (var car in cars_a)
                        {
                            if (cars[k].Polyline.Centroid.Coordinate.Distance(car.Centroid.Coordinate) < 1)
                            {
                                added_cars.Add(cars[k]);
                                removed_infocars.Add(cars[k]);
                                cars.RemoveAt(k);
                                k--;
                                break;
                            }
                        }
                    }
                    var another_edges_b = car_b.GetEdges().OrderBy(seg => iline.ClosestPoint(seg.MidPoint).Distance(seg.MidPoint)).ToList();
                    another_edges_b.RemoveAt(0);
                    var another_edge_b = another_edges_b.Take(2).OrderByDescending(seg => ptwall.Distance(seg.MidPoint)).First();
                    var another_edge_b_mid = another_edge_b.MidPoint;
                    var b_correspond_lane = lanes.OrderBy(e => e.ClosestPoint(another_edge_b_mid).Distance(another_edge_b_mid)).First();
                    var p_s = iline.ClosestPoint(b_correspond_lane.P0, true);
                    var p_s_on_lane = b_correspond_lane.ClosestPoint(p_s);
                    var p_e = p_s_on_lane.Translation(perpvec * MParkingPartitionPro.DisLaneWidth / 2);
                    carLine = new LineSegment(p_s_on_lane, p_e);
                    succeed_line = b_correspond_lane;
                    if (IsPerpLine(carLine, succeed_line))
                    {
                        //一边垂直，一边背靠背的平行情况暂不支持
                        cars.AddRange(added_cars);
                        added_infocars.AddRange(added_cars);
                        return false;
                    }
                    if (succeed_line.ClosestPoint(carLine.P0).Distance(carLine.P0) < 1 && succeed_line.ClosestPoint(carLine.P1).Distance(carLine.P1) < 1)
                    {
                        cars.AddRange(added_cars);
                        added_infocars.AddRange(added_cars);
                        return false;
                    }
                    //_carspacialindex = carspacialindex;
                    return true;
                }
                return false;
            }
            else if (cars_a.Count() == 0 && cars_b.Count() > 0)
            {
                var car_b = cars_b.OrderBy(e =>
                {
                    var edge = e.GetEdges().OrderBy(seg => iline.ClosestPoint(seg.MidPoint).Distance(seg.MidPoint)).First();
                    return edge.Distance(ptwall);
                }).First();
                var edge_b = car_b.GetEdges().OrderBy(seg => iline.ClosestPoint(seg.MidPoint).Distance(seg.MidPoint)).First();
                var iaVert_b = Math.Abs(edge_b.Length - MParkingPartitionPro.DisVertCarLength) < 1;
                if (iaVert_b)
                {
                    var another_edges_b = car_b.GetEdges().OrderBy(seg => iline.ClosestPoint(seg.MidPoint).Distance(seg.MidPoint)).ToList();
                    another_edges_b.RemoveAt(0);
                    var another_edge_b = another_edges_b.Take(2).OrderByDescending(seg => ptwall.Distance(seg.MidPoint)).First();
                    var another_edge_b_mid = another_edge_b.MidPoint;
                    var b_correspond_lane = lanes.OrderBy(e => e.ClosestPoint(another_edge_b_mid).Distance(another_edge_b_mid)).First();
                    var p_s = iline.ClosestPoint(b_correspond_lane.P0, true);
                    var p_s_on_lane = b_correspond_lane.ClosestPoint(p_s);
                    var p_e = p_s_on_lane.Translation(perpvec * MParkingPartitionPro.DisLaneWidth / 2);
                    carLine = new LineSegment(p_s_on_lane, p_e);
                    succeed_line = b_correspond_lane;
                    if (IsPerpLine(carLine, succeed_line))
                    {
                        //一边垂直，一边背靠背的平行情况暂不支持
                        return false;
                    }
                    if (succeed_line.ClosestPoint(carLine.P0).Distance(carLine.P0) < 1 && succeed_line.ClosestPoint(carLine.P1).Distance(carLine.P1) < 1)
                    {
                        return false;
                    }
                    //_carspacialindex = carspacialindex;
                    return true;
                }
                else return false;
            }
            else if (cars_a.Count() > 0 && cars_b.Count() == 0)
            {
                var car_a = cars_a.OrderBy(e =>
                {
                    var edge = e.GetEdges().OrderBy(seg => iline.ClosestPoint(seg.MidPoint).Distance(seg.MidPoint)).First();
                    return edge.Distance(ptwall);
                }).First();
                var edge_a = car_a.GetEdges().OrderBy(seg => iline.ClosestPoint(seg.MidPoint).Distance(seg.MidPoint)).First();
                var iaVert_a = Math.Abs(edge_a.Length - MParkingPartitionPro.DisVertCarLength) < 1;
                if (iaVert_a)
                {
                    var another_edges_a = car_a.GetEdges().OrderBy(seg => iline.ClosestPoint(seg.MidPoint).Distance(seg.MidPoint)).ToList();
                    another_edges_a.RemoveAt(0);
                    var another_edge_a = another_edges_a.Take(2).OrderByDescending(seg => ptwall.Distance(seg.MidPoint)).First();
                    var another_edge_a_mid = another_edge_a.MidPoint;
                    var a_correspond_lane = lanes.OrderBy(e => e.ClosestPoint(another_edge_a_mid).Distance(another_edge_a_mid)).First();
                    var p_s = iline.ClosestPoint(a_correspond_lane.P0, true);
                    var p_s_on_lane = a_correspond_lane.ClosestPoint(p_s);
                    var p_e = p_s_on_lane.Translation(-perpvec * MParkingPartitionPro.DisLaneWidth / 2);
                    carLine = new LineSegment(p_s_on_lane, p_e);
                    succeed_line = a_correspond_lane;
                    if (IsPerpLine(carLine, succeed_line))
                    {
                        //一边垂直，一边背靠背的平行情况暂不支持
                        return false;
                    }
                    if (succeed_line.ClosestPoint(carLine.P0).Distance(carLine.P0) < 1 && succeed_line.ClosestPoint(carLine.P1).Distance(carLine.P1) < 1)
                    {
                        return false;
                    }
                    //_carspacialindex = carspacialindex;
                    return true;
                }
                else return false;
            }
            else return false;
        }
        private bool OCanAddCarSpots(Coordinate testpoint, LineSegment line, Vector2D linevec, Coordinate pt, Vector2D vec, MNTSSpatialIndex carspacialindex, ref LineSegment carLine, ref bool aligned)
        {
            var pta = pt.Translation(vec * 6000);
            var linea = new LineSegment(pt, pta);
            var ptb = pt.Translation(-vec * 6000);
            var lineb = new LineSegment(pt, ptb);
            var carsa = new List<Polygon>();
            var carsb = new List<Polygon>();
            for (int i = 0; i < 7; i++)
            {
                double step = 1000;
                linea = linea.Translation(linevec * /*i **/ step);
                lineb = lineb.Translation(linevec * /*i **/ step);
                var crossedcarsa = carspacialindex.SelectCrossingGeometry(linea.Buffer(10)).Cast<Polygon>()
             .OrderBy(e => e.Centroid.Coordinate.Distance(pt));
                var crossedcarsb = carspacialindex.SelectCrossingGeometry(lineb.Buffer(10)).Cast<Polygon>()
                    .OrderBy(e => e.Centroid.Coordinate.Distance(pt));
                if (crossedcarsa.Count() > 0 && crossedcarsb.Count() > 0)
                {
                    carsa.Add(crossedcarsa.First());
                    carsb.Add(crossedcarsb.First());
                }
            }
            carsa = carsa.OrderByDescending(e => e.GetEdges().OrderBy(s => line.ClosestPoint(s.MidPoint).Distance(s.MidPoint)).First().Distance(pt)).ToList();
            carsb = carsb.OrderByDescending(e => e.GetEdges().OrderBy(s => line.ClosestPoint(s.MidPoint).Distance(s.MidPoint)).First().Distance(pt)).ToList();
            var cara = new Polygon(new LinearRing(new Coordinate[0]));
            var carb = new Polygon(new LinearRing(new Coordinate[0]));
            cara = carsa.Count > 0 ? carsa[0] : cara;
            carb = carsb.Count > 0 ? carsb[0] : carb;
            var hasparallel = false;
            var hasvert = false;
            if (cara.Area > 0 && carb.Area > 0)
            {
                var pta_on_line = line.ClosestPoint(cara.GetEdges().OrderByDescending(e => e.Length).Take(2).First().MidPoint);
                var ptb_on_line = line.ClosestPoint(carb.GetEdges().OrderByDescending(e => e.Length).Take(2).First().MidPoint);
                if (pta_on_line.Distance(ptb_on_line) > 100) aligned = false;
            }
            if (cara.Area > 0)
            {
                var segs = cara.GetEdges();
                var seg = segs.OrderByDescending(e => e.Length).First();
                if (seg.Length != MParkingPartitionPro.DisVertCarLength)
                    hasvert = true;
                if (IsParallelLine(seg, line))
                {
                    hasparallel = true;
                    carLine = seg;
                }
                else if (IsPerpLine(seg, line)) hasvert = true;
            }
            if (carb.Area > 0)
            {
                var segs = carb.GetEdges();
                var seg = segs.OrderByDescending(e => e.Length).First();
                if (seg.Length != MParkingPartitionPro.DisVertCarLength)
                    hasvert = true;
                if (IsParallelLine(seg, line))
                {
                    hasparallel = true;
                    if (line.ClosestPoint(seg.MidPoint).Distance(testpoint) < line.ClosestPoint(carLine.MidPoint).Distance(testpoint))
                    {
                        carLine = seg;
                    }
                }
                else if (IsPerpLine(seg, line)) hasvert = true;
            }
            if (hasparallel && !hasvert)
            {
                return true;
            }
            return false;
        }

        private void Ogenerate_cars(List<LineString> Walls, List<LineSegment> lanes, LineSegment lane, Coordinate pton_wall, LineSegment carLine, Vector2D vec, Polygon boundary, MNTSSpatialIndex obspacialindex
            , MNTSSpatialIndex carspacialindex, ref List<InfoCar> cars, ref List<Polygon> pillars, bool isstartpoint = true)
        {
            var partitionpro = new MParkingPartitionPro();
            partitionpro.Walls = Walls;
            var ps = lane.ClosestPoint(carLine.P0, false);
            var pe = lane.ClosestPoint(carLine.P1, false);
            ps = ps.Translation(new Vector2D(pe, ps).Normalize() * 10);
            pe = pe.Translation(new Vector2D(ps, pe).Normalize() * 10);
            var point = isstartpoint ? lane.P0 : lane.P1;
            if (ps.Distance(point) < pe.Distance(point))
            {
                var p = pe;
                pe = ps;
                ps = p;
            }
            //pe = pton_wall;
            var ls = LineSegmentSDL(ps, vec.Normalize(), 999999);
            ls = ls.Translation(-vec.Normalize() * ls.Length / 2);
            ls = SplitLine(ls, boundary).OrderBy(e => e.MidPoint.Distance(ps)).First();
            var buffer = ls.Buffer(10);
            buffer = PolyFromLines(ls, ls.Translation(new Vector2D(ps, pton_wall)));
            var obcrossed = obspacialindex.SelectCrossingGeometry(buffer).Cast<Polygon>().ToList();
            var splits = SplitLine(ls, obcrossed)
                .Where(e => !IsInAnyPolys(e.MidPoint, obcrossed))
                .OrderBy(e => e.MidPoint.Distance(ps));
            if (splits.Count() > 0) ls = splits.First();
            else return;
            var carcrossded = carspacialindex.SelectCrossingGeometry(buffer).Cast<Polygon>().ToList();
            ls = SplitLine(ls, carcrossded, 1, true).OrderBy(e => e.MidPoint.Distance(ps)).First();
            var perplanes = lanes.Where(e => IsPerpLine(ls, e)).Where(e => e.ClosestPoint(ps, false).Distance(ps) > 10).ToList();
            ls = SplitLine(ls, perplanes)
                .OrderBy(e => e.MidPoint.Distance(ps)).First();
            var le = new LineSegment(ls);
            le = le.Translation(new Vector2D(ps, pton_wall));
            var pl = PolyFromLines(ls, le);

            if (ClosestPointInVertLines(ls.P1, ls, lanes.ToArray()) < 10) ls = new LineSegment(ls.P1, ls.P0);
            if (ClosestPointInVertLines(ls.P0, ls, lanes.ToArray()) < 10)
                ls.P0 = ls.P0.Translation(Vector(ls).Normalize() * VMStock.RoadWidth / 2);
            if (ClosestPointInVertLines(ls.P1, ls, lanes.ToArray()) < 10)
                ls.P1 = ls.P1.Translation(-Vector(ls).Normalize() * VMStock.RoadWidth / 2);
            var vecmove = new Vector2D(ps, pe);
            ls = ls.Translation(vecmove.Normalize() * 10);
            ls = ls.Translation(-vecmove.Normalize() * VMStock.RoadWidth / 2);
            partitionpro.Boundary = boundary;
            partitionpro.ObstaclesSpatialIndex = obspacialindex;
            partitionpro.Obstacles = obspacialindex.SelectAll().Cast<Polygon>().ToList();
            //partitionpro.IniLanes.Add(new Lane(ls, vecmove));
            var lsbuffer = new Polygon(new LinearRing(new Coordinate[0]));
            try
            {
                lsbuffer = ls.Buffer(MParkingPartitionPro.DisLaneWidth / 2 - 10);
            }
            catch { return; }
            lsbuffer = lsbuffer.Scale(MParkingPartitionPro.ScareFactorForCollisionCheck);
            if (carspacialindex.SelectCrossingGeometry(lsbuffer).Count > 0) return;
            pl = pl.Scale(MParkingPartitionPro.ScareFactorForCollisionCheck);
            cars = cars.Where(e => e.Polyline.IntersectPoint(pl).Count() == 0).ToList();
            pillars = pillars.Where(e => e.IntersectPoint(pl).Count() == 0).ToList();
            //重排
            var inilanesex = new List<LineSegment>();
            var lanes_copy = lanes.Select(e => e).ToList();
            for (int i = 0; i < lanes_copy.Count; i++)
            {
                if (lanes_copy[i].ClosestPoint(ls.P0).Distance(ls.P0) < 1)
                {
                    inilanesex.Add(lanes_copy[i]);
                    lanes_copy.RemoveAt(i);
                    i--;
                }
                else if (lanes_copy[i].ClosestPoint(ls.P1).Distance(ls.P1) < 1)
                {
                    inilanesex.Add(lanes_copy[i]);
                    lanes_copy.RemoveAt(i);
                    i--;
                }
            }
            var tlanes = JoinCurves(new List<LineString>(), inilanesex).OrderByDescending(e => e.Length);
            if (tlanes.Count() >= 2)
            {
                var tlane = new LineSegment(tlanes.First().StartPoint.Coordinate, tlanes.First().EndPoint.Coordinate);
                var tlane_depth = tlane.Translation(vecmove.Normalize() * (MParkingPartitionPro.DisVertCarLength + MParkingPartitionPro.DisLaneWidth / 2));
                var tlane_rec = PolyFromLines(tlane, tlane_depth);
                cars = cars.Where(e => !tlane_rec.Contains(e.Polyline.Envelope.Centroid)).ToList();
                pillars = pillars.Where(e => !tlane_rec.Contains(e.Envelope.Centroid)).ToList();
                partitionpro.IniLanes.Add(new Lane(tlane, vecmove));
            }
            else
            {
                partitionpro.IniLanes.Add(new Lane(ls, vecmove));
            }
            partitionpro.IniLanes.AddRange(lanes.Select(e => new Lane(e, vecmove)));
            partitionpro.UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
            var firstlane = partitionpro.IniLanes[0];
            partitionpro.IniLanes = new List<Lane>() { firstlane };
            var vertlanes = partitionpro.GeneratePerpModuleLanes(VMStock.RoadWidth / 2 + (VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotLength : VMStock.VerticalSpotWidth),
                VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotWidth : VMStock.VerticalSpotLength, false, null, true);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                if (ClosestPointInVertLines(vl.P1, vl, lanes.ToArray()) < 10) ls = new LineSegment(ls.P1, ls.P0);
                //if (ClosestPointInVertLines(vl.P0, vl, lanes.ToArray()) < 10)
                //{
                //    vl.P0 = vl.P0.Translation(Vector(vl).Normalize() * MParkingPartitionPro.DisLaneWidth / 2);
                //}
                //if (ClosestPointInVertLines(vl.P1, vl, lanes.ToArray()) < 10)
                //{
                //    vl.P1 = vl.P1.Translation(-Vector(vl).Normalize() * MParkingPartitionPro.DisLaneWidth / 2);
                //}
                var line = new LineSegment(vl);
                line = line.Translation(k.Vec.Normalize() * VMStock.RoadWidth / 2);
                var line_align_backback_rest = new LineSegment();
                partitionpro.GenerateCarsAndPillarsForEachLane(line, k.Vec.Normalize(), VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotWidth : VMStock.VerticalSpotLength,
                    VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotLength : VMStock.VerticalSpotWidth
                    , ref line_align_backback_rest, true, false, false, false, true, true, false, false, false, true, false, false, false, true);
            }
            vertlanes = partitionpro.GeneratePerpModuleLanes(VMStock.ParallelSpotLength > VMStock.ParallelSpotWidth ? VMStock.ParallelSpotWidth + VMStock.RoadWidth / 2 : VMStock.ParallelSpotLength
                + VMStock.RoadWidth / 2,
                VMStock.ParallelSpotLength > VMStock.ParallelSpotWidth ? VMStock.ParallelSpotLength : VMStock.ParallelSpotWidth,
                false);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                if (ClosestPointInVertLines(vl.P1, vl, lanes.ToArray()) < 10) ls = new LineSegment(ls.P1, ls.P0);
                //if (ClosestPointInVertLines(vl.P0, vl, lanes.ToArray()) < 10)
                //{
                //    vl.P0 = vl.P0.Translation(Vector(vl).Normalize() * MParkingPartitionPro.DisLaneWidth / 2);
                //}
                //if (ClosestPointInVertLines(vl.P1, vl, lanes.ToArray()) < 10)
                //{
                //    vl.P1 = vl.P1.Translation(-Vector(vl).Normalize() * MParkingPartitionPro.DisLaneWidth / 2);
                //}
                var line = new LineSegment(vl);
                line = line.Translation(k.Vec.Normalize() * 2750);
                var line_align_backback_rest = new LineSegment();
                partitionpro.GenerateCarsAndPillarsForEachLane(line, k.Vec,
                    VMStock.ParallelSpotLength > VMStock.ParallelSpotWidth ? VMStock.ParallelSpotLength : VMStock.ParallelSpotWidth,
                    VMStock.ParallelSpotLength > VMStock.ParallelSpotWidth ? VMStock.ParallelSpotWidth : VMStock.ParallelSpotLength
                    , ref line_align_backback_rest, true, false, false, false, true, true, false);
            }
            partitionpro.ReDefinePillarDimensions();
            cars.AddRange(partitionpro.Cars);
            pillars.AddRange(partitionpro.Pillars);
        }

        public void OGenerateCarsOntheEndofLanesByFillTheEndDistrict(ref List<InfoCar> cars, ref List<Polygon> pillars, ref List<LineSegment> lanes,
            List<LineString> Walls, MNTSSpatialIndex obspacialindex, Polygon boundary)
        {
            var carspacialindex = new MNTSSpatialIndex(cars.Select(e => e.Polyline));
            var laneboxpacialindex = new MNTSSpatialIndex(lanes.Select(e => e.Buffer(MParkingPartitionPro.DisLaneWidth / 2 - 1).Scale(MParkingPartitionPro.ScareFactorForCollisionCheck)));
            var recoglines = new List<LineSegment>();
            for (int i = 0; i < lanes.Count; i++)
            {
                if (ClosestPointInVertLines(lanes[i].P0, lanes[i], lanes) > 1)
                    recoglines.Add(new LineSegment(lanes[i].P1, lanes[i].P0));
                else if (ClosestPointInVertLines(lanes[i].P1, lanes[i], lanes) > 1)
                    recoglines.Add(new LineSegment(lanes[i]));
            }
            int count = 0;
            foreach (var lane in recoglines)
            {
                count++;
                var line = LineSegmentSDL(lane.P1, Vector(lane).Normalize().GetPerpendicularVector(), MParkingPartitionPro.DisLaneWidth / 2);
                line.P0 = line.P0.Translation(-Vector(line).Normalize() * MParkingPartitionPro.DisLaneWidth / 2);
                //lane很短，在垂直于它的车道框里的case解决
                var line_depth_test = line.Translation(Vector(lane).Normalize() * 1000);
                var rec_test = PolyFromLines(line, line_depth_test);
                var lanecrossed = laneboxpacialindex.SelectCrossingGeometry(rec_test).Cast<Polygon>();
                if (lanecrossed.Count() > 0 && lanecrossed.First().IntersectPoint(rec_test).Count() > 0)
                {
                    var cross = lanecrossed.First();
                    var insec_pt = cross.IntersectPoint(rec_test).First();
                    var move_dis = line.ClosestPoint(insec_pt, true).Distance(insec_pt);
                    line = line.Translation(Vector(lane).Normalize() * move_dis);
                }
                var line_depth = line.Translation(Vector(lane).Normalize() * (MParkingPartitionPro.DisVertCarLength + MParkingPartitionPro.CollisionD - MParkingPartitionPro.CollisionTOP));
                var rec = PolyFromLines(line, line_depth);
                var rec_sc = rec.Scale(MParkingPartitionPro.ScareFactorForCollisionCheck);
                if (carspacialindex.SelectCrossingGeometry(rec_sc).Count() > 0) continue;
                var inherit_line = new LineSegment(line.MidPoint, line_depth.MidPoint);
                var points = new List<Coordinate>();
                var obs_crossed = obspacialindex.SelectCrossingGeometry(rec_sc).Cast<Polygon>();
                foreach (var obj in obs_crossed)
                {
                    points.AddRange(obj.IntersectPoint(rec));
                    points.AddRange(obj.Coordinates.Where(p => rec.Contains(p)));
                }
                points.AddRange(boundary.IntersectPoint(rec));
                points.AddRange(boundary.Coordinates.Where(p => rec.Contains(p)));
                //var inherit_line_points = points.Select(p => inherit_line.ClosestPoint(p)).ToList();
                //var splits_inherit_lines = SplitLine(inherit_line, inherit_line_points);
                //var ini_inherit_line_length = inherit_line.Length;
                //if (splits_inherit_lines.Count() > 0) inherit_line = splits_inherit_lines.First();
                //if (inherit_line.Length < ini_inherit_line_length) continue;
                var points_line = points.Select(p => line.ClosestPoint(p)).ToList();
                var splits_line = SplitLine(line, points_line).Where(e => e.Length >= MParkingPartitionPro.DisVertCarWidth);
                foreach (var split in splits_line)
                {
                    var split_depth = split.Translation(Vector(lane).Normalize() * (MParkingPartitionPro.DisVertCarLength + MParkingPartitionPro.CollisionD - MParkingPartitionPro.CollisionTOP));
                    var split_rec = PolyFromLines(split, split_depth);
                    var split_rec_sc = split_rec.Scale(MParkingPartitionPro.ScareFactorForCollisionCheck);
                    if (obspacialindex.SelectCrossingGeometry(split_rec_sc).Count() > 0) continue;
                    if (boundary.IntersectPoint(split_rec_sc).Count() > 0) continue;
                    MParkingPartitionPro tmpro = new MParkingPartitionPro();
                    tmpro.IniLanes.Add(new Lane(split, Vector(inherit_line).Normalize()));
                    tmpro.Obstacles = new List<Polygon>();
                    tmpro.ObstaclesSpatialIndex = new MNTSSpatialIndex(tmpro.Obstacles);
                    var line_align_backback_rest = new LineSegment();
                    tmpro.GenerateCarsAndPillarsForEachLane(split, Vector(inherit_line).Normalize(), VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotWidth : VMStock.VerticalSpotLength,
                                   VMStock.VerticalSpotLength > VMStock.VerticalSpotWidth ? VMStock.VerticalSpotLength : VMStock.VerticalSpotWidth
                                   , ref line_align_backback_rest, true, false, false, false, true, true, false, false, false, true, false, false, false, true);
                    var tmpcars = tmpro.Cars;
                    tmpcars = tmpro.Cars.Where(e => boundary.Contains(e.Polyline.Centroid.Coordinate))
                        .Where(e => laneboxpacialindex.SelectCrossingGeometry(e.Polyline.Scale(MParkingPartitionPro.ScareFactorForCollisionCheck)).Count == 0)
                        .ToList();
                    tmpcars = tmpcars.Where(e => carspacialindex.SelectCrossingGeometry(e.Polyline).Count == 0).ToList();
                    carspacialindex.Update(tmpcars.Select(e => e.Polyline), new List<Polygon>());
                    if (tmpcars.Count > 0)
                        cars.AddRange(tmpcars);
                }
            }
        }
    }
}
