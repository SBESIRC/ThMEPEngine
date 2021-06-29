using System;
using System.Collections.Generic;

namespace ThMEPHVAC.Model
{
    public class ThDuctResourceDistribute
    {
        public ThDuctResourceDistribute(List<Merged_endline_Info> merged_endlines, double air_volumn, int port_num)
        {
            Statistic_endline_info(merged_endlines, out double total_len, out int total_seg_num);
            Distrib_port_num(merged_endlines, total_len, total_seg_num, port_num);
            Distrib_total_air_volumn(merged_endlines, air_volumn, port_num);
            Set_duct_in_air_volume(merged_endlines);
        }

        private void Set_duct_in_air_volume(List<Merged_endline_Info> merged_endlines)
        {
            foreach (var info in merged_endlines)
            {
                double pre_air_volume = 0;
                foreach (var edge in info.segments)
                {
                    int seg_port_num = edge.ports.Count;
                    if (seg_port_num > 0)
                    {
                        edge.direct_edge.AirVolume = edge.ports[seg_port_num - 1].air_volume;
                        pre_air_volume = edge.direct_edge.AirVolume;
                    }
                    else
                        edge.direct_edge.AirVolume = pre_air_volume;
                }
            }
        }

        private void Statistic_endline_info(List<Merged_endline_Info> merged_endlines, out double total_len, out int total_seg_num)
        {
            total_len = 0;
            total_seg_num = 0;
            foreach (var info in merged_endlines)
            {
                foreach (var edge in info.segments)
                {
                    if (edge.distrib_enable)
                    {
                        total_len += edge.direct_edge.EdgeLength;
                        total_seg_num++;
                    }
                }
            }
        }
        public void Distrib_total_air_volumn(List<Merged_endline_Info> merged_endlines, double air_volumn, int ports_num)
        {
            double average_air_volume = (double)air_volumn / ports_num;
            foreach (var info in merged_endlines)
            {
                double cur_air_volume = 0;
                foreach (var edge in info.segments)
                {
                    foreach (var port in edge.ports)
                    {
                        cur_air_volume += average_air_volume;
                        port.air_volume = cur_air_volume;
                    }
                }
            }
        }
        private void Distrib_port_num( List<Merged_endline_Info> merged_endlines,
                                       double total_len,
                                       int total_seg_num,
                                       int dis_port_num)
        {
            int have_dis_port_num = 0;
            Distrib_sub_port(merged_endlines, total_len, dis_port_num, ref have_dis_port_num);
            if (have_dis_port_num > dis_port_num)
                Remove_remind_port(merged_endlines, dis_port_num, have_dis_port_num);
        }
        private void Distrib_sub_port( List<Merged_endline_Info> merged_endlines,
                                       double total_len,
                                       int port_num,
                                       ref int have_dis_port_num)
        {
            foreach (var info in merged_endlines)
            {
                foreach (var edge in info.segments)
                {
                    if (edge.distrib_enable)
                    {
                        int dis_port = (int)(edge.direct_edge.EdgeLength / total_len * port_num);
                        for (int i = 0; i <= dis_port; ++i)
                        {
                            edge.ports.Add(new Port_Info());
                            have_dis_port_num++;
                        }
                    }
                }
            }
        }
        private void Remove_remind_port(List<Merged_endline_Info> merged_endlines,
                                        int dis_port_num, 
                                        int have_dis_port_num)
        {
            int num = have_dis_port_num - dis_port_num;
            foreach (var info in merged_endlines)
            {
                foreach (var edge in info.segments)
                {
                    if (edge.distrib_enable && edge.ports.Count > 1)
                    {
                        edge.ports.RemoveAt(edge.ports.Count - 1);
                        --num;
                    }
                    if (num == 0)
                        return;
                }
            }
        }
    }
}