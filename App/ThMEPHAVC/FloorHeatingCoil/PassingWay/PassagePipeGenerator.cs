using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPHVAC.FloorHeatingCoil.PassageWay
{
    class DirTreeNode
    {
        public int dir;
        public int left_idx, right_idx;
        public DirTreeNode left = null;
        public DirTreeNode right = null;
        public DirTreeNode(int dir,int left,int right) 
        { 
            this.dir = dir;
            this.left_idx = left;
            this.right_idx = right;
        }
    }
    class PassagePipeGenerator
    {
        // input
        Polyline region;
        double buffer = -500;
        double pipe_width = 50;
        List<PipeInput> pipe_inputs { get; set; } = new List<PipeInput>();
        int main_index;
        // output
        public List<Polyline> skeleton = new List<Polyline>();

        List<RectBox> boxes { get; set; }
        int[,] boxmap { set; get; }
        double[,] distmap { set; get; }
        List<List<PipeSegment>> pipe_segments { get; set; }
        DirTreeNode root { get; set; }
        int start_dir { get; set; }
        List<int> end_dirs { get; set; }
        List<BufferPoly> shortest_way { get; set; }

        public PassagePipeGenerator(Polyline region, List<Point3d> pipe_in, List<Point3d> pipe_out, List<double> in_buffers,List<double> out_buffers,int main_index = 6, double buffer = -500)
        {
            this.region = region;
            for (int i = 0; i < pipe_in.Count; ++i)
                pipe_inputs.Add(new PipeInput(pipe_in[i], pipe_out[i], in_buffers[i], out_buffers[i]));

            this.buffer = buffer;
            this.main_index = main_index;
        }
        public void CalculatePipeline()
        {
            BuildGraph();
            pipe_segments = new List<List<PipeSegment>>();
            end_dirs = new List<int>();
            for (int i = 0; i < pipe_inputs.Count; ++i)
            {
                var box_path = FindShortestBoxWay(pipe_inputs[i], i);
                pipe_segments.Add(ConvertToDirWay(box_path, i));
                //if(i==3)
                //{
                //    ShowBoxWay(box_path);
                //    ShowSegmentGate(pipe_segments.Last());
                //}    
            }
            root = BuildDirTree(0, pipe_segments.Count - 1, 0);
            PassageShowUtils.PrintMassage("%%%%%%%%%%%%实验%%%%%%%%%%%%");
            for (int i = 0; i < pipe_inputs.Count; ++i)
                ShowDirWay(i);
            shortest_way = new List<BufferPoly>();
            for (int i = 0; i < pipe_inputs.Count; ++i)
            {
                //if (i != 3) continue;
                shortest_way.Add(ConvertToShortestWay(i));
                //if (i == main_index) continue;
                //var pipe = shortest_way[i].Buffer();
                //pipe.ColorIndex = 3;
                //pipe.ColorIndex = i % 7 + 1;
                //skeleton.Add(pipe);
                PassageShowUtils.ShowEntity(shortest_way[i].poly);
            }
            for(int i = 0; i < pipe_inputs.Count; ++i)
            {
                if(i==1)
                    ConvertToIntersectWay(i);
            }
            //ConvertMainWay();
        }
        void BuildGraph()
        {
            // get all x and y
            HashSet<double> xs = new HashSet<double>();
            HashSet<double> ys = new HashSet<double>();
            var points = region.GetPoints().ToList();
            for(int i = 0; i < points.Count-1; ++i)
            {
                xs.Add((int)points[i].X);
                ys.Add((int)points[i].Y);
            }
            var x = xs.ToList();x.Sort();
            var y = ys.ToList();y.Sort();
            // init graph
            boxes = new List<RectBox>();
            boxmap = new int[y.Count - 1, x.Count - 1];
            for (int i = 0; i < y.Count - 1; ++i)
                for (int j = 0; j < x.Count - 1; ++j)
                    boxmap[i, j] = -1;
            for (int i = 0; i < y.Count - 1; i++)
            {
                for (int j = 0; j < x.Count - 1; j++) 
                {
                    var center = new Point3d((x[j] + x[j + 1]) / 2, (y[i] + y[i + 1]) / 2, 0);
                    if (region.Contains(center))
                    {
                        boxmap[i, j] = boxes.Count;
                        boxes.Add(new RectBox(x[j], x[j + 1], y[i], y[i + 1], i, j));
                    }  
                }
            }
            // init dist map
            int n = boxes.Count;
            distmap = new double[n, n];
            for (int i = 0; i < n; ++i)
                for (int j = 0; j < n; ++j)
                    distmap[i, j] = boxes[i].DistanceTo(boxes[j]);
        }
        List<int> FindShortestBoxWay(PipeInput pipe, int index)
        {
            int s = FindBoxPointIn(pipe.pin);
            int t = FindBoxPointIn(pipe.pout);
            // calculate in's dir
            if (Math.Abs(pipe.pin.X - boxes[s].xmin) < 1)
                start_dir = 0;
            else if (Math.Abs(pipe.pin.X - boxes[s].xmax) < 1)
                start_dir = 2;
            else if (Math.Abs(pipe.pin.Y - boxes[s].ymin) < 1) 
                start_dir = 1;
            else
                start_dir = 3;
            // calculate out's dir
            int end_dir;
            if (Math.Abs(pipe.pout.X - boxes[t].xmin) < 1)
                end_dir = 2;
            else if (Math.Abs(pipe.pout.X - boxes[t].xmax) < 1)
                end_dir = 0;
            else if (Math.Abs(pipe.pout.Y - boxes[t].ymin) < 1)
                end_dir = 3;
            else
                end_dir = 1;
            end_dirs.Add(end_dir);
            int n = boxes.Count;
            // init arrays
            double[] dist = new double[n];
            int[] path = new int[n];
            bool[] used = new bool[n];
            int[] dirs = new int[n];
            for (int i = 0; i < n; ++i)
            {
                dist[i] = 120000;
                path[i] = -1;
                used[i] = false;
                dirs[i] = 100;
            }
            // init start box
            dist[s] = 1;
            path[s] = s;
            dirs[s] = 0;
            // calculate target box path with dijkstra
            for(int i = 0; i < n; ++i)
            {
                int x = -1;
                for(int y = 0; y < n; ++y)
                {
                    if (!used[y] && (x == -1 || dist[y] < dist[x]))
                        x = y;
                }
                used[x] = true;
                for(int y = 0; y < n; ++y)
                {
                    if (used[y]) continue;
                    if (dist[x] + distmap[x, y] <= dist[y])
                    {
                        // update dist
                        dist[y] = dist[x] + distmap[x, y];
                        // calculate <path[x]-x-y>'s dirs
                        var pre_dir = path[x] == x ? start_dir : GetDirBetweenTwoBox(boxes[path[x]], boxes[x]);
                        var cur_dir = GetDirBetweenTwoBox(boxes[x], boxes[y]);
                        int new_dirs = dirs[x] + (Math.Abs(cur_dir - pre_dir) % 2 != 0 ? 1 : 0);
                        // update dirs and path
                        if(dist[x]+distmap[x,y]<dist[y]||new_dirs<dirs[y])
                        {
                            dirs[y] = new_dirs;
                            path[y] = x;
                        }
                    }
                }
            }
            // get path idxs
            int c = t;
            List<int> path_idx = new List<int>();
            while (path[c] != c)
            {
                path_idx.Add(c);
                c = path[c];
            }
            path_idx.Add(s);
            path_idx.Reverse();
            return path_idx;
        }
        List<PipeSegment> ConvertToDirWay(List<int> box_path,int index)
        {
            // group index
            int length = box_path.Count;
            List<PipeSegment> ret = new List<PipeSegment>();
            List<KeyValuePair<int, List<int>>> dir_groups = new List<KeyValuePair<int, List<int>>>();
            int i = 1;
            while (i<box_path.Count)
            {
                var dir = GetDirBetweenTwoBox(boxes[box_path[i - 1]], boxes[box_path[i]]);
                dir_groups.Add(new KeyValuePair<int, List<int>>(dir, new List<int>()));
                dir_groups.Last().Value.Add(box_path[i - 1]);
                while (i < box_path.Count)
                {
                    var cur_dir= GetDirBetweenTwoBox(boxes[box_path[i - 1]], boxes[box_path[i]]);
                    if (cur_dir != dir)
                        break;
                    dir_groups.Last().Value.Add(box_path[i]);
                    i++;
                }
            }
            // convert to pipe segment
            for(i = 0; i < dir_groups.Count; ++i)
            {
                PipeSegment ps = new PipeSegment();
                ps.dir = dir_groups[i].Key;
                // calculate start and end
                switch (ps.dir)
                {
                    case 0: ps.end = boxes[dir_groups[i].Value.Last()].xmax; ps.start = boxes[dir_groups[i].Value.First()].xmin; break;
                    case 1: ps.end = boxes[dir_groups[i].Value.Last()].ymax; ps.start = boxes[dir_groups[i].Value.First()].ymin; break;
                    case 2: ps.end = boxes[dir_groups[i].Value.Last()].xmin; ps.start = boxes[dir_groups[i].Value.First()].xmax; break;
                    case 3: ps.end = boxes[dir_groups[i].Value.Last()].ymin; ps.start = boxes[dir_groups[i].Value.First()].ymax; break;
                }
                var box_idxs = dir_groups[i].Value;
                // calculate min and max
                if (ps.dir % 2 == 0)// left or right
                {
                    // down boundary
                    int current_row = boxes[box_idxs[0]].i;
                    bool flag = true;
                    while (flag)
                    {
                        var ymin = double.MinValue;
                        for(int j = 0; j < box_idxs.Count; ++j)
                        {
                            var col = boxes[box_idxs[j]].j;
                            if (boxmap[current_row, col] == -1)
                                flag = false;
                            else
                                ymin = Math.Max(ymin, boxes[boxmap[current_row, col]].ymin);
                        }
                        if (flag)
                        {
                            current_row -= 1;
                            ps.min = ymin;
                        }
                        if (current_row < 0)
                            break;
                    }
                    // upper boundary
                    current_row = boxes[box_idxs[0]].i;
                    flag = true;
                    while (flag)
                    {
                        var ymax = double.MaxValue;
                        for (int j = 0; j < box_idxs.Count; ++j)
                        {
                            var col = boxes[box_idxs[j]].j;
                            if (boxmap[current_row, col] == -1)
                                flag = false;
                            else
                                ymax = Math.Min(ymax, boxes[boxmap[current_row, col]].ymax);
                        }
                        if (flag)
                        {
                            current_row += 1;
                            ps.max = ymax;
                        }
                        if (current_row >= boxmap.Rank)  
                            break;
                    }
                }
                else// up or down 
                {
                    // left boundary
                    int current_col = boxes[box_idxs[0]].j;
                    bool flag = true;
                    while (flag)
                    {
                        var xmin = double.MinValue;
                        for (int j = 0; j < box_idxs.Count; ++j)
                        {
                            var row = boxes[box_idxs[j]].i;
                            if (boxmap[row, current_col] == -1)
                                flag = false;
                            else
                                xmin = Math.Max(xmin, boxes[boxmap[row, current_col]].xmin);
                        }
                        if (flag)
                        {
                            current_col -= 1;
                            ps.min = xmin;
                        }
                        if (current_col < 0)
                            break;
                    }
                    // upper boundary
                    current_col = boxes[box_idxs[0]].j;
                    flag = true;
                    while (flag)
                    {
                        var xmax = double.MaxValue;
                        for (int j = 0; j < box_idxs.Count; ++j)
                        {
                            var row = boxes[box_idxs[j]].i;
                            if (boxmap[row, current_col] == -1)
                                flag = false;
                            else
                                xmax = Math.Min(xmax, boxes[boxmap[row, current_col]].xmax);
                        }
                        if (flag)
                        {
                            current_col += 1;
                            ps.max = xmax;
                        }
                        if (current_col >= boxmap.GetLength(1)) 
                            break;
                    }
                }
                ret.Add(ps);
            }
            // update start and end
            for (i = 0; i < ret.Count - 1; ++i)
            {
                if (ret[i].dir < 2)
                {
                    ret[i].start = Math.Max(ret[i].start, ret[i + 1].min);
                    ret[i].end = Math.Min(ret[i].end, ret[i + 1].max);
                }
                else
                {
                    ret[i].start = Math.Min(ret[i].start, ret[i + 1].max);
                    ret[i].end = Math.Max(ret[i].end, ret[i + 1].min);
                }
            }
            // update first segment
            if (box_path.Count == 1)
            {
                ret.Add(new PipeSegment());
                ret[0].dir = start_dir;
                switch (start_dir)
                {
                    case 0: ret[0].start = boxes[box_path[0]].xmin; ret[0].end = boxes[box_path[0]].xmax; break;
                    case 1: ret[0].start = boxes[box_path[0]].ymin; ret[0].end = boxes[box_path[0]].ymax; break;
                    case 2: ret[0].start = boxes[box_path[0]].xmax; ret[0].end = boxes[box_path[0]].xmin; break;
                    case 3: ret[0].start = boxes[box_path[0]].ymax; ret[0].end = boxes[box_path[0]].ymin; break;
                }
            }
            if (ret[0].dir != start_dir)
            {
                ret.Insert(0, new PipeSegment());
                ret[0].dir = start_dir;
                ret[0].start = start_dir <= 1 ? ret[1].min : ret[1].max;
                ret[0].end = start_dir <= 1 ? ret[1].max : ret[1].min;
            }
            //// update end segment
            //if (ret.Count == 1)
            //{
            //    ret.Add(new PipeSegment());
            //    if (start_dir % 2 == 0)
            //    {
            //        if (pipe_inputs[index].pout.Y == pipe_inputs[index].pin.Y)
            //            ret.RemoveAt(1);
            //        else
            //            ret[1].dir = pipe_inputs[index].pout.Y > pipe_inputs[index].pin.Y ? 1 : 3;
            //    }
            //    else
            //    {
            //        if (pipe_inputs[index].pout.X == pipe_inputs[index].pin.Y)
            //            ret.RemoveAt(1);
            //        else
            //            ret[1].dir = pipe_inputs[index].pout.X > pipe_inputs[index].pin.X ? 0 : 2;
            //    }
            //}
            return ret;
        }
        DirTreeNode BuildDirTree(int left, int right, int depth, bool side = true)
        {
            if (right < left) return null;
            var dir = pipe_segments[left][depth].dir;
            var node = new DirTreeNode(dir, left, right);
            if (depth == 0)
            {
                for(int i=left;i<=right;++i)
                {
                    pipe_segments[i][0].offset = i <= main_index ? (i - left) : (right - i);
                    pipe_segments[i][0].buffer_turn = i <= main_index;
                    pipe_segments[i][0].dw = buffer / 2;
                }
            }
            if (depth > 0)
            {
                // check if node contain main pipe
                var pipe_num = right - left + 1;
                int mid_index = right >= main_index && left <= main_index ? main_index : (right + left) / 2;
                //if (right - mid_index - (mid_index - left + 1) == -2)
                //    mid_index--;
                for (int i = left; i <= right; ++i)
                {
                    // set side
                    pipe_segments[i][depth].side = side;
                    // set close_to
                    pipe_segments[i][depth].close_to = side ^ (i <= mid_index);
                    // set offset
                    pipe_segments[i][depth].offset = i <= mid_index ? (i - left) : (right - i);
                    // set buffer turn's direction
                    pipe_segments[i][depth].buffer_turn = i <= mid_index;
                    // set buffer
                    var s = pipe_segments[i][depth - 1].start;
                    var e = pipe_segments[i][depth - 1].end;
                    var half_dw = (e - s) / (pipe_num * 2 + 1);
                    if (Math.Abs(half_dw) > 0.5 * Math.Abs(buffer))
                        half_dw = Math.Sign(half_dw) * 0.5 * Math.Abs(buffer);
                    pipe_segments[i][depth].dw = half_dw;
                }
            }
            // calculate child'dir
            var left_dir = (dir + 1) % 4;
            var right_dir = (dir - 1 + 4) % 4;
            // next depth has shape like XXXXLLLLXXXXRRRRXXXX
            // calculate LLLL's start and end
            int start_idx = left;
            while (start_idx <= right && pipe_segments[start_idx].Count <= depth + 1) 
                start_idx++;
            int end_idx = start_idx;
            while (end_idx <= right && pipe_segments[end_idx].Count > depth + 1 && pipe_segments[end_idx][depth + 1].dir == left_dir)
                end_idx++;
            node.left = BuildDirTree(start_idx, end_idx - 1, depth + 1, true);
            // calculate RRRR's start and end
            start_idx = end_idx;
            while (start_idx <= right && pipe_segments[start_idx].Count <= depth + 1) 
                start_idx++;
            end_idx = start_idx;
            while (end_idx <= right && pipe_segments[end_idx].Count > depth + 1 && pipe_segments[end_idx][depth + 1].dir == right_dir)
                end_idx++;
            node.right = BuildDirTree(start_idx, end_idx - 1, depth + 1, false);
            return node;
        }
        BufferPoly ConvertToShortestWay(int index)
        {
            Polyline way = new Polyline();
            way.AddVertexAt(0, pipe_inputs[index].pin.ToPoint2D(), 0, 0, 0);
            List<double> buffers = new List<double>();
            buffers.Add(pipe_inputs[index].in_buffer);
            bool is_equal_end = pipe_segments[index].Last().dir == end_dirs[index];
            for(int i = 1; i < pipe_segments[index].Count; ++i)
            {
                // for convience
                var dir = pipe_segments[index][i - 1].dir;
                var s = pipe_segments[index][i - 1].start;
                var e = pipe_segments[index][i - 1].end;
                // calculate simple axis
                double x = 0, y = 0;
                if (dir % 2 == 0) 
                    y = way.GetPoint2dAt(i-1).Y;
                else
                    x = way.GetPoint2dAt(i-1).X;
                // calculate hard axis
                double new_xy = 0;
                if (pipe_segments[index][i].close_to)
                    new_xy = e - pipe_segments[index][i].dw * (pipe_segments[index][i].offset * 2 + 1.5);
                else
                    new_xy = s + pipe_segments[index][i].dw * (pipe_segments[index][i].offset * 2 + 1.5);
                if (dir % 2 == 0)
                    x = new_xy;
                else
                    y = new_xy;
                way.AddVertexAt(i, new Point2d(x, y), 0, 0, 0);
                buffers.Add(Math.Abs(pipe_segments[index][i].dw / 2));
            }
            // calculate last segment
            var out_point = pipe_inputs[index].pout;
            var last_point = way.EndPoint;
            var last_seg = pipe_segments[index].Last();
            if (!is_equal_end)
            {
                if (last_seg.dir % 2 == 0)
                    way.AddVertexAt(way.NumberOfVertices, new Point2d(out_point.X, last_point.Y), 0, 0, 0);
                else
                    way.AddVertexAt(way.NumberOfVertices, new Point2d(last_point.X, out_point.Y), 0, 0, 0);
                buffers.Add(pipe_inputs[index].out_buffer);
            }
            else
            {
                var max_buff = buffers.Max();
                var new_xy = last_seg.end - Math.Sign(last_seg.end - last_seg.start) * (max_buff*4) * (last_seg.offset + 0.75);
                if (last_seg.dir % 2 == 0)
                {
                    var p0 = new Point2d(new_xy, last_point.Y);
                    var p1 = new Point2d(p0.X, out_point.Y);
                    if (p0.GetDistanceTo(p1) < 50)
                    {
                        way.SetPointAt(way.NumberOfVertices - 1, new Point2d(last_point.X, out_point.Y));
                        buffers[buffers.Count - 1] = Math.Min(buffers.Last(), pipe_inputs[index].out_buffer);
                    }
                    else
                    {
                        way.AddVertexAt(way.NumberOfVertices, p0, 0, 0, 0);
                        way.AddVertexAt(way.NumberOfVertices, p1, 0, 0, 0);
                        buffers.Add(buffers.Last());
                        buffers.Add(pipe_inputs[index].out_buffer);
                    }
                }
                else
                {
                    var p0 = new Point2d(last_point.X, new_xy);
                    var p1 = new Point2d(out_point.X, p0.Y);
                    if (p0.GetDistanceTo(p1) < 50)
                    {
                        way.SetPointAt(way.NumberOfVertices - 1, new Point2d(out_point.X, last_point.Y));
                        buffers[buffers.Count - 1] = Math.Min(buffers.Last(), pipe_inputs[index].out_buffer);
                    }
                    else
                    {
                        way.AddVertexAt(way.NumberOfVertices, p0, 0, 0, 0);
                        way.AddVertexAt(way.NumberOfVertices, p1, 0, 0, 0);
                        buffers.Add(buffers.Last());
                        buffers.Add(pipe_inputs[index].out_buffer);
                    }
                }
            }
            way.AddVertexAt(way.NumberOfVertices, pipe_inputs[index].pout.ToPoint2D(), 0, 0, 0);
            return new BufferPoly(way, buffers);
        }
        //void ConvertToIntersectWay(int index)
        //{
        //    Polyline intersect_way = new Polyline();
        //    List<double> intersect_buffers = new List<double>();
        //    if (pipe_segments[index].Count == 1)
        //    {
        //        // get current seg
        //        var cur_seg = shortest_way[index].poly;
        //        var buffers = shortest_way[index].buff;
        //        // get current buffer
        //        var buff_dist = -2 * Math.Abs(pipe_segments[index][0].dw) * (pipe_segments[index][0].offset + 0.75);
        //        var buff_poly = PassageWayUtils.Buffer(region, buff_dist);
        //        // do intersect
        //        bool turn_left = pipe_segments[index][0].buffer_turn;
        //        PassageShowUtils.ShowEntity(cur_seg, turn_left ? 2 : 3);
        //        intersect_way = buff_poly.Count != 1 ? cur_seg : NomalPipeIntersector.IntersectWithBuffer(cur_seg, buff_poly[0], ref buffers, Math.Abs(pipe_segments[index][0].dw) / 2, turn_left);
        //        for (int i = 0; i < buffers.Count; ++i)
        //            intersect_buffers.Add(buffers[i]);
        //    }
        //    else
        //    {
        //        for (int i = 1; i < pipe_segments[index].Count; ++i)
        //        {
        //            // get current seg
        //            Polyline cur_seg = new Polyline();
        //            List<double> buffers = new List<double>();
        //            if (i < pipe_segments[index].Count - 1)
        //            {
        //                cur_seg.AddVertexAt(0, shortest_way[index].poly.GetPoint2dAt(i - 1), 0, 0, 0);
        //                cur_seg.AddVertexAt(1, shortest_way[index].poly.GetPoint2dAt(i), 0, 0, 0);
        //                buffers.Add(shortest_way[index].buff[i - 1]);
        //            }
        //            else
        //            {
        //                for (int j = i - 1; j < shortest_way[index].poly.NumberOfVertices; ++j)
        //                    cur_seg.AddVertexAt(j - i + 1, shortest_way[index].poly.GetPoint2dAt(j), 0, 0, 0);
        //                buffers.AddRange(shortest_way[index].buff.GetRange(i - 1, cur_seg.NumberOfVertices - 1));
        //            }
        //            // get current buffer
        //            var buff_dist = -2 * Math.Abs(pipe_segments[index][i].dw) * (pipe_segments[index][i - 1].offset + 0.75);
        //            var buff_poly = PassageWayUtils.Buffer(region, buff_dist);
        //            // do intersect
        //            bool turn_left = pipe_segments[index][i].buffer_turn;
        //            PassageShowUtils.ShowEntity(cur_seg, turn_left ? 2 : 3);
        //            Polyline target_way = buff_poly.Count != 1 ? cur_seg : NomalPipeIntersector.IntersectWithBuffer(cur_seg, buff_poly[0], ref buffers, Math.Abs(pipe_segments[index][i].dw) / 2, turn_left);
        //            // add to way
        //            for (int j = 0; j < target_way.NumberOfVertices - 1; ++j)
        //            {
        //                intersect_way.AddVertexAt(intersect_way.NumberOfVertices, target_way.GetPoint2dAt(j), 0, 0, 0);
        //                intersect_buffers.Add(buffers[j]);
        //            }
        //        }
        //        intersect_way.AddVertexAt(intersect_way.NumberOfVertices, pipe_inputs[index].pout.ToPoint2D(), 0, 0, 0);
        //    }
        //    intersect_way = NomalPipeIntersector.SmoothPolyline(intersect_way, ref intersect_buffers);
        //    shortest_way[index].Dispose();
        //    shortest_way[index] = new BufferPoly(intersect_way, intersect_buffers);
        //}
        void ConvertToIntersectWay(int index)
        {
            // init region
            var points = PassageWayUtils.GetPolyPoints(region);
            points.Add(points.First());
            Polyline intersect_region = PassageWayUtils.BuildPolyline(points);
            DBObjectCollection rest = new DBObjectCollection();
            if (index > 0 && index < main_index)
                intersect_region = MainPipeIntersector.GetMainRegion(shortest_way[index - 1].poly, region, false);
            else if (index > main_index && index < pipe_inputs.Count - 1)
                intersect_region = MainPipeIntersector.GetMainRegion(shortest_way[index + 1].poly, region, true);
            var max_buff = shortest_way[index].buff.Max();
            intersect_region = PassageWayUtils.Buffer(intersect_region, -max_buff * 3).First();
            //Polyline target_way = NomalPipeIntersector.IntersectWithBuffer(cur_seg, buff_poly[0], ref buffers, Math.Abs(pipe_segments[index][i].dw) / 2, turn_left);
            PassageShowUtils.ShowEntity(intersect_region, 4);
            //if (pipe_inputs.Count > 1)
            //{
            //    main_region.Dispose();
            //    if (main_index == 0)
            //        main_region = MainPipeIntersector.GetMainRegion(shortest_way[index].poly, region, true);
            //    else if (main_index == pipe_inputs.Count - 1)
            //        main_region = MainPipeIntersector.GetMainRegion(shortest_way[main_index - 1].poly, region, false);
            //    else
            //        main_region = MainPipeIntersector.GetMainRegion(shortest_way[main_index - 1].poly, shortest_way[main_index + 1].poly, region);
            //}
        }
        void ConvertMainWay()
        {
            // calculate max_dw
            double max_dw = double.MaxValue;
            for (int i = 0; i < shortest_way[main_index].buff.Count; ++i)
                max_dw = Math.Min(max_dw, -2 * Math.Abs(shortest_way[main_index].buff[i]));
            // init region
            Polyline main_region = new Polyline();
            //region = region.Buffer(max_dw * 0.5).Cast<Polyline>().OrderByDescending(o => o.Area).First();
            if (pipe_inputs.Count > 1)
            {
                main_region.Dispose();
                if (main_index == 0)
                    main_region = MainPipeIntersector.GetMainRegion(shortest_way[main_index + 1].poly, region, true);
                else if (main_index == pipe_inputs.Count - 1)
                    main_region = MainPipeIntersector.GetMainRegion(shortest_way[main_index - 1].poly, region, false);
                else
                    main_region = MainPipeIntersector.GetMainRegion(shortest_way[main_index - 1].poly, shortest_way[main_index + 1].poly, region);
            }
            // init remove part
            DBObjectCollection rest = new DBObjectCollection();
            rest.Add(shortest_way[main_index].Buffer(3));
            if (pipe_inputs.Count > 1)
            {
                if (main_index > 0)
                    rest.Add(shortest_way[main_index - 1].Buffer(3));
                if (main_index < pipe_inputs.Count - 1)
                    rest.Add(shortest_way[main_index + 1].Buffer(3));
            }
            // get rest
            rest = main_region.Difference(rest);
            List<Polyline> pipes = new List<Polyline>();
            pipes.Add(shortest_way[main_index].Buffer());
            foreach (Polyline poly in rest)
            {
                //PassageWayUtils.ShowEntity(poly, 1);
                pipes.AddRange(MainPipeIntersector.DealWithRegion(shortest_way[main_index], poly, region, -max_dw * 2, ref skeleton));
            }
            var main_pipe = pipes.ToArray().ToCollection().UnionPolygons().Cast<Polyline>().ToList().First();
            main_pipe.ColorIndex = main_index % 7 + 1;
            skeleton.Add(main_pipe);
        }
        int FindBoxPointIn(Point3d p)
        {
            int index = -1;
            double min_dis = double.MaxValue;
            for(int i = 0; i < boxes.Count; ++i)
            {
                if (p.X >= boxes[i].xmin - 1 && p.X <= boxes[i].xmax + 1
                    && p.Y >= boxes[i].ymin - 1 && p.Y <= boxes[i].ymax + 1) 
                {
                    min_dis = boxes[i].center.DistanceTo(p);
                    index = i;
                }
            }
            return index;
        }
        int GetDirBetweenTwoBox(RectBox a,RectBox b)
        {
            var dx = b.j - a.j;
            var dy = b.i - a.i;
            if (dx == 1)
                return 0;
            if (dx == -1)
                return 2;
            if (dy == 1)
                return 1;
            return 3;
        }
        void ShowBoxWay(List<int> box_path)
        {
            for (int i = 0; i < box_path.Count; ++i)
            {
                PassageShowUtils.ShowEntity(PassageWayUtils.BuildRectangle(boxes[box_path[i]]), 4);
                PassageShowUtils.ShowText(boxes[box_path[i]].center, box_path[i].ToString(), 4);
            }
                
        }
        void ShowSegmentGate(List<PipeSegment> ret)
        {
            for (int i = 0; i < ret.Count; ++i)
            {
                var polyend = new Polyline();
                var polystart = new Polyline();
                if (ret[i].dir % 2 == 0)
                {
                    polyend.AddVertexAt(0, new Point2d(ret[i].end, ret[i].min), 0, 0, 0);
                    polyend.AddVertexAt(1, new Point2d(ret[i].end, ret[i].max), 0, 0, 0);
                    polystart.AddVertexAt(0, new Point2d(ret[i].start, ret[i].min), 0, 0, 0);
                    polystart.AddVertexAt(1, new Point2d(ret[i].start, ret[i].max), 0, 0, 0);
                }
                else
                {
                    polyend.AddVertexAt(0, new Point2d(ret[i].min, ret[i].end), 0, 0, 0);
                    polyend.AddVertexAt(1, new Point2d(ret[i].max, ret[i].end), 0, 0, 0);
                    polystart.AddVertexAt(0, new Point2d(ret[i].min, ret[i].start), 0, 0, 0);
                    polystart.AddVertexAt(1, new Point2d(ret[i].max, ret[i].start), 0, 0, 0);
                }
                polyend.Closed = false;
                polyend.ColorIndex = 2;
                polystart.Closed = false;
                polystart.ColorIndex = 3;
                skeleton.Add(polyend);
                skeleton.Add(polystart);
            }
        }
        void ShowDirWay(int index)
        {
            System.Diagnostics.Trace.WriteLine("------( " + index.ToString() + " )------");
            for (int j = 0; j < pipe_segments[index].Count; ++j)
            {
                string dir = "";
                switch (pipe_segments[index][j].dir)
                {
                    case 0: dir = " [right]"; break;
                    case 1: dir = " [up]   "; break;
                    case 2: dir = " [left] "; break;
                    case 3: dir = " [down] "; break;
                }
                string seg_idx = "segment[" + j.ToString() + "]:";
                string side = "";
                if (j < pipe_segments[index].Count - 1)
                    side = " turn " + (pipe_segments[index][j + 1].side ? "[left] " : "[right]") + ',';
                else
                    side = " turn [NaN]  ,";
                string close_to = " close to " + (pipe_segments[index][j].close_to ? "[end]  " : "[start]") + ",";
                string offset = " offset = [" + pipe_segments[index][j].offset + "]";
                System.Diagnostics.Trace.WriteLine(seg_idx + dir + side + close_to + offset);
            }
        }
    }
}
