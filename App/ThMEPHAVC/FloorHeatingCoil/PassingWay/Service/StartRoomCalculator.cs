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
    class StartRoomCalculator
    {
        Polyline region = null;
        List<PipeInput> pipe_inputs = null;

        int indir = -1;
        int outdir = -1;

        public List<PipeOutput> outputs = new List<PipeOutput>();

        public StartRoomCalculator(Polyline region, List<PipeInput> pipe_inputs)
        {
            this.region = region;
            this.pipe_inputs = pipe_inputs;

            check();
            indir = pipe_inputs[0].start_dir;
            outdir = pipe_inputs[0].end_dir;
        }
        public void check()
        {
            HashSet<int> end_dirs = new HashSet<int>();
            foreach (var pipe_input in pipe_inputs)
                end_dirs.Add(pipe_input.end_dir);
            if (end_dirs.Count > 1)
            {
                //throw new NotSupportedException("住宅模式不支持含有多个门的输出。");
                throw new NotSupportedException(ThFloorHeatingCommon.Error_privateOneDoor);
            }
        }
        public void CalculatePipeline()
        {
            if (indir % 2 != outdir % 2)
                CalculateDifferentIODirection();
            else if (indir == outdir)
                CalculateSameIODirection();
        }
        void CalculateDifferentIODirection()
        {
            for (int i = 0; i < pipe_inputs.Count; ++i)
            {
                List<Point3d> points = new List<Point3d>();
                points.Add(pipe_inputs[i].pin);
                points.Add(pipe_inputs[i].pout);
                Point3d mid_point = indir % 2 == 0 ? new Point3d(points[1].X, points[0].Y, 0) :
                                                     new Point3d(points[0].X, points[1].Y, 0);
                points.Insert(1, mid_point);

                List<double> buff = new List<double>();
                buff.Add(pipe_inputs[i].in_buffer);
                buff.Add(pipe_inputs[i].out_buffer);

                var shortest_way = new BufferPoly(points, buff);
                outputs.Add(new PipeOutput(pipe_inputs[i].pipe_id, shortest_way));
            }
        }
        void CalculateSameIODirection()
        {
            for (int i = 0; i < pipe_inputs.Count; ++i)
            {
                // calculate skeleton
                List<Point3d> points = new List<Point3d>();
                points.Add(pipe_inputs[i].pin);
                points.Add(pipe_inputs[i].pout);
                Point3d mid_point = points[0] + Vector3d.XAxis.RotateBy(Math.PI / 2 * indir, Vector3d.ZAxis) * 250;
                mid_point = indir % 2 == 0 ? new Point3d(mid_point.X, points[1].Y, 0) :
                                              new Point3d(points[1].X, mid_point.Y, 0);
                points.Insert(1, mid_point);
                var skeleton = PassageWayUtils.BuildPolyline(points);
                // calculate shape
                var shape_points = new List<Point3d>();
                var left_norm = Vector3d.XAxis.RotateBy(Math.PI / 2 * ((indir + 1) % 4), Vector3d.ZAxis);
                var right_norm = Vector3d.XAxis.RotateBy(Math.PI / 2 * ((indir + 3) % 4), Vector3d.ZAxis);
                shape_points.Add(points[0] + left_norm * pipe_inputs[i].in_buffer);
                shape_points.Add(points[1] + left_norm * pipe_inputs[i].out_buffer);
                shape_points.Add(points[2] + left_norm * pipe_inputs[i].out_buffer);
                shape_points.Add(points[2] + right_norm * pipe_inputs[i].out_buffer);
                shape_points.Add(points[1] + right_norm * pipe_inputs[i].out_buffer);
                shape_points.Add(points[0] + right_norm * pipe_inputs[i].in_buffer);
                shape_points.Add(shape_points.First());
                var shape = PassageWayUtils.BuildPolyline(shape_points);
                outputs.Add(new PipeOutput(pipe_inputs[i].pipe_id, skeleton, shape));
            }
        }
    }
}
