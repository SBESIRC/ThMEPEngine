using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NetTopologySuite.Geometries;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Diagnostics;
using ThMEPHVAC.FloorHeatingCoil.Heating;

namespace ThMEPHVAC.FloorHeatingCoil
{
    class PassagePipeGenerator:IDisposable
    {
        // input
        Polyline region;
        double buffer { get; set; } = 600;
        double room_buffer { get; set; } = 100;
        List<PipeInput> pipe_inputs { get; set; } = new List<PipeInput>();
        int main_index { get; set; } = -1;
        int mode { get; set; } = 0;         // 0:normal generate/1:simple generate

        // output
        public List<PipeOutput> outputs { get; set; } = new List<PipeOutput>();
        public List<ChangePointData> change_point_datas { get; set; } = new List<ChangePointData>();

        // inner data
        bool main_has_output { get; set; } = true;
        PipeInput main_pipe_input { get; set; } = null;
        List<List<PipeSegment>> pipe_segments { get; set; }
        List<List<Line>> equispaced_lines { get; set; }
        List<List<double>> equispaced_buffers { get; set; }
        List<List<Polyline>> equispaced_segments { get; set; }
        List<BufferPoly> shortest_way { get; set; }
        public PassagePipeGenerator(Polyline region, List<DrawPipeData> pipe_in_list, List<DrawPipeData> pipe_out_list, int main_index, double buffer = 600, double room_buffer = 100, int mode = 0)
        {
            PassageDataPreprocessor passageDataPreprocess = new PassageDataPreprocessor(region, pipe_in_list, pipe_out_list, main_index, buffer, room_buffer, mode);
            this.region = passageDataPreprocess.region;
            this.buffer = passageDataPreprocess.buffer;
            this.room_buffer = passageDataPreprocess.room_buffer;
            this.pipe_inputs = passageDataPreprocess.pipe_inputs;
            this.main_index = passageDataPreprocess.main_index;
            this.mode = passageDataPreprocess.mode;
            this.main_has_output = passageDataPreprocess.main_has_output;
            this.main_pipe_input = passageDataPreprocess.main_pipe_input;
        }
        public void CalculatePipeline()
        {
            //for(int i=0;i<pipe_inputs.Count;++i)
            //{
            //    if (pipe_inputs[i].is_out_free == true)
            //        PassageShowUtils.ShowPoint(pipe_inputs[i].pout);
            //}
            if (mode == 0)
            {
                CalculateDirectionWay();
                CalculateShortestWay();
                CalculateIntersectWay();
                CheckPoutMoved();
                CalculateMainShortestWay();
                CalculateMainWay();
            }
            else
            {
                StartRoomCalculator startRoomCalculator = new StartRoomCalculator(region, pipe_inputs);
                startRoomCalculator.CalculatePipeline();
                this.outputs = startRoomCalculator.outputs;
            }
        }
        void CalculateDirectionWay()
        {
            // init calculator
            DirectionWayCalculator directionWayCalculator = new DirectionWayCalculator(region);
            // init box graph
            directionWayCalculator.BuildGraph();
            // calculate shortest direction way
            pipe_segments = new List<List<PipeSegment>>();
            for (int i = 0; i < pipe_inputs.Count; ++i)
                pipe_segments.Add(directionWayCalculator.Calculate(pipe_inputs[i]));
        }
        void CalculateShortestWay()
        {
            // init calculator
            EquispacedWayCalculator equispacedWayCalculator = new EquispacedWayCalculator(region, main_index, buffer, room_buffer, pipe_inputs, pipe_segments);
            // group direction
            equispacedWayCalculator.BuildDirTree(0, pipe_inputs.Count - 1, 0);
            
            // show direction group result
            equispacedWayCalculator.ShowDirWay();

            // adjust command buffer
            equispacedWayCalculator.AdjustCommandBuffer();
            buffer = equispacedWayCalculator.buffer;
            room_buffer = equispacedWayCalculator.room_buffer;

            // calcualte equispaces way
            equispaced_segments = new List<List<Polyline>>();
            equispaced_lines = new List<List<Line>>();
            equispaced_buffers = new List<List<double>>();
            for (int i = 0; i < pipe_inputs.Count; ++i)
            {
                List<Line> lines;
                List<double> buffers;
                equispaced_segments.Add(equispacedWayCalculator.Calculate(i, out lines, out buffers));
                equispaced_lines.Add(lines);
                equispaced_buffers.Add(buffers);
            }
        }
        void CalculateIntersectWay()
        {
            IntersectWayCalculator intersectWayCalculator = new IntersectWayCalculator(region, main_index, buffer, room_buffer, pipe_inputs, equispaced_lines, equispaced_buffers, equispaced_segments);
            if (main_has_output)
            {
                for (int i = 0; i < main_index; ++i)
                    intersectWayCalculator.Calculate(i, true);
                for (int i = pipe_inputs.Count - 1; i > main_index; --i)
                    intersectWayCalculator.Calculate(i, false);
                intersectWayCalculator.Calculate(main_index, main_index < pipe_inputs.Count / 2);
            }
            else
            {
                for (int i = 0; i <= main_index; ++i)
                    intersectWayCalculator.Calculate(i, true);
                for (int i = pipe_inputs.Count - 1; i > main_index; --i)
                    intersectWayCalculator.Calculate(i, false);
            }
            shortest_way = intersectWayCalculator.shortest_way;

            for (int i = 0; i < shortest_way.Count; ++i)
            {
                if (main_has_output && i == main_index) continue;
                outputs.Add(new PipeOutput(pipe_inputs[i].pipe_id, shortest_way[i]));
            }
        }
        void CheckPoutMoved()
        {
            for(int i = 0; i < pipe_inputs.Count; ++i)
            {
                var point = shortest_way[i].poly.Last();
                var radius = shortest_way[i].buff.Last();
                var dir = pipe_inputs[i].end_dir;
                if(point.DistanceTo(pipe_inputs[i].pout)>1)
                {
                    if (dir % 2 != 0) 
                    {
                        var left_point = new Point3d(point.X - radius, point.Y, 0);
                        var right_point = new Point3d(point.X + radius, point.Y, 0);
                        change_point_datas.Add(new ChangePointData(pipe_inputs[i].pipe_id, pipe_inputs[i].door_id, left_point, right_point));
                    }
                    else
                    {
                        var left_point = new Point3d(point.X, point.Y+radius, 0);
                        var right_point = new Point3d(point.X, point.Y - radius, 0);
                        change_point_datas.Add(new ChangePointData(pipe_inputs[i].pipe_id, pipe_inputs[i].door_id, left_point, right_point));
                    }
                }
            }
        }
        void CalculateMainShortestWay()
        {
            if (main_has_output) return;
            var dir = main_pipe_input.start_dir;
            var axis = dir % 2 == 0 ? main_pipe_input.pin.Y : main_pipe_input.pin.X;
            var env = region.ToNTSPolygon().EnvelopeInternal;
            Line line = null;
            switch (dir)
            {
                case 0: line=new Line(new Point3d(env.MinX, axis, 0), new Point3d(env.MaxX, axis, 0));break;
                case 1: line=new Line(new Point3d(axis, env.MinY, 0), new Point3d(axis, env.MaxY, 0));break;
                case 2: line=new Line(new Point3d(env.MaxX, axis, 0), new Point3d(env.MinX, axis, 0));break;
                case 3: line=new Line(new Point3d(axis, env.MaxY, 0), new Point3d(axis, env.MinY, 0));break;
            }
            var last_lines = line.ToNTSLineString().Intersection(region.ToNTSPolygon()).ToDbCollection().Cast<Polyline>().ToList();
            if (last_lines.Count > 0)
            {
                var last_line = last_lines.Find(o => o.StartPoint.DistanceTo(main_pipe_input.pin) < 1 || o.EndPoint.DistanceTo(main_pipe_input.pin) < 1);
                line.Dispose();
                line = new Line(last_line.StartPoint, last_line.EndPoint);
                main_index++;
                shortest_way.Insert(main_index, new BufferPoly(line, main_pipe_input.in_buffer));
                foreach (var poly in last_lines)
                    last_line.Dispose();
            }
            var main_equispaced_line = new List<Line>();
            main_equispaced_line.Add(line);
            var main_equispaced_buffer = new List<double>();
            main_equispaced_buffer.Add(main_pipe_input.in_buffer);
            equispaced_lines.Insert(main_index, main_equispaced_line);
            equispaced_buffers.Insert(main_index, main_equispaced_buffer);
        }
        void CalculateMainWay()
        {
            MainPipeGet mainPipeGet = new MainPipeGet(region, shortest_way, main_index, buffer, room_buffer, main_has_output);
            mainPipeGet.Pipeline();
            MainPipeBuffer mainPipeBuffer = new MainPipeBuffer(
                mainPipeGet.Skeleton,
                equispaced_lines[main_index],
                equispaced_buffers[main_index],
                main_has_output ? pipe_inputs[main_index] : main_pipe_input,
                buffer,
                main_has_output);
            mainPipeBuffer.Calculate();

            if (main_index == outputs.Count)
                outputs.Add(mainPipeBuffer.output);
            else
                outputs.Insert(main_index, mainPipeBuffer.output);
        }

        //void CalculateMainWay()
        //{
        //    MainPipeCalculator mainPipeCalculator = new MainPipeCalculator(region, shortest_way, main_index, buffer, room_buffer, main_has_output);
        //    mainPipeCalculator.Calculate();
        //}
        public void Dispose()
        {
            region.Dispose();
            if (equispaced_lines != null)
            {
                foreach (var lines in equispaced_lines)
                {
                    foreach (var line in lines)
                        line.Dispose();
                    lines.Clear();
                }
                equispaced_lines.Clear();
            }
            if (equispaced_segments != null)
            {
                foreach (var polys in equispaced_segments)
                {
                    foreach (var poly in polys)
                        poly.Dispose();
                    polys.Clear();
                }
                equispaced_lines.Clear();
            }
        }
    }
}
