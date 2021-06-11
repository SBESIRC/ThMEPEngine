using System.Collections.Generic;

namespace ThMEPHVAC.Model
{
    public class ThDuctResourceDistribute
    {
        private int[] total_dis_port_res { get; set; }
        private List<int>[] sub_dis_port_res { get; set; }
        public ThDuctResourceDistribute(List<Merged_endline_Info> merged_endlines, int port_num)
        {
            Distrib_total_ports(merged_endlines, port_num);
            Distrib_sub_ports(merged_endlines);
        }
        public void Distrib_total_air_volumn(List<Merged_endline_Info> merged_endlines, double air_volumn, int ports_num)
        {
            double average_air_volumn = (double)air_volumn / ports_num;
            for (int i = 0; i < total_dis_port_res.Length; ++i)
            {
                double pre_air_volumn = 0;
                var info = merged_endlines[i];
                int segment_num = sub_dis_port_res[i].Count;
                for (int j = 0; j < segment_num; ++j)
                {
                    Endline_Info endlines = info.segments[j];
                    int ports_count = sub_dis_port_res[i][j];
                    endlines.ports = new List<Port_Info>();
                    for (int k = 0; k < ports_count; ++k)
                    {
                        // port list中的air_volumn是递增的
                        endlines.ports.Add(new Port_Info());
                        endlines.ports[k].air_vloume = (k + 1) * average_air_volumn + pre_air_volumn;
                    }
                    pre_air_volumn = endlines.ports[ports_count - 1].air_vloume;
                }
            }
        }
        private void Distrib_total_ports(List<Merged_endline_Info> merged_endlines, int ports_num)
        {
            total_dis_port_res = new int[merged_endlines.Count];
            Statistic_endline_info(merged_endlines, out double total_len, out int total_seg_num);
            // 从总数中减去endsegment的数量是为了保证每个segment至少分到一个ports
            int remain_ports_num = ports_num - total_seg_num;
            Do_distribute_total_ports(merged_endlines, total_len, remain_ports_num);
        }
        private void Statistic_endline_info(List<Merged_endline_Info> merged_endlines, out double total_len, out int total_seg_num)
        {
            total_len = 0;
            total_seg_num = 0;
            foreach (var info in merged_endlines)
            {
                total_len += info.total_len;
                total_seg_num += info.segments.Count;
            }
        }
        private void Do_distribute_total_ports(List<Merged_endline_Info> merged_endlines, double total_endline_len, int ports_num)
        {
            int sum = 0;
            int max_idx = 0;
            int max_port_num = 0;
            for (int i = 0; i < merged_endlines.Count; ++i)
            {
                int dis_port_num = (int)((merged_endlines[i].total_len / total_endline_len) * ports_num);
                sum += dis_port_num;
                total_dis_port_res[i] = (dis_port_num == 0) ? 0 : dis_port_num;
                if (max_port_num < dis_port_num)
                {
                    max_port_num = dis_port_num;
                    max_idx = i;
                }
            }
            // 将剩余的port分配到最长的endline上
            if (ports_num - sum > 0)
                total_dis_port_res[max_idx] += (ports_num - sum);
        }

        private void Distrib_sub_ports(List<Merged_endline_Info> merged_endlines)
        {
            int endline_num = merged_endlines.Count;
            sub_dis_port_res = new List<int>[endline_num];

            for (int i = 0; i < endline_num; ++i)
            {
                sub_dis_port_res[i] = new List<int>();
                Do_distribute_sub_ports(merged_endlines, i);
            }
        }
        private void Do_distribute_sub_ports(List<Merged_endline_Info> merged_endlines, int nth_merged_endline)
        {
            int sum = 0;
            int max_idx = 0;
            int max_port_num = 0;
            int ports_num = total_dis_port_res[nth_merged_endline];
            Merged_endline_Info info = merged_endlines[nth_merged_endline];
            for (int i = 0; i < info.segments.Count; ++i)
            {
                double cur_line_len = info.segments[i].direct_edge.EdgeLength;
                int dis_port_num = (int)(cur_line_len / info.total_len * ports_num);
                //保证每个segment至少分到一个ports
                sub_dis_port_res[nth_merged_endline].Add(dis_port_num + 1);
                sum += dis_port_num;
                if (max_port_num < dis_port_num)
                {
                    max_idx = i;
                    max_port_num = dis_port_num;
                }
            }
            if (ports_num - sum > 0)
                sub_dis_port_res[nth_merged_endline][max_idx] += (ports_num - sum);
        }
    }
}