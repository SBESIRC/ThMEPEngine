using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Geometries;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPHVAC.FloorHeatingCoil
{
    class IntersectWayCalculator
    {
        Polyline region = null;
        int main_index = -1;
        public double buffer = 600;
        public double room_buffer = 100;
        List<PipeInput> pipe_inputs = null;
        List<List<Polyline>> equispaced_segments = null;
        List<List<Line>> equispaced_lines = null;
        List<List<double>> equispaced_buffers = null;

        public Polyline buff_region = null;
        public List<BufferPoly> shortest_way = null;

        public IntersectWayCalculator(
            Polyline region,
            int main_index,
            double buffer,
            double room_buffer,
            List<PipeInput> pipe_inputs,
            List<List<Line>> equispaces_lines,
            List<List<double>> equispaced_buffers,
            List<List<Polyline>> equispaced_segments)
        {
            this.region = region;
            this.main_index = main_index;
            this.buffer = buffer;
            this.room_buffer = room_buffer;
            this.pipe_inputs = pipe_inputs;
            this.equispaced_segments = equispaced_segments;
            this.equispaced_lines = equispaces_lines;
            this.equispaced_buffers = equispaced_buffers;

            shortest_way = new List<BufferPoly>();
            for (int i = 0; i < pipe_inputs.Count; ++i)
                shortest_way.Add(new BufferPoly());

            buff_region = region.Buffer(-room_buffer - 0.25 * buffer).Cast<Polyline>().First();
        }

        public void Calculate(int index, bool turn_left)
        {
            // 第1阶段相交测试
            AdjustLastEquispacedSegment(index, turn_left);
            var buffer_polygon = GetBufferPolygon(index, turn_left);
            if (index == main_index)
            {
                var other_buffer_polygon = GetBufferPolygon(index, !turn_left);
                if (other_buffer_polygon != null)
                {
                    var inter = buffer_polygon.ToNTSPolygon().Intersection(other_buffer_polygon.ToNTSPolygon());
                    if(!(inter is Polygon)||inter.Area<10)
                    {
                        buffer_polygon.Dispose();
                        buffer_polygon = null;
                    }
                    else
                    {
                        buffer_polygon = (inter as Polygon).Shell.ToDbPolyline();
                    }
                    other_buffer_polygon.Dispose();
                }
            }
            var polygon_points = GetBufferPolyline(index, buffer_polygon, turn_left);
            //if (index == 6)
            //{
            //    if (buffer_polygon != null)
            //        PassageShowUtils.ShowEntity(buffer_polygon, 4);
            //    if (polygon_points != null)
            //        PassageShowUtils.ShowPoints(polygon_points);
            //    foreach (var poly in equispaced_segments[index])
            //        PassageShowUtils.ShowEntity(poly);
            //}
            ConvertToIntersectWay(index, polygon_points, turn_left);


            GetIntersectWayBuffer(index);
            // 第二阶段相交测试
            if (CheckIntersection(index, turn_left))
            {
                AdjustLastEquispacedSegment(index, turn_left, true);
                buffer_polygon = GetBufferPolygon(index, turn_left);
                if (index == main_index)
                {
                    var other_buffer_polygon = GetBufferPolygon(index, !turn_left);
                    if (other_buffer_polygon != null)
                    {
                        var inter = buffer_polygon.ToNTSPolygon().Intersection(other_buffer_polygon.ToNTSPolygon());
                        if (!(inter is Polygon) || inter.Area < 10)
                        {
                            buffer_polygon.Dispose();
                            buffer_polygon = null;
                        }
                        else
                        {
                            buffer_polygon = (inter as Polygon).Shell.ToDbPolyline();
                        }
                        other_buffer_polygon.Dispose();
                    }
                }
                polygon_points = GetBufferPolyline(index, buffer_polygon, turn_left);
                ConvertToIntersectWay(index, polygon_points, turn_left);
                GetIntersectWayBuffer(index);
            }
            // 第三阶段相交测试
            if (CheckIntersection(index, turn_left))
            {
                ToughFixIntersection(index, turn_left);
            }
            // 主导管线相交测试
            if (index == main_index && CheckIntersection(index, !turn_left))
            {
                ToughFixIntersectionMain(index, !turn_left);
            }
            if (buffer_polygon != null)
                buffer_polygon.Dispose();
        }

        private bool CheckIntersection(int index, bool turn_left)
        {
            if (turn_left && index > 0 || !turn_left && index < pipe_inputs.Count - 1)
            {
                var last_pipe = shortest_way[turn_left ? index - 1 : index + 1].Buffer();
                var cur_pipe = shortest_way[index].Buffer();
                if (cur_pipe.Intersects(last_pipe))
                    return true;
            }
            return false;
        }

        Polyline GetBufferPolygon(int index, bool turn_left)
        {
            Polyline intersect_region = null;
            if (pipe_inputs[index].in_near_wall || pipe_inputs[index].out_near_wall || index == 0 || index == pipe_inputs.Count - 1)
                intersect_region = AdjustBufferRoom(index);
            else
                intersect_region = buff_region.Clone() as Polyline;
            if (turn_left && index > 0 || !turn_left && index < pipe_inputs.Count - 1)
            {
                var last_way = shortest_way[turn_left ? index - 1 : index + 1];
                var last_way_poly = PassageWayUtils.BuildPolyline(last_way.poly);

                intersect_region = MainRegionCalculator.GetMainRegion(last_way_poly, intersect_region, !turn_left);
                var last_way_buffer = PassageWayUtils.BufferWithHole(last_way.Buffer(1), 0.75 * buffer).First();
                var diff = intersect_region.ToNTSPolygon().Difference(last_way_buffer.ToNTSPolygon()).ToDbCollection().Cast<Polyline>();
                last_way_buffer.Dispose();
                last_way_poly.Dispose();
                if (diff.Count() > 0)
                {
                    if (diff.Count() > 1)
                        diff = diff.OrderBy(o => o.Distance(pipe_inputs[index].pin)).ToList();
                    intersect_region = diff.First();
                }

                else
                    intersect_region = null;
            }

            return intersect_region;
        }
        List<Point3d> GetBufferPolyline(int index, Polyline buffer_polygon, bool turn_left)
        {
            if (buffer_polygon == null) return null;
            var points = PassageWayUtils.GetPolyPoints(buffer_polygon, true);
            points = SmoothUtils.SmoothPoints(points, 1e-3);
            // add first point
            var inter = IntersectUtils.PolylineIntersectionPolyline(equispaced_segments[index][0], buffer_polygon);
            HashSet<Point3d> move_points = new HashSet<Point3d>();
            if (inter.Count == 0)
            {
                if (equispaced_segments[index][0].NumberOfVertices > 2) return null;
                else
                {
                    double min_dis = double.MaxValue;
                    int near_index = -1;
                    points.RemoveAt(points.Count - 1);
                    for (int i = 0; i < points.Count; ++i)
                    {
                        var point = equispaced_segments[index][0].GetClosePoint(points[i]);
                        if (point.DistanceTo(points[i]) < min_dis)
                        {
                            min_dis = points[i].DistanceTo(point);
                            near_index = i;
                        }
                    }
                    var pre = (near_index - 1 + points.Count) % points.Count;
                    var next = (near_index + 1) % points.Count;
                    var dir = PassageWayUtils.GetDirBetweenTwoPoint(equispaced_segments[index][0].GetPoint3dAt(0), equispaced_segments[index][0].GetPoint3dAt(1));
                    var dir1 = PassageWayUtils.GetDirBetweenTwoPoint(points[pre], points[near_index]);
                    if (dir1 % 2 == dir % 2)
                    {
                        var near_point = equispaced_segments[index][0].GetClosePoint(points[near_index]);
                        var dp = near_point - points[near_index];
                        var pred1 = region.Contains(points[near_index] + dp);
                        var pred2 = region.Contains(points[pre] + dp);
                        if (pred1 && pred2)
                        {
                            points[near_index] += dp;
                            points[pre] += dp;
                            move_points.Add(points[near_index]);
                            move_points.Add(points[pre]);
                        }
                        else if (pred1)
                        {
                            points.Insert(near_index, points[near_index] + dp);
                            points.Insert(near_index, points[(near_index + 1) % points.Count]);
                        }
                        else if (pred2)
                        {
                            points.Insert(pre, points[pre] + dp);
                            points.Insert(pre, points[(pre + 1) % points.Count]);
                        }
                    }
                    else
                    {
                        var near_point = equispaced_segments[index][0].GetClosePoint(points[near_index]);
                        var dp = near_point - points[near_index];
                        var pred1 = region.Contains(points[near_index] + dp);
                        var pred2 = region.Contains(points[next] + dp);
                        if (pred1 && pred2)
                        {
                            points[near_index] += dp;
                            points[next] += dp;
                            move_points.Add(points[near_index]);
                            move_points.Add(points[next]);
                        }
                        else if (pred1)
                        {
                            points.Insert(near_index, points[near_index] + dp);
                            points.Insert(near_index, points[(near_index + 1) % points.Count]);
                        }
                        else if (pred2)
                        {
                            points.Insert(next, points[next] + dp);
                            points.Insert(next, points[(next + 1) % points.Count]);
                        }
                    }
                    points.Add(points[0]);
                    buffer_polygon = PassageWayUtils.BuildPolyline(points);
                    inter = IntersectUtils.PolylineIntersectionPolyline(equispaced_segments[index][0], buffer_polygon);
                }
            }
            var first_point = inter.FindByMin(o => o.DistanceTo(pipe_inputs[index].pin));
            if (PassageWayUtils.GetPointIndex(first_point, points, 1e-3) == -1)
                IntersectUtils.InsertPoint(first_point, ref points, 1e-3);
            // add last point
            var out_point = GetLastPoint(index);
            inter = IntersectUtils.PolylineIntersectionPolyline(equispaced_segments[index].Last(), buffer_polygon);
            Point3d last_point = Point3d.Origin;
            bool inter_with_last_seg = true;
            if (inter.Count == 0)
            {
                last_point = buffer_polygon.GetClosePoint(out_point);
                if (move_points.Contains(last_point))
                    return null;
                inter_with_last_seg = false;
            }
            else
            {
                last_point = inter.FindByMin(o => o.DistanceTo(out_point));
                if (PassageWayUtils.GetPointIndex(last_point, points, 1e-3) == -1)
                    IntersectUtils.InsertPoint(last_point, ref points, 1e-3);
            }
            points.RemoveAt(points.Count - 1);
            var start_index = PassageWayUtils.GetPointIndex(first_point, points, 1e-3);
            var end_index = PassageWayUtils.GetPointIndex(last_point, points, 1e-3);
            if (end_index == -1)
            {
                points.Add(points.Last());
                var new_last = MergeLastPolyline(equispaced_segments[index][equispaced_segments[index].Count - 2],
                                                equispaced_segments[index].Last(), pipe_inputs[index].end_dir);
                inter = IntersectUtils.PolylineIntersectionPolyline(new_last, buffer_polygon);
                new_last.Dispose();
                inter_with_last_seg = true;
                if (inter.Count == 0)
                {
                    last_point = buffer_polygon.GetClosePoint(out_point);
                    inter_with_last_seg = false;
                }
                else
                {
                    last_point = inter.FindByMin(o => o.DistanceTo(out_point));
                    if (PassageWayUtils.GetPointIndex(last_point, points, 1e-3) == -1)
                        IntersectUtils.InsertPoint(last_point, ref points, 1e-3);
                }
                points.RemoveAt(points.Count - 1);
                end_index = PassageWayUtils.GetPointIndex(last_point, points, 1e-3);
            }
            var polygon_points = new List<Point3d>();
            for (int i = start_index; i != end_index; i = turn_left ? (i + 1) % points.Count : (i - 1 + points.Count) % points.Count)
                polygon_points.Add(points[i]);
            polygon_points.Add(last_point);
            if (!inter_with_last_seg)
            {
                var line = new Line(equispaced_segments[index].Last().GetPoint3dAt(0), equispaced_segments[index].Last().GetPoint3dAt(1));
                last_point = line.GetClosestPointTo(last_point, true);
                if (!line.IsOnLine(last_point))
                {
                    var move_index = (line.StartPoint.DistanceTo(last_point) < line.EndPoint.DistanceTo(last_point)) ? 0 : 1;
                    equispaced_segments[index].Last().SetPointAt(move_index, last_point.ToPoint2D());
                }
                polygon_points.Add(last_point);
            }

            inter = IntersectUtils.PolylineIntersectionPolyline(equispaced_segments[index][0], equispaced_segments[index].Last());
            if (inter.Count > 0)
            {
                var p = inter.First();
                var is_start_closer_to_pin = polygon_points.First().DistanceTo(pipe_inputs[index].pin) <= p.DistanceTo(pipe_inputs[index].pin);
                var is_end_closer_to_pout = polygon_points.Last().DistanceTo(out_point) <= p.DistanceTo(out_point);
                var first_dis = polygon_points.First().DistanceTo(p);
                if (!is_start_closer_to_pin && !inter_with_last_seg && first_dis > 10) 
                {
                    return null;
                }

                if (!is_start_closer_to_pin && !is_end_closer_to_pout)
                    return null;
                else if (is_start_closer_to_pin && !is_end_closer_to_pout)
                    polygon_points.Add(p);
            }
            return SmoothUtils.SmoothPoints(polygon_points, 1e-3);
        }
        void ConvertToIntersectWay(int index, List<Point3d> polygon_points, bool turn_left)
        {
            if (polygon_points == null || polygon_points.Count == 1)
            {
                var last_point = GetLastPoint(index);
                polygon_points = PassageWayUtils.GetPolyPoints(equispaced_segments[index][0]);
                if (IntersectUtils.PolylineIntersectionPolyline(PassageWayUtils.BuildPolyline(new Line(polygon_points[polygon_points.Count - 2], polygon_points.Last())), equispaced_segments[index].Last()).Count > 0)
                {
                    if (pipe_inputs[index].end_dir % 2 == 0)
                        polygon_points[polygon_points.Count - 1] = new Point3d(polygon_points.Last().X, last_point.Y, 0);
                    else
                        polygon_points[polygon_points.Count - 1] = new Point3d(last_point.X, polygon_points.Last().Y, 0);
                }
                else
                {
                    var offset = GetLastOffset(index, turn_left);
                    var last2_point = last_point - Vector3d.XAxis.RotateBy(Math.PI / 2 * pipe_inputs[index].end_dir, Vector3d.ZAxis) * ((offset + 0.25) * buffer + room_buffer);
                    if (pipe_inputs[index].end_dir % 2 == 0)
                        polygon_points[polygon_points.Count - 1] = new Point3d(last2_point.X, polygon_points.Last().Y, 0);
                    else
                        polygon_points[polygon_points.Count - 1] = new Point3d(polygon_points.Last().X, last2_point.Y, 0);
                    polygon_points.Add(last2_point);
                    if (offset >= 1)
                    {
                        var point = polygon_points[polygon_points.Count - 2];
                        var line = new Line(pipe_inputs[turn_left ? index - 1 : index + 1].pout, pipe_inputs[index].pout);
                        point = line.GetClosestPointTo(point, true);
                        if (PassageWayUtils.PointOnSegment(point, line.StartPoint, line.EndPoint)) 
                        {
                            polygon_points.RemoveAt(polygon_points.Count - 1);
                            offset = 0;
                            last2_point = last_point - Vector3d.XAxis.RotateBy(Math.PI / 2 * pipe_inputs[index].end_dir, Vector3d.ZAxis) * ((offset + 0.25) * buffer + room_buffer);
                            if (pipe_inputs[index].end_dir % 2 == 0)
                                polygon_points[polygon_points.Count - 1] = new Point3d(last2_point.X, polygon_points.Last().Y, 0);
                            else
                                polygon_points[polygon_points.Count - 1] = new Point3d(polygon_points.Last().X, last2_point.Y, 0);
                            polygon_points.Add(last2_point);
                }
                    }
                }
                polygon_points.Add(last_point);
                polygon_points = SmoothUtils.SmoothPoints(polygon_points);
                var raw_polygon = PassageWayUtils.BuildPolyline(polygon_points);
                shortest_way[index].poly = PassageWayUtils.GetPolyPoints(IntersectUtils.PolylineIntersectionPolygon(raw_polygon, region));
                raw_polygon.Dispose();
                return;
            }
            // do intersect
            var target_points = new List<Point3d>();
            target_points.Add(pipe_inputs[index].pin);
            for (int i = 0; i < equispaced_segments[index].Count; ++i)
            {
                var polygon = PassageWayUtils.BuildPolyline(polygon_points);
                var inter_sets = IntersectUtils.PolylineIntersectionPolyline(equispaced_segments[index][i], polygon);
                if (inter_sets.Count == 0) continue;
                // update line points
                var line_points = PassageWayUtils.GetPolyPoints(equispaced_segments[index][i]);
                var line_set = inter_sets.Except(line_points);
                foreach (var point in line_set)
                    IntersectUtils.InsertPoint(point, ref line_points, 1e-3);
                // update polygon points
                var polygon_set = inter_sets.Except(polygon_points);
                foreach (var point in polygon_set)
                    IntersectUtils.InsertPoint(point, ref polygon_points, 1e-3);
                // calculate intersect points' index
                var inter_index_on_line = inter_sets.Select(o => PassageWayUtils.GetPointIndex(o, line_points, 1e-3)).OrderBy(o => o).ToList();
                var inter_index_on_polygon = inter_sets.Select(o => PassageWayUtils.GetPointIndex(o, polygon_points, 1e-3)).OrderBy(o => o).ToList();
                // add first seg
                if (i == 0)
                {
                    for (int j = 1; j < inter_index_on_line[0]; ++j)
                        target_points.Add(line_points[j]);
                }
                // add seg between equispaced segs
                else
                {
                    for (int j = 0; j < inter_index_on_polygon[0]; ++j)
                        target_points.Add(polygon_points[j]);
                }
                // add intersect seg 
                for (int j = 0; j < inter_sets.Count - 1; ++j)
                {
                    var cur = line_points[inter_index_on_line[j]];
                    target_points.Add(cur);
                    var line_next = line_points[inter_index_on_line[j] + 1];
                    var polygon_next = polygon_points[inter_index_on_polygon[j] + 1];
                    var line_dir = PassageWayUtils.GetDirBetweenTwoPoint(cur, line_next);
                    var polygon_dir = PassageWayUtils.GetDirBetweenTwoPoint(cur, polygon_next);
                    bool choose_polygon = turn_left ? (line_dir + 1) % 4 == polygon_dir : (line_dir + 3) % 4 == polygon_dir;
                    if (choose_polygon)
                    {
                        for (int k = inter_index_on_polygon[j] + 1; k < inter_index_on_polygon[j + 1]; ++k)
                            target_points.Add(polygon_points[k]);
                    }
                    else
                    {
                        for (int k = inter_index_on_line[j] + 1; k < inter_index_on_line[j + 1]; ++k)
                            target_points.Add(line_points[k]);
                    }
                }
                // calculate last polygon seg
                polygon_points.RemoveRange(0, inter_index_on_polygon.Last());
                polygon.Dispose();
                //calculate next equispaced seg
                line_points.RemoveRange(0, inter_index_on_line.Last());
                if (line_points.Count > 1 && i < equispaced_segments[index].Count - 1)
                {
                    var cur_last_line = new Line(line_points[line_points.Count - 2], line_points[line_points.Count - 1]);
                    var next_first_line = new Line(equispaced_segments[index][i + 1].GetPoint3dAt(0), equispaced_segments[index][i + 1].GetPoint3dAt(1));
                    var geo = cur_last_line.ToNTSLineString().Intersection(next_first_line.ToNTSLineString());
                    if (geo is Point point)
                    {
                        equispaced_segments[index][i + 1].SetPointAt(0, point.ToAcGePoint2d());
                        for (int t = line_points.Count - 2; t >= 0; --t)
                            equispaced_segments[index][i + 1].AddVertexAt(0, line_points[t].ToPoint2D(), 0, 0, 0);
                    }
                }
                if (i == equispaced_segments[index].Count - 1)
                {
                    target_points.AddRange(line_points);
                }

                if (polygon_points.Count == 1)
                    break;
            }
            var target_last_point = GetLastPoint(index);
            if (target_points.Last() != target_last_point)
            {
                // add last seg
                target_points.Add(polygon_points.Last());
                target_points.Add(target_last_point);
                var target_last2_point = target_points[target_points.Count - 2];
                if (target_last2_point.X != target_last_point.X && target_last2_point.Y != target_last_point.Y)
                {
                    if (pipe_inputs[index].end_dir % 2 == 0)
                        target_points.Insert(target_points.Count - 1, new Point3d(target_last2_point.X, target_last_point.Y, 0));
                    else
                        target_points.Insert(target_points.Count - 1, new Point3d(target_last_point.X, target_last2_point.Y, 0));
                }
            }
            target_points = SmoothUtils.SmoothPoints(target_points);
            SmoothUtils.RoundXY(ref target_points, false, false);
            for (int i = target_points.Count - 2; i > 0; --i)
            {
                if (!region.Contains(target_points[i]))
                    target_points.RemoveAt(i);
            }
            shortest_way[index].poly = target_points;
        }
        void GetIntersectWayBuffer(int index)
        {
            List<double> buff_list = new List<double>();
            var points = shortest_way[index].poly;
            for (int i = 0; i < points.Count - 1; ++i)
            {
                var dir = PassageWayUtils.GetDirBetweenTwoPoint(points[i], points[i + 1]);
                var p = points[i] + (points[i + 1] - points[i]) / 2;
                double buff = buffer / 4;
                for (int j = 0; j < equispaced_lines[index].Count; ++j)
                {
                    var p0 = equispaced_lines[index][j].StartPoint;
                    var p1 = equispaced_lines[index][j].EndPoint;
                    var line_dir = PassageWayUtils.GetDirBetweenTwoPoint(p0, p1);
                    if (dir % 2 == line_dir % 2)
                    {
                        var dis = equispaced_lines[index][j].GetClosestPointTo(p, false).DistanceTo(p);
                        if(PassageWayUtils.PointOnSegment(p, p0, p1) || dis < equispaced_buffers[index][j])
                        {
                            buff = equispaced_buffers[index][j];
                            break;
                        }

                    }
                }
                if (i == points.Count - 2 && points[i + 1].DistanceTo(pipe_inputs[index].pout) < 1)
                    buff = pipe_inputs[index].out_buffer;
                buff_list.Add(buff);
            }
            shortest_way[index].buff = buff_list;
            // smooth second seg
            var target_points = shortest_way[index].poly;
            var buffs = shortest_way[index].buff;
            if (target_points.Count >= 5)
            {
                var dp = target_points[1] - target_points[2];
                if (dp.Length + pipe_inputs[index].in_buffer < buffer / 4 || dp.Length < pipe_inputs[index].in_buffer)
                {
                    if (pipe_inputs[index].in_buffer > buffer / 4 - 10 && buffs[2] == buffer / 4) 
                    {
                        target_points[3] += dp;
                        target_points.RemoveAt(1);
                        target_points.RemoveAt(1);
                        buffs.RemoveAt(1);
                        buffs.RemoveAt(1);
                    }
                }
            }
            // smooth last seg
            if (target_points.Count >= 5)
            {
                var c = target_points.Count;
                var dp = target_points[c - 2] - target_points[c - 3];
                if (dp.Length + pipe_inputs[index].out_buffer < buffer / 4 || dp.Length < pipe_inputs[index].out_buffer || dp.Length <= buffer / 3)
                {
                    if (pipe_inputs[index].is_out_free && pipe_inputs[index].check(target_points[c - 1], buffer / 4, room_buffer))
                    {
                        target_points[c - 1] -= dp;
                        target_points.RemoveAt(c - 2);
                        target_points.RemoveAt(c - 3);
                        buffs.RemoveAt(c-2);
                        buffs.RemoveAt(c-3);
                    }
                    else if (buffs[c - 4] == buffer / 4) 
                    {
                        target_points[c - 4] += dp;
                        target_points.RemoveAt(c - 2);
                        target_points.RemoveAt(c - 3);
                        buffs.RemoveAt(c - 3);
                        buffs.RemoveAt(c - 4);
                    }
                }
            }
            shortest_way[index].poly = SmoothUtils.SmoothPoints(target_points);
        }
        Point3d GetLastPoint(int index)
        {
            return IntersectUtils.PolylineIntersectionPolyline(equispaced_segments[index].Last(), region).FindByMin(o => o.DistanceTo(pipe_inputs[index].pout));
        }
        int GetLastOffset(int index, bool turn_left)
        {
            int ret = 0;
            if (turn_left)
            {
                for (int i = index - 1; i >= 0; i--)
                {
                    if (pipe_inputs[i].end_offset == pipe_inputs[index].end_offset)
                    {
                        if (pipe_inputs[i].pout.DistanceTo(pipe_inputs[index].pout) <= buffer * (index - i))
                            ret++;
                        else break;
                    }
                    else
                        break;
                }
            }
            else
            {
                for (int i = index + 1; i < pipe_inputs.Count; ++i)
                {
                    if (pipe_inputs[i].end_offset == pipe_inputs[index].end_offset)
                    {
                        if (pipe_inputs[i].pout.DistanceTo(pipe_inputs[index].pout) <= buffer * (i - index))
                            ret++;
                        else break;
                    }
                    else
                        break;
                }
            }
            return ret;
        }
        Polyline AdjustBufferRoom(int index)
        {
            var points = PassageWayUtils.GetPolyPoints(region);
            var pout = pipe_inputs[index].pout;
            var out_index = PassageWayUtils.GetSegIndexOnPolygon(pipe_inputs[index].pout, points);
            if (pipe_inputs[index].in_near_wall || index == pipe_inputs.Count - 1 || index == 0)
            {
                var pre = PassageWayUtils.GetSegIndexOnPolygon(pipe_inputs[index].pin, points);
                var next = (pre + 1) % points.Count;
                if (pipe_inputs[index].pin.DistanceTo(points[pre]) <= room_buffer + pipe_inputs[index].in_buffer + 1)
                {
                    var ppre = (pre - 1 + points.Count) % points.Count;
                    var old_pre_point = points[pre];
                    var dir = PassageWayUtils.GetDirBetweenTwoPoint(points[pre], points[ppre]);
                    if (dir == pipe_inputs[index].start_dir)
                    {
                        points[pre] = pipe_inputs[index].pin + (old_pre_point - pipe_inputs[index].pin).GetNormal() * (buffer / 4 + room_buffer);
                        points[ppre] += (points[pre] - old_pre_point);
                        if (ppre == out_index)
                        {
                            pout += (points[pre] - old_pre_point);
                        }
                    }
                }
                else if (pipe_inputs[index].pin.DistanceTo(points[next]) <= room_buffer + pipe_inputs[index].in_buffer + 1)
                {
                    var nnext = (next + 1) % points.Count;
                    var old_next_point = points[next];
                    var dir = PassageWayUtils.GetDirBetweenTwoPoint(points[next], points[nnext]);
                    if (dir == pipe_inputs[index].start_dir)
                    {
                        points[next] = pipe_inputs[index].pin + (old_next_point - pipe_inputs[index].pin).GetNormal() * (buffer / 4 + room_buffer);
                        points[nnext] += (points[next] - old_next_point);
                    }
                    if (next == out_index)
                    {
                        pout += (points[next] - old_next_point);
                    }
                }
            }
            if (pipe_inputs[index].out_near_wall)
            {
                var pre = PassageWayUtils.GetSegIndexOnPolygon(pout, points);
                var next = (pre + 1) % points.Count;
                if (pout.DistanceTo(points[pre]) <= room_buffer + pipe_inputs[index].out_buffer + 1)
                {
                    var ppre = (pre - 1 + points.Count) % points.Count;
                    var old_pre_point = points[pre];
                    var dir = PassageWayUtils.GetDirBetweenTwoPoint(points[ppre], points[pre]);
                    if (dir == pipe_inputs[index].end_dir)
                    {
                        points[pre] = pout + (old_pre_point - pout).GetNormal() * (buffer / 4 + room_buffer);
                        points[ppre] += (points[pre] - old_pre_point);
                    }
                }
                else if (pout.DistanceTo(points[next]) <= room_buffer + pipe_inputs[index].out_buffer + 1)
                {
                    var nnext = (next + 1) % points.Count;
                    var old_next_point = points[next];
                    var dir = PassageWayUtils.GetDirBetweenTwoPoint(points[nnext], points[next]);
                    if (dir == pipe_inputs[index].end_dir)
                    {
                        points[next] = pout + (old_next_point - pout).GetNormal() * (buffer / 4 + room_buffer);
                        points[nnext] += (points[next] - old_next_point);
                    }
                }
            }
            points.Add(points.First());
            var new_region = PassageWayUtils.BuildPolyline(points);
            var ret = new_region.Buffer(-room_buffer - buffer / 4).Cast<Polyline>().First();
            new_region.Dispose();
            return ret;
        }
        void AdjustLastEquispacedSegment(int index, bool turn_left, bool fix = false)
        {
            Polyline last_pipe = null;
            if (turn_left && index > 0 || !turn_left && index < pipe_inputs.Count - 1)
            {
                if (!fix)
                {
                    last_pipe = shortest_way[turn_left ? index - 1 : index + 1].Buffer();
                }
                else
                {
                    last_pipe = shortest_way[turn_left ? index - 1 : index + 1].Buffer(4);
                    last_pipe = PassageWayUtils.Buffer(last_pipe, -10).First();
                }
            }
            if (last_pipe == null) return;
            // adjust equispcaced segment last
            var last_point = equispaced_segments[index].Last().GetClosestPointTo(pipe_inputs[index].pout, false);
            var last_lines = equispaced_segments[index].Last().ToNTSLineString().Difference(last_pipe.ToNTSPolygon()).ToDbCollection().Cast<Polyline>().ToList();
            if (last_lines.Count > 0)
            {
                var last_line = last_lines.Find(o => o.StartPoint.DistanceTo(last_point) < 2 || o.EndPoint.DistanceTo(last_point) < 2);
                if (last_line == null) return;
                equispaced_segments[index][equispaced_segments[index].Count - 1].Dispose();
                equispaced_segments[index][equispaced_segments[index].Count - 1] = last_line;
            }
        }
        void ToughFixIntersection(int index, bool turn_left)
        {
            if (pipe_inputs[index].is_out_free == false) return;
            if (turn_left && index > 0 || !turn_left && index < pipe_inputs.Count - 1)
            {
                var last_pipe = shortest_way[turn_left ? index - 1 : index + 1].Buffer(4);
                last_pipe = PassageWayUtils.Buffer(last_pipe, -10).First();
                int i = 0;
                for (; i < shortest_way[index].poly.Count; ++i)
                {
                    if (last_pipe.Contains(shortest_way[index].poly[i]))
                        break;
                }
                if (i > 1)
                {
                    var points = PassageWayUtils.GetPolyPoints(region);
                    var pre = PassageWayUtils.GetSegIndexOnPolygon(shortest_way[index].poly.Last(), points);
                    var next = (pre + 1) % points.Count;
                    var line = new Line(points[pre], points[next]);
                    var point = line.GetClosestPointTo(shortest_way[index].poly[i - 1], false);
                    if (pipe_inputs[index].check(point, shortest_way[index].buff[i - 2], room_buffer))
                    {
                        shortest_way[index].poly[i - 1] = point;
                        shortest_way[index].poly = shortest_way[index].poly.GetRange(0, i);
                        shortest_way[index].buff = shortest_way[index].buff.GetRange(0, i - 1);
                    }

                }
            }
        }
        void ToughFixIntersectionMain(int index, bool turn_left)
        {
            if (turn_left && index > 0 || !turn_left && index < pipe_inputs.Count - 1)
            {
                var last_index = turn_left ? index - 1 : index + 1;
                if (pipe_inputs[last_index].is_out_free == false)
                {
                    var cl = shortest_way[last_index].poly.Count;
                    var pl0 = shortest_way[last_index].poly[cl - 2];
                    var pl1 = shortest_way[last_index].poly[cl - 1];
                    var c = shortest_way[index].poly.Count;
                    var p0 = shortest_way[index].poly[c - 2];
                    var p1 = shortest_way[index].poly[c - 1];
                    var dp = (pl0 - pl1).GetNormal() * (p0.DistanceTo(p1) + buffer - pl0.DistanceTo(pl1));
                    shortest_way[last_index].poly[cl - 2] += dp;
                    shortest_way[last_index].poly[cl - 3] += dp;
                    return;
                }
                var main_pipe = shortest_way[index].Buffer(4);
                main_pipe = PassageWayUtils.Buffer(main_pipe, -10).First();
                int i = 0;
                for (; i < shortest_way[last_index].poly.Count; ++i)
                {
                    if (main_pipe.Contains(shortest_way[last_index].poly[i]))
                        break;
                }
                if (i > 1)
                {
                    var points = PassageWayUtils.GetPolyPoints(region);
                    var pre = PassageWayUtils.GetSegIndexOnPolygon(shortest_way[last_index].poly.Last(), points);
                    var next = (pre + 1) % points.Count;
                    var line = new Line(points[pre], points[next]);
                    var point = line.GetClosestPointTo(shortest_way[last_index].poly[i - 1], false);
                    if (pipe_inputs[last_index].check(point, shortest_way[last_index].buff[i - 2], room_buffer))
                    {
                        shortest_way[last_index].poly[i - 1] = point;
                        shortest_way[last_index].poly = shortest_way[last_index].poly.GetRange(0, i);
                        shortest_way[last_index].buff = shortest_way[last_index].buff.GetRange(0, i - 1);
                    }
                }
            }
        }
        Polyline MergeLastPolyline(Polyline a,Polyline b,int dir)
        {
            var apts = PassageWayUtils.GetPolyPoints(a);
            var bpts = PassageWayUtils.GetPolyPoints(b);
            if (dir % 2 == 0)
            {
                apts[apts.Count - 1] = new Point3d(apts.Last().X, bpts.Last().Y, 0);
            }
            else
                apts[apts.Count - 1] = new Point3d(bpts.Last().X, apts.Last().Y, 0);
            apts.Add(bpts.Last());
            return PassageWayUtils.BuildPolyline(apts);
        }

        public void ShowResult()
        {
            for (int i = 0; i < shortest_way.Count; ++i)
            {
                PassageShowUtils.ShowEntity(PassageWayUtils.BuildPolyline(shortest_way[i].poly), i % 7 + 1);
            }
        }
    }
}
