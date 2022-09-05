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
        // 输入数据
        public Polyline region;
        public double buffer { get; set; } = 500;
        public double room_buffer { get; set; } = 100;
        public List<PipeInput> pipe_inputs { get; set; } = new List<PipeInput>();
        public int main_index { get; set; } = -1;
        public int mode { get; set; } = 0;         // 0:正常模式/1:集水器模式

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
            // 房间框线修整
            this.region = SmoothUtils.SmoothPolygonByRoundXY(region);
            for (int i = 0; i < pipe_in_list.Count; ++i)
                pipe_in_list[i].CenterPoint = this.region.GetClosestPointTo(pipe_in_list[i].CenterPoint, false);
            for (int i = 0; i < pipe_out_list.Count; ++i)
                pipe_out_list[i].CenterPoint = this.region.GetClosestPointTo(pipe_out_list[i].CenterPoint, false);
            // 检查主导管线是否有出口
            main_has_output = pipe_in_list.Count == pipe_out_list.Count;
            if (!main_has_output)
            {
                main_pipe_input = new PipeInput(pipe_in_list[main_index]);
                pipe_in_list.RemoveAt(main_index);
                main_index--;
            }
            // 初始化输入数据
            for (int i = 0; i < pipe_in_list.Count; ++i)
            {
                pipe_inputs.Add(new PipeInput(pipe_in_list[i], pipe_out_list[i]));
                pipe_inputs[i].passage_index = i;
            }
            // 设置推荐间距
            this.buffer = buffer;
            // 设置距墙间距
            this.room_buffer = room_buffer;
            // 计算出入口方向
            CalculateIODirection();
            // 设置主导管线索引
            this.main_index = main_index;
            // 设置布置模式
            this.mode = CheckMode(mode);
        }
        void CalculateIODirection()
        {
            // 初始化房间边界点
            var points = PassageWayUtils.GetPolyPoints(this.region);
            for (int i = 0; i < pipe_inputs.Count; ++i)
            {
                // 计算入口方向
                var pre = PassageWayUtils.GetSegIndexOnPolygon(pipe_inputs[i].pin, points);
                int next = -1;
                int dir = -10;
                if (pre != -1)
                {
                    next = (pre + 1) % points.Count;
                    if (Math.Abs(pipe_inputs[i].pin.DistanceTo(points[pre]) - room_buffer - pipe_inputs[i].in_buffer) < 1 ||
                        Math.Abs(pipe_inputs[i].pin.DistanceTo(points[next]) - room_buffer - pipe_inputs[i].in_buffer) < 1)
                        pipe_inputs[i].in_near_wall = true;
                    dir = PassageWayUtils.GetDirBetweenTwoPoint(points[pre], points[next]);
                    pipe_inputs[i].start_dir = (dir + 3) % 4;
                }
                // 计算出口方向
                pre = PassageWayUtils.GetSegIndexOnPolygon(pipe_inputs[i].pout, points);
                if (pre != -1)
                {
                    next = (pre + 1) % points.Count;
                    if (Math.Abs(pipe_inputs[i].pout.DistanceTo(points[pre]) - room_buffer - pipe_inputs[i].out_buffer) < 1 ||
                        Math.Abs(pipe_inputs[i].pout.DistanceTo(points[next]) - room_buffer - pipe_inputs[i].out_buffer) < 1)
                        pipe_inputs[i].out_near_wall = true;
                    dir = PassageWayUtils.GetDirBetweenTwoPoint(points[pre], points[next]);
                    pipe_inputs[i].end_dir = (dir + 1) % 4;
                }
                // 设置出口所在房间边的索引
                pipe_inputs[i].end_offset = pre;
                // 打印输入数据
                PassageShowUtils.PrintMassage(i.ToString() + ":" + pipe_inputs[i].start_dir.ToString() + pipe_inputs[i].end_dir.ToString() +
                                    "-" + (pipe_inputs[i].in_near_wall ? "T" : "F") + (pipe_inputs[i].out_near_wall ? "T" : "F") +
                                    "-" + pipe_inputs[i].is_out_free);
                
            }
            // 计算没有出口的主导管线方向
            if (!main_has_output)
            {
                var pre = PassageWayUtils.GetSegIndexOnPolygon(main_pipe_input.pin, points);
                if (pre != -1)
                {
                    var next = (pre + 1) % points.Count;
                    var dir = PassageWayUtils.GetDirBetweenTwoPoint(points[pre], points[next]);
                    main_pipe_input.start_dir = (dir + 3) % 4;
                    if (Math.Abs(main_pipe_input.pin.DistanceTo(points[pre]) - room_buffer - main_pipe_input.in_buffer) < 1 ||
                        Math.Abs(main_pipe_input.pin.DistanceTo(points[next]) - room_buffer - main_pipe_input.in_buffer) < 1)
                        main_pipe_input.in_near_wall = true;
                }
                PassageShowUtils.PrintMassage("main:" + main_pipe_input.start_dir.ToString() + "-" + (main_pipe_input.in_near_wall ? "T" : "F"));
            }
        }
        int CheckMode(int mode)
        {
            // 散热器房间
            if (main_has_output && pipe_inputs.Count == 1)
                return 2;
            // 用户指定按照集水器模式布置
            if (mode == 1) 
                return 1;
            // 如果房间不是矩形，那么按照正常模式布置
            var points = SmoothUtils.SmoothPoints(PassageWayUtils.GetPolyPoints(region));
            if (points.Count > 4) 
                return mode;

            // 如果管线出口不在同一边，那么按照正常模式布置
            if (pipe_inputs[0].end_offset != pipe_inputs.Last().end_offset) 
                return mode;

            // 判断最后一条边是否是均匀布置
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
            return mode;
        }
    }
}
