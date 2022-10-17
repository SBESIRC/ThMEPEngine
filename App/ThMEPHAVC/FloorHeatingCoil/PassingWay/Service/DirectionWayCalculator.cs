using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPHVAC.FloorHeatingCoil
{
    class DirectionWayCalculator
    {
        Polyline region { get; set; }
        List<RectBox> boxes;
        int[,] boxmap;
        double[,] distmap;
        public DirectionWayCalculator(Polyline region)
        {
            this.region = region;
        }
        public void BuildGraph()
        {
            // get all x and y
            HashSet<double> xs = new HashSet<double>();
            HashSet<double> ys = new HashSet<double>();
            var points = PassageWayUtils.GetPolyPoints(region);
            for (int i = 0; i < points.Count - 1; ++i)
            {
                xs.Add(points[i].X);
                ys.Add(points[i].Y);
            }
            var x = xs.ToList(); x.Sort();
            var y = ys.ToList(); y.Sort();
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
        public List<PipeSegment> Calculate(PipeInput pipe)
        {
            var box_path = FindShortestBoxWay(pipe);       //以矩形为节点的路线
            return ConvertToDirectionWay(pipe, box_path);  //上面那个结构抽出来的信息
        }
        int FindBoxPointIn(Point3d p)
        {
            int index = -1;
            for (int i = 0; i < boxes.Count; ++i)
            {
                if (p.X >= boxes[i].xmin - 1 &&  //+-1 eps
                    p.X <= boxes[i].xmax + 1 &&
                    p.Y >= boxes[i].ymin - 1 && 
                    p.Y <= boxes[i].ymax + 1)
                {
                    index = i;
                }
            }
            return index;
        }
        int GetDirBetweenTwoBox(RectBox a, RectBox b)
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
        List<int> FindShortestBoxWay(PipeInput pipe)
        {
            int s = FindBoxPointIn(pipe.pin);
            int t = FindBoxPointIn(pipe.pout);
            int n = boxes.Count;
            // init arrays
            double[] dist = new double[n];    //全局信息       框线间的真实距离 double
            int[] path = new int[n];          //整条管线       最短路径上的每一个框的index
            bool[] used = new bool[n];      
            int[] dirs = new int[n];          // count = dist.count  累计最短路径上的转弯数量
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
            for (int i = 0; i < n; ++i)
            {
                int x = -1;
                for (int y = 0; y < n; ++y)
                {
                    if (!used[y] && (x == -1 || dist[y] < dist[x]))
                        x = y;
                }
                used[x] = true;
                for (int y = 0; y < n; ++y)
                {
                    if (used[y]) continue;
                    if (dist[x] + distmap[x, y] <= dist[y])
                    {
                        // update dist
                        dist[y] = dist[x] + distmap[x, y];
                        // calculate <path[x]-x-y>'s dirs
                        // 起点所在框的前一个框是这个框自己
                        var pre_dir = path[x] == x ? pipe.start_dir : GetDirBetweenTwoBox(boxes[path[x]], boxes[x]);
                        var cur_dir = GetDirBetweenTwoBox(boxes[x], boxes[y]);
                        int new_dirs = dirs[x] + (Math.Abs(cur_dir - pre_dir) % 2 != 0 ? 1 : 0);
                        // update dirs and path
                        if (dist[x] + distmap[x, y] < dist[y] || new_dirs < dirs[y])
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
        List<PipeSegment> ConvertToDirectionWay(PipeInput pipe, List<int> box_path)
        {
            // group index
            int length = box_path.Count;
            List<PipeSegment> ret = new List<PipeSegment>();
            List<KeyValuePair<int, List<int>>> dir_groups = new List<KeyValuePair<int, List<int>>>();
            int i = 1;
            //给同方向的box分到一组里
            while (i < box_path.Count)
            {
                var dir = GetDirBetweenTwoBox(boxes[box_path[i - 1]], boxes[box_path[i]]);
                dir_groups.Add(new KeyValuePair<int, List<int>>(dir, new List<int>()));
                dir_groups.Last().Value.Add(box_path[i - 1]);
                while (i < box_path.Count)
                {
                    var cur_dir = GetDirBetweenTwoBox(boxes[box_path[i - 1]], boxes[box_path[i]]);
                    if (cur_dir != dir)
                        break;
                    dir_groups.Last().Value.Add(box_path[i]);
                    i++;
                }
            }
            // convert to pipe segment
            for (i = 0; i < dir_groups.Count; ++i)
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
                        for (int j = 0; j < box_idxs.Count; ++j)
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
                        if (current_row >= boxmap.GetLength(0)) 
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
                    ret[i].start = ret[i + 1].min;
                    ret[i].end = ret[i + 1].max;
                }
                else
                {
                    ret[i].start = ret[i + 1].max;
                    ret[i].end = ret[i + 1].min;
                }
            }
            // update first segment
            if (box_path.Count == 1)
            {
                ret.Add(new PipeSegment());
                ret[0].dir = pipe.start_dir;
                switch (pipe.start_dir)
                {
                    case 0: ret[0].start = boxes[box_path[0]].xmin; ret[0].end = boxes[box_path[0]].xmax; break;
                    case 1: ret[0].start = boxes[box_path[0]].ymin; ret[0].end = boxes[box_path[0]].ymax; break;
                    case 2: ret[0].start = boxes[box_path[0]].xmax; ret[0].end = boxes[box_path[0]].xmin; break;
                    case 3: ret[0].start = boxes[box_path[0]].ymax; ret[0].end = boxes[box_path[0]].ymin; break;
                }
            }
            if (ret[0].dir != pipe.start_dir)
            {
                ret.Insert(0, new PipeSegment());
                ret[0].dir = pipe.start_dir;
                ret[0].start = pipe.start_dir <= 1 ? ret[1].min : ret[1].max;
                ret[0].end = pipe.start_dir <= 1 ? ret[1].max : ret[1].min;
            }
            // update end segment(only for U-shape pipe)
            if (ret.Count == 1 && pipe.start_dir != pipe.end_dir && (pipe.start_dir - pipe.end_dir) % 2 == 0) 
            {
                ret.Add(new PipeSegment());
                if (pipe.start_dir % 2 == 0)
                {
                    ret[1].dir = pipe.pout.Y > pipe.pin.Y ? 1 : 3;
                    ret[1].start = pipe.pin.Y;
                    ret[1].end = pipe.pout.Y;
                }
                else
                {
                    ret[1].dir = pipe.pout.X > pipe.pin.X ? 0 : 2;
                    ret[1].start = pipe.pin.X;
                    ret[1].end = pipe.pout.X;
                }
                
            }
            return ret;
        }
        void ShowBoxWay(List<int> box_path, int color_index = 4)
        {
            for (int i = 0; i < box_path.Count; ++i)
            {
                PassageShowUtils.ShowEntity(PassageWayUtils.BuildRectangle(boxes[box_path[i]]), color_index);
                PassageShowUtils.ShowText(boxes[box_path[i]].center, box_path[i].ToString(), color_index);
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
                PassageShowUtils.ShowEntity(polyend,3);
                PassageShowUtils.ShowEntity(polystart, 2);
            }
        }
    }
}
