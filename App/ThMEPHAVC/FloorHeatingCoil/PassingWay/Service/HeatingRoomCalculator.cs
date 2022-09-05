using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FloorHeatingCoil
{
    class HeatingRoomCalculator
    {
        Polyline region = null;
        PipeInput pipe_input = null;
        double buffer;
        double room_buffer;
        public PipeOutput output = null;
        public HeatingRoomCalculator(Polyline region,PipeInput pipe_input,double buffer,double room_buffer)
        {
            this.region = region;
            this.pipe_input = pipe_input;
            this.buffer = buffer;
            this.room_buffer = room_buffer;
        }

        public void Calculate()
        {
            // 初始化计算管线
            DirectionWayCalculator directionWayCalculator = new DirectionWayCalculator(region);
            // 初始化线框图
            directionWayCalculator.BuildGraph();
            // 计算最短方向路径
            List<PipeSegment> pipe_segments = directionWayCalculator.Calculate(pipe_input);
            CalculateShortestWay(pipe_segments);
        }
        void CalculateShortestWay(List<PipeSegment> pipe_segments)
        {
            List<Point3d> poly = new List<Point3d>();
            List<double> buffers = new List<double>();
            poly.Add(pipe_input.pin);
            buffers.Add(pipe_input.in_buffer);
            for (int i = 0; i < pipe_segments.Count; ++i)
            {
                var s = pipe_segments[i].start;
                var e = pipe_segments[i].end;
                var dir = pipe_segments[i].dir;
                if (pipe_segments[i].dir % 2 == 0)
                    poly.Add(new Point3d((s + e) / 2, poly.Last().Y, 0));
                else
                    poly.Add(new Point3d(poly.Last().X, (s + e) / 2, 0));
                if (i > 0)
                {
                    var len = Math.Abs(pipe_segments[i - 1].start - pipe_segments[i - 1].end);
                    if (len < 2 * room_buffer + buffer/2)
                        len /= 3;
                    else
                        len = buffer/2;
                    buffers.Add(len / 2);
                }
                if (i == pipe_segments.Count - 1)
                {
                    if (dir % 2 != pipe_input.end_dir % 2) 
                    {
                        var p_last = poly.Last();
                        var p_out = pipe_input.pout;
                        if (pipe_input.end_dir % 2 == 0)
                        {
                            poly[poly.Count - 1] = new Point3d(p_last.X, p_out.Y, 0);
                        }
                        else
                            poly[poly.Count - 1] = new Point3d(p_out.X, p_last.Y, 0);
                    }
                    else
                    {
                        if (i > 0)
                        {
                            poly.RemoveAt(poly.Count - 1);
                            buffers.RemoveAt(buffers.Count - 1);
                            var p_last = poly.Last();
                            var p_out = pipe_input.pout;
                            if (pipe_input.end_dir % 2 == 0)
                            {
                                poly[poly.Count - 1] = new Point3d(p_last.X, p_out.Y, 0);
                            }
                            else
                                poly[poly.Count - 1] = new Point3d(p_out.X, p_last.Y, 0);
                        }
                        else
                        {
                            var p_last = poly.Last();
                            var p_out = pipe_input.pout;
                            if (pipe_input.end_dir % 2 == 0)
                            {
                                poly.Add(new Point3d(p_last.X, p_out.Y, 0));
                            }
                            else
                                poly.Add(new Point3d(p_out.X, p_last.Y, 0));
                            var len = Math.Abs(e - s);
                            if (len < 2 * room_buffer + buffer / 2)
                                len /= 3;
                            else
                                len = buffer / 2;
                            buffers.Add(len / 2);
                        }
                    }
                }
            }
            poly.Add(pipe_input.pout);
            buffers.Add(pipe_input.out_buffer);
            var buff_poly = new BufferPoly(poly, buffers);
            output = new PipeOutput(pipe_input.pipe_id, buff_poly);
        }
    }
}
