using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPHVAC.FloorHeatingCoil.Heating;

namespace ThMEPHVAC.FloorHeatingCoil
{
    class EquispacedWayCalculator
    {
        Polyline region = null;
        int main_index = -1;
        public double buffer = 600;
        public double room_buffer = 100;
        List<PipeInput> pipe_inputs = null;
        List<List<PipeSegment>> pipe_segments = null;

        Envelope env = null;

        public List<int> changed_index = new List<int>();
        
        public EquispacedWayCalculator(
            Polyline region, 
            int main_index, 
            double buffer, 
            double room_buffer, 
            List<PipeInput> pipe_inputs, 
            List<List<PipeSegment>> pipe_segments)
        {
            this.region = region;
            this.main_index = main_index;
            this.buffer = buffer;
            this.room_buffer = room_buffer;
            this.pipe_inputs = pipe_inputs;
            this.pipe_segments = pipe_segments;

            this.env = region.ToNTSPolygon().EnvelopeInternal;
        }
        public void BuildDirTree(int left, int right, int depth, bool side = true)
        {
            if (right < left) return;
            var dir = pipe_segments[left][depth].dir;
            if (depth == 0)
            {
                for (int i = left; i <= right; ++i)
                {
                    pipe_segments[i][0].offset = i <= main_index ? (i - left) : (right - i);
                    pipe_segments[i][0].pw = pipe_inputs[i].in_buffer;
                    pipe_segments[i][0].equispaced = true;
                }
            }
            if (depth > 0)
            {
                // check if node contain main pipe
                int mid_index = right >= main_index && left <= main_index ? main_index : (right + left) / 2;
                for (int i = left; i <= right; ++i)
                {
                    // set side
                    pipe_segments[i][depth].side = side;
                    // set close_to
                    pipe_segments[i][depth].close_to = side ^ (i <= mid_index);
                    // set offset
                    pipe_segments[i][depth].offset = i <= mid_index ? (i - left) : (right - i);
                    // calculate the same group pipe num
                    var s = pipe_segments[i][depth - 1].start;
                    var e = pipe_segments[i][depth - 1].end;
                    int pipe_num = 0;
                    for (int j = left; j <= right; ++j)
                        if (pipe_segments[j][depth - 1].start == s && pipe_segments[j][depth - 1].end == e)
                            pipe_num++;
                    // set distribution
                    pipe_segments[i][depth].equispaced = (pipe_num - 0.5) * buffer + 2 * room_buffer > Math.Abs(e - s);
                    // set segment width
                    if (pipe_segments[i][depth].equispaced)
                        pipe_segments[i][depth].pw = Math.Abs(e - s) / (4 * pipe_num + 2);
                    else
                        pipe_segments[i][depth].pw = buffer / 4;
                }
            }
            // calculate child'dir
            var left_dir = (dir + 1) % 4;
            var right_dir = (dir + 3) % 4;
            // next depth has shape like XXXXLLLLXXXXRRRRXXXX
            // calculate LLLL's start and end
            int start_idx = left;
            while (start_idx <= right && pipe_segments[start_idx].Count <= depth + 1)
                start_idx++;
            int end_idx = start_idx;
            while (end_idx <= right && pipe_segments[end_idx].Count > depth + 1 && pipe_segments[end_idx][depth + 1].dir == left_dir)
                end_idx++;
            BuildDirTree(start_idx, end_idx - 1, depth + 1, true);
            // calculate RRRR's start and end
            start_idx = end_idx;
            while (start_idx <= right && pipe_segments[start_idx].Count <= depth + 1)
                start_idx++;
            end_idx = start_idx;
            while (end_idx <= right && pipe_segments[end_idx].Count > depth + 1 && pipe_segments[end_idx][depth + 1].dir == right_dir)
                end_idx++;
            BuildDirTree(start_idx, end_idx - 1, depth + 1, false);
        }
        public List<Polyline> Calculate(int index, out List<Line> lines, out List<double> buffers)
        {
            List<int> idxs = null;
            lines = FindEquispacedSegment(index, out idxs, out buffers);
            var ret = ConvertToEquispacedWay(index, lines, idxs);
            return ret;
        }
        public void AdjustCommandBuffer()
        {
            bool flag = true;
            double max_pw = -1;
            foreach (var pipe in pipe_segments)
            {
                for (int i = 1; i < pipe.Count - 1; ++i)
                {
                    flag &= pipe[i].equispaced;
                    max_pw = Math.Max(max_pw, pipe[i].pw);
                }
            }
            if (flag && max_pw != -1)
            {
                buffer = max_pw * 4;
                room_buffer = max_pw * 2;
            }
        }
        List<Line> FindEquispacedSegment(int index, out List<int> idxs,out List<double> buffers)
        {
            List<Line> lines = new List<Line>();
            idxs = new List<int>();
            buffers = new List<double>();

            for (int i = 0; i < pipe_segments[index].Count; ++i)
            {
                double axis = 0;
                var dir = pipe_segments[index][i].dir;
                // calculate first segment
                if (i == 0)
                    axis = dir % 2 == 0 ? pipe_inputs[index].pin.Y : pipe_inputs[index].pin.X;
                // calculate middle segments
                else
                {
                    var s = pipe_segments[index][i - 1].start;
                    var e = pipe_segments[index][i - 1].end;
                    double offset = 0;
                    if (pipe_segments[index][i].equispaced)
                        offset = pipe_segments[index][i].pw * (pipe_segments[index][i].offset * 4 + 3);
                    else
                        offset = pipe_segments[index][i].pw * (pipe_segments[index][i].offset * 4 + 1) + room_buffer;
                    if (pipe_segments[index][i].close_to)
                        axis = e - Math.Sign(e - s) * offset;
                    else
                        axis = s + Math.Sign(e - s) * offset;
                }
                // adjust last segment
                if (i == pipe_segments[index].Count - 1)
                {
                    if (dir == pipe_inputs[index].end_dir)
                    {
                        // if last seg is equispaced, use the equispaced pipe width
                        if (!pipe_segments[index][i].equispaced)
                            axis = dir % 2 == 0 ? pipe_inputs[index].pout.Y : pipe_inputs[index].pout.X;
                        else
                        {
                            var out_axis= dir % 2 == 0 ? pipe_inputs[index].pout.Y : pipe_inputs[index].pout.X;
                            if (Math.Abs(axis - out_axis) > 10 && pipe_inputs[index].is_out_free == false)
                            {
                                if (i == 0 || pipe_segments[index][i].pw < pipe_inputs[index].out_buffer) 
                                    pipe_segments[index].Add(new PipeSegment(pipe_inputs[index].end_dir, pipe_inputs[index].out_buffer));
                                else
                                    axis = dir % 2 == 0 ? pipe_inputs[index].pout.Y : pipe_inputs[index].pout.X;
                            }

                        }
                        pipe_segments[index][i].equispaced = true;
                    }
                    else
                        pipe_segments[index].Add(new PipeSegment(pipe_inputs[index].end_dir, pipe_inputs[index].out_buffer));
                }
                if (pipe_segments[index][i].equispaced || i == pipe_segments[index].Count - 1)
                {
                    lines.Add(BuildLine(dir, axis));
                    idxs.Add(i);
                    buffers.Add(pipe_segments[index][i].pw);
                }
            }
            if (lines.Count == 1)
            {
                var dir = pipe_inputs[index].end_dir;
                var axis = dir % 2 == 0 ? pipe_inputs[index].pout.Y : pipe_inputs[index].pout.X;
                lines.Add(BuildLine(dir, axis));
                idxs.Add(-1);
                buffers.Add(pipe_inputs[index].out_buffer);
            }
            var last_point = lines.Last().GetClosestPointTo(pipe_inputs[index].pout, false);
            var last_lines = lines.Last().ToNTSLineString().Intersection(region.ToNTSPolygon()).ToDbCollection().Cast<Polyline>().ToList();
            if (last_lines.Count > 0)
            {
                var last_line = last_lines.Find(o => o.StartPoint.DistanceTo(last_point) < 2 || o.EndPoint.DistanceTo(last_point) < 2);
                lines[lines.Count - 1].Dispose();
                lines[lines.Count - 1] = new Line(last_line.StartPoint, last_line.EndPoint);
            }
            foreach (var poly in last_lines)
                poly.Dispose();
            return lines;
        }
        List<Polyline> ConvertToEquispacedWay(int index, List<Line> lines, List<int> idxs)
        {
            List<Polyline> ret = new List<Polyline>();
            for (int i = 0; i < idxs.Count - 1; ++i)
            {
                int r = i + 1;
                while (r < idxs.Count - 1 && idxs[r - 1] + 1 == idxs[r])
                    r++;
                var poly = new Polyline();
                poly.AddVertexAt(poly.NumberOfVertices, lines[i].StartPoint.ToPoint2D(), 0, 0, 0);
                for (int j = i; j < r - 1; j++)
                    poly.AddVertexAt(poly.NumberOfVertices, lines[j].Intersection(lines[j + 1]).ToPoint2D(), 0, 0, 0);
                poly.AddVertexAt(poly.NumberOfVertices, lines[r - 1].EndPoint.ToPoint2D(), 0, 0, 0);
                ret.Add(poly);
                i = r - 1;
            }
            ret.Add(PassageWayUtils.BuildPolyline(lines.Last()));
            return ret;
        }
        Line BuildLine(int dir,double axis)
        {
            switch (dir)
            {
                case 0: return new Line(new Point3d(env.MinX, axis, 0), new Point3d(env.MaxX, axis, 0));
                case 1: return new Line(new Point3d(axis, env.MinY, 0), new Point3d(axis, env.MaxY, 0));
                case 2: return new Line(new Point3d(env.MaxX, axis, 0), new Point3d(env.MinX, axis, 0));
                case 3: return new Line(new Point3d(axis, env.MaxY, 0), new Point3d(axis, env.MinY, 0));
                default:
                    return null;
            }
        }
        public void ShowDirWay()
        {
            PassageShowUtils.PrintMassage("%%%%%%%%%%%% experiment %%%%%%%%%%%");
            for (int i = 0; i < pipe_inputs.Count; ++i)
            {
                System.Diagnostics.Trace.WriteLine("------( " + i.ToString() + "th pipe's segments )------");
                for (int j = 0; j < pipe_segments[i].Count; ++j)
                {
                    string dir = "";
                    switch (pipe_segments[i][j].dir)
                    {
                        case 0: dir = " [right]"; break;
                        case 1: dir = " [up]   "; break;
                        case 2: dir = " [left] "; break;
                        case 3: dir = " [down] "; break;
                    }
                    string seg_idx = "segment[" + j.ToString() + "]:";
                    string side = "";
                    if (j < pipe_segments[i].Count - 1)
                        side = " turn " + (pipe_segments[i][j + 1].side ? "[left] " : "[right]") + ',';
                    else
                        side = " turn [NaN]  ,";
                    string close_to = " close to " + (pipe_segments[i][j].close_to ? "[end]  " : "[start]") + ",";
                    string offset = " offset = [" + pipe_segments[i][j].offset + "],";
                    string dw = "dw = " + Math.Round(pipe_segments[i][j].pw, 3);
                    System.Diagnostics.Trace.WriteLine(seg_idx + dir + side + close_to + offset + dw);
                }
            }
        }
    }
}
