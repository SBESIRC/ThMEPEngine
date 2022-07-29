using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.FloorHeatingCoil.Heating;

namespace ThMEPHVAC.FloorHeatingCoil
{
    public class PassageDataPreprocessor
    {
        // input
        public Polyline region;
        public double buffer { get; set; } = 500;
        public double room_buffer { get; set; } = 200;
        public List<PipeInput> pipe_inputs { get; set; } = new List<PipeInput>();
        public int main_index { get; set; } = -1;
        public int mode { get; set; } = 0;         // 0:normal generate/1:simple generate

        public bool main_has_output { get; set; } = true;
        public PipeInput main_pipe_input { get; set; } = null;

        public PassageDataPreprocessor(
            Polyline region,
            List<DrawPipeData> pipe_in_list,
            List<DrawPipeData> pipe_out_list,
            int main_index,
            double buffer = 600,
            double room_buffer = 100, 
            int mode = 0)
        {
            // adjust region
            this.region = SmoothUtils.SmoothPolygonByRoundXY(region);
            // check if main pipe has output
            main_has_output = pipe_in_list.Count == pipe_out_list.Count;
            if (!main_has_output)
            {
                main_pipe_input = new PipeInput(pipe_in_list[main_index]);
                pipe_in_list.RemoveAt(main_index);
                main_index--;
            }
            // build pipe input
            for (int i = 0; i < pipe_in_list.Count; ++i)
            {
                pipe_inputs.Add(new PipeInput(pipe_in_list[i], pipe_out_list[i]));
                pipe_inputs[i].passage_index = i;
            }
            // calculate IO direction
            CalculateIODirection();
            // calculate command buffer
            int count = 0;
            double max_pw = -1;
            for (int i = 0; i < pipe_inputs.Count; ++i)
            {
                if (pipe_inputs[i].out_buffer > room_buffer && pipe_inputs[i].out_near_wall)
                {
                    count++;
                    max_pw = Math.Max(max_pw, pipe_inputs[i].out_buffer);
                }
            }
            this.buffer = count == 0 ? buffer : 4 * max_pw;
            this.room_buffer = room_buffer;
            this.main_index = main_index;
            this.mode = CheckMode(mode);
        }
        void CalculateIODirection()
        {
            // get room points
            var points = PassageWayUtils.GetPolyPoints(this.region);
            for (int i = 0; i < pipe_inputs.Count; ++i)
            {
                // calculate start direction
                var pre = PassageWayUtils.GetSegIndexOnPolygon(pipe_inputs[i].pin, points);
                var next = (pre + 1) % points.Count;
                if (Math.Abs(pipe_inputs[i].pin.DistanceTo(points[pre]) - room_buffer - pipe_inputs[i].in_buffer) < 1 ||
                    Math.Abs(pipe_inputs[i].pin.DistanceTo(points[next]) - room_buffer - pipe_inputs[i].in_buffer) < 1)
                    pipe_inputs[i].in_near_wall = true;
                var dir = PassageWayUtils.GetDirBetweenTwoPoint(points[pre], points[next]);
                pipe_inputs[i].start_dir = (dir + 3) % 4;
                // calculate end direction
                pre = PassageWayUtils.GetSegIndexOnPolygon(pipe_inputs[i].pout, points);
                next = (pre + 1) % points.Count;
                if (Math.Abs(pipe_inputs[i].pout.DistanceTo(points[pre]) - room_buffer - pipe_inputs[i].out_buffer) < 1 ||
                    Math.Abs(pipe_inputs[i].pout.DistanceTo(points[next]) - room_buffer - pipe_inputs[i].out_buffer) < 1)
                    pipe_inputs[i].out_near_wall = true;
                dir = PassageWayUtils.GetDirBetweenTwoPoint(points[pre], points[next]);
                pipe_inputs[i].end_dir = (dir + 1) % 4;
                // set end index on room boundary
                pipe_inputs[i].end_offset = pre;
                // show result
                PassageShowUtils.PrintMassage(i.ToString() + ":" + pipe_inputs[i].start_dir.ToString() + pipe_inputs[i].end_dir.ToString() +
                                 "-" + (pipe_inputs[i].in_near_wall ? "T" : "F") + (pipe_inputs[i].out_near_wall ? "T" : "F") +
                                 "-" + pipe_inputs[i].is_out_free);
            }
            if (!main_has_output)
            {
                var pre = PassageWayUtils.GetSegIndexOnPolygon(main_pipe_input.pin, points);
                var next = (pre + 1) % points.Count;
                var dir = PassageWayUtils.GetDirBetweenTwoPoint(points[pre], points[next]);
                main_pipe_input.start_dir = (dir + 3) % 4;
                if (Math.Abs(main_pipe_input.pin.DistanceTo(points[pre]) - room_buffer - main_pipe_input.in_buffer) < 1 ||
                    Math.Abs(main_pipe_input.pin.DistanceTo(points[next]) - room_buffer - main_pipe_input.in_buffer) < 1)
                    main_pipe_input.in_near_wall = true;
                PassageShowUtils.PrintMassage("main:" + main_pipe_input.start_dir.ToString() + "-" + (main_pipe_input.in_near_wall ? "T" : "F"));
            }
        }
        int CheckMode(int mode)
        {
            if (mode == 1) 
                return 1;

            var points = SmoothUtils.SmoothPoints(PassageWayUtils.GetPolyPoints(region));
            if (points.Count > 4) 
                return mode;

            // pipes' out are not same
            if (pipe_inputs[0].end_offset != pipe_inputs.Last().end_offset) 
                return mode;

            var pre = pipe_inputs.First().end_offset;
            var next = (pre + 1) % points.Count;
            var dis = points[pre].DistanceTo(points[next]);
            var count = pipe_inputs.Count;
            if (dis < (pipe_inputs.Count - 0.5) * buffer + 2 * room_buffer)
            {
                if (pipe_inputs.First().pout.DistanceTo(pipe_inputs.Last().pout) / (count - 1) * count > 0.8 * dis)
                {
                    mode = 1;
                }
            }
            //PassageShowUtils.ShowText(region.GetCentroidPoint(), "Mode:" + mode.ToString());
            return mode;
        }
    }
}
