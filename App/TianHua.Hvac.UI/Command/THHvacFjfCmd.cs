using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using NFox.Cad;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Model;
using TianHua.FanSelection.Function;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.Geometry;
using ThCADCore.NTS;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacFjfCmd : IAcadCommand, IDisposable
    {
        private bool is_integrate;
        private double type3_sep_dis = 30;
        public ThHvacFjfCmd() { }
        public ThHvacFjfCmd(bool is_integrate)
        {
            this.is_integrate = is_integrate;
        }
        public void Dispose() { }
        public void Execute()
        {
            var center_lines = ThHvacCmdService.Get_fan_and_centerline(out ObjectId fan_id, out List<ObjectId>line_ids);
            if (fan_id.IsNull || center_lines.Count == 0)
            {
                ThMEPHVACService.Prompt_msg("未选择中心线或风机");
                return;
            }
            var wall_lines = ThHvacCmdService.Get_walls();
            if (wall_lines.Count == 0)
            {
                ThMEPHVACService.Prompt_msg("未选择墙线");
                return;
            }
            var fan = new ThDbModelFan(fan_id);
            var fjf_param = Get_duct_info(fan, out string air_volume, out ThMEPHVACParam fpm_param);
            if (string.IsNullOrEmpty(air_volume))
            {
                ThMEPHVACService.Prompt_msg("风机风量为0");
                return;
            }
            if (fan.scenario == "消防加压送风")
            {
                Get_bypass_info(fan, ref fjf_param);
                if (string.IsNullOrEmpty(fjf_param.bypass_pattern))
                {
                    ThMEPHVACService.Prompt_msg("未选择旁通模式");
                    return;
                }
                string tee_pattern = fjf_param.bypass_pattern;
                var bypass_lines = Get_bypass(tee_pattern, out Line max_bypass);
                if (!Checkout_input(tee_pattern, bypass_lines, center_lines))
                    return;
                var anay_res = new ThFanAnalysis(fan, fjf_param, bypass_lines, center_lines, wall_lines);
                if (anay_res.center_lines.Count == 0)
                {
                    ThMEPHVACService.Prompt_msg("未搜索到与风机相连的中心线");
                    return;
                }
                Record_bypass_alignment_line(max_bypass, fjf_param, fan, bypass_lines, anay_res.text_alignment, anay_res.move_srt_p);
                Merge_bypass(tee_pattern, anay_res.center_lines);
                // 先画阀，pinter会移动中心线导致墙线与中心线交不上
                var valve_hole = new ThHolesAndValvesEngine(fan, wall_lines, bypass_lines, fjf_param, anay_res.room_lines, anay_res.not_room_lines);
                valve_hole.RunInletValvesInsertEngine();
                valve_hole.RunOutletValvesInsertEngine();
                using (var db = AcadDatabase.Active())
                    ThDuctPortsDrawService.Remove_ids(line_ids.ToArray());
                var pinter = new ThFanDraw(anay_res);
                Insert_electric_valve(fjf_param, fan, max_bypass, pinter);
                if (tee_pattern == "RBType4" || tee_pattern == "RBType5")
                {
                    var vt_pinter = new ThDrawVBypass(fan.air_volume, fjf_param.scale, fan.scenario, anay_res.move_srt_p, pinter.start_id, fjf_param.bypass_size, fjf_param.room_elevation);
                    if (tee_pattern == "RBType4")
                        vt_pinter.Draw_4vertical_bypass(anay_res.vt.vt_elbow, anay_res.in_vt_pos, anay_res.out_vt_pos);
                    else
                        vt_pinter.Draw_5vertical_bypass(anay_res.vt.vt_elbow, anay_res.in_vt_pos, anay_res.out_vt_pos);
                }
                if (is_integrate)
                {
                    var duct_port = new ThHvacDuctPortsCmd(is_integrate, anay_res.fan_break_p, fpm_param, anay_res.out_center_line);
                    duct_port.Execute();
                }
            }
            else
            {
                var bypass_lines = Get_bypass(fjf_param.bypass_pattern, out Line _);
                var anay_res = new ThFanAnalysis(fan, fjf_param, bypass_lines, center_lines, wall_lines);
                if (anay_res.center_lines.Count == 0)
                    return;
                var valve_hole = new ThHolesAndValvesEngine(fan, wall_lines, bypass_lines, fjf_param, anay_res.room_lines, anay_res.not_room_lines);
                valve_hole.RunInletValvesInsertEngine();
                valve_hole.RunOutletValvesInsertEngine();
                using (var db = AcadDatabase.Active())
                    ThDuctPortsDrawService.Remove_ids(line_ids.ToArray());
                _ = new ThFanDraw(anay_res);
                if (is_integrate)
                {
                    var duct_port = new ThHvacDuctPortsCmd(is_integrate, anay_res.fan_break_p, fpm_param, anay_res.out_center_line);
                    duct_port.Execute();
                }
            }
        }
        private bool Checkout_input(string tee_pattern, DBObjectCollection bypass_lines, DBObjectCollection center_lines)
        {
            if ((tee_pattern != "RBType4" && tee_pattern != "RBType5") && bypass_lines.Count == 0)
            {
                ThMEPHVACService.Prompt_msg("未选择旁通旁通管");
                return false;
            }
            if (bypass_lines.Polygonize().Count > 1)
            {
                ThMEPHVACService.Prompt_msg("旁通线闭合");
                return false;
            }
            if (center_lines.Polygonize().Count > 1)
            {
                ThMEPHVACService.Prompt_msg("中心线闭合");
                return false;
            }
            return true;
        }
        private void Record_bypass_alignment_line(Line max_bypass,
                                                  Duct_InParam param, 
                                                  ThDbModelFan fan, 
                                                  DBObjectCollection bypass, 
                                                  List<TextAlignLine> text_alignment,
                                                  Point3d move_srt_p)
        {
            if (param.bypass_size == null)
                return;
            if (bypass.Count == 0)
                text_alignment.Add(new TextAlignLine(new Line(fan.FanInletBasePoint, fan.FanOutletBasePoint), true, param.bypass_size));
            else
            {
                var dis_mat = Matrix3d.Displacement(-move_srt_p.GetAsVector());
                max_bypass.TransformBy(dis_mat);
                text_alignment.Add(new TextAlignLine(max_bypass, true, param.bypass_size));
            }
        }
        private void Insert_electric_valve(Duct_InParam param, ThDbModelFan fan, Line max_bypass, ThFanDraw pinter)
        {
            var dir_vec = (param.bypass_pattern == "RBType4" || param.bypass_pattern == "RBType5") ?
                            (fan.FanOutletBasePoint - fan.FanInletBasePoint).GetNormal() :
                            ThMEPHVACService.Get_edge_direction(max_bypass);
            var angle = dir_vec.GetAngleTo(-Vector3d.XAxis);
            var z = dir_vec.CrossProduct(-Vector3d.XAxis).Z;
            if (Math.Abs(z) < 1e-3)
                z = 0;
            if (z > 0)
                angle = Math.PI * 2 - angle;
            var l = (param.bypass_pattern == "RBType4" || param.bypass_pattern == "RBType5") ?
                     new Line(fan.FanOutletBasePoint, fan.FanInletBasePoint) : max_bypass;
            var insert_p = ThMEPHVACService.Get_mid_point(l);
            var width = ThMEPHVACService.Get_width(param.bypass_size);
            pinter.Insert_electric_valve(insert_p.GetAsVector(), width, angle + 0.5 * Math.PI);
        }
        private void Merge_bypass(string bypass_pattern, List<Fan_duct_Info> center_lines)
        {
            if (bypass_pattern == "RBType3")
            {
                var detect_dis = type3_sep_dis - 2;
                var bypass1 = new Fan_duct_Info();
                var bypass2 = new Fan_duct_Info();
                foreach (var f_duct in center_lines)
                {
                    foreach (var l_duct in center_lines)
                    {
                        var dis = ThMEPHVACService.Get_line_dis(f_duct.sp, f_duct.ep, l_duct.sp, l_duct.ep);
                        if (Math.Abs(dis - detect_dis) < 1e-3)
                        {
                            bypass1 = f_duct;
                            bypass2 = l_duct;
                            break;
                        }
                    }
                    if (bypass1.size != null)
                        break;
                }
                center_lines.Remove(bypass1);
                center_lines.Remove(bypass2);
                var merge_duct = new Fan_duct_Info(bypass1.sp, bypass2.sp, bypass1.size, bypass1.src_shrink, bypass2.src_shrink);
                center_lines.Add(merge_duct);
            }
        }
        private void Fix_duct_size_info(ref Duct_InParam info)
        {
            var w1 = ThMEPHVACService.Get_width(info.room_duct_size);
            var w2 = ThMEPHVACService.Get_width(info.other_duct_size);
            var big_size = w1 > w2 ? info.room_duct_size : info.other_duct_size;
            var small_size = w1 < w2 ? info.room_duct_size : info.other_duct_size;
            info.room_duct_size = big_size;
            info.other_duct_size = small_size;
        }
        private Duct_InParam Get_duct_info(ThDbModelFan fan, out string air_volume, out ThMEPHVACParam fpm_param)
        {
            air_volume = string.Empty;
            fpm_param = new ThMEPHVACParam();
            if (is_integrate)
            {
                using (var dlg = Create_integrate_diag(fan))
                {
                    if (AcadApp.ShowModalDialog(dlg) == DialogResult.OK)
                    {
                        var fjf_param = Get_fjf_param(dlg, fan.is_exhaust, out air_volume);
                        Fix_duct_size_info(ref fjf_param);
                        fpm_param = Get_fpm_param(dlg, fan.is_exhaust, fjf_param.room_duct_size);
                        return fjf_param;
                    }
                }
            }
            else
            {
                using (var dlg = Create_duct_diag(fan))
                {
                    if (AcadApp.ShowModalDialog(dlg) == DialogResult.OK)
                    { 
                        var fjf_param = Get_fjf_param(dlg, fan.is_exhaust, out air_volume);
                        Fix_duct_size_info(ref fjf_param);
                        return fjf_param;
                    }
                }
            }
            return new Duct_InParam();
        }
        private ThMEPHVACParam Get_fpm_param(fmFjfFpm dlg, bool is_exhaust, string room_duct_size)
        {
            var info = new ThMEPHVACParam();
            info.port_num = dlg.port_num;
            info.scenario = dlg.scenario;
            info.scale = dlg.scale;
            var elevation = is_exhaust ? dlg.elevation1.ToString() : dlg.elevation2.ToString();
            info.elevation = Double.Parse(elevation);
            info.port_size = dlg.port_size;
            info.port_name = dlg.port_name;
            info.air_volume = dlg.air_volume;
            info.high_air_volume = dlg.high_air_volume;
            info.port_range = dlg.port_range;
            info.in_duct_size = room_duct_size;//room 侧
            info.air_speed = dlg.air_speed;
            return info;
        }
        private Duct_InParam Get_fjf_param(fmFjfFpm dlg, bool is_exhaust, out string air_volume)
        {
            var info = new Duct_InParam();
            info.room_duct_size = dlg.i_duct_size;
            info.other_duct_size = dlg.o_duct_size;
            air_volume = dlg.air_volume.ToString();
            info.room_elevation = is_exhaust ? dlg.elevation1.ToString() : dlg.elevation2.ToString();
            info.other_elevation = is_exhaust ? dlg.elevation2.ToString() : dlg.elevation1.ToString();
            info.scale = dlg.scale;
            return info;
        }
        private Duct_InParam Get_fjf_param(fmDuctSpec dlg, bool is_exhaust, out string air_volume)
        {
            var info = new Duct_InParam();
            info.room_duct_size = dlg.SelectedInnerDuctSize;
            info.other_duct_size = dlg.SelectedOuterDuctSize;
            air_volume = dlg.AirVolume.ToString();
            info.room_elevation = is_exhaust ? dlg.Elevation : dlg.Elevation2;
            info.other_elevation = is_exhaust ? dlg.Elevation2 : dlg.Elevation;
            info.scale = dlg.TextSize;
            return info;
        }
        private void Get_bypass_info(ThDbModelFan fan, ref Duct_InParam info)
        {
            using (var dlg = Create_bypass_diag(fan))
            {
                if (AcadApp.ShowModalDialog(dlg) == DialogResult.OK)
                {
                    info.bypass_size = dlg.bypass_size;
                    info.bypass_pattern = dlg.bypass_pattern;
                }
                else
                    return;
            }
        }
        private DBObjectCollection Get_bypass(string tee_pattern, out Line max_bypass_line)
        {
            max_bypass_line = new Line();
            if (tee_pattern == null)
                return new DBObjectCollection();
            if (tee_pattern == "RBType4" || tee_pattern == "RBType5")
                return new DBObjectCollection();
            var objIds = ThHvacCmdService.Get_from_prompt("请选择旁通管", false);
            if (objIds.Count == 0)
                return new DBObjectCollection();
            var bypass = objIds.Cast<ObjectId>().Select(o => o.GetDBObject().Clone() as Curve).ToCollection();
            bypass = ThMEPHVACLineProc.Explode(bypass);
            var bypass_line = ThMEPHVACService.Get_max_line(bypass);
            // 给较长的线段上插点
            if (tee_pattern == "RBType3")
                Cut_max_bypass(bypass, bypass_line);//修改线集里的旁通
            max_bypass_line = new Line(bypass_line.StartPoint, bypass_line.EndPoint);//保存最长旁通防止被修改
            return bypass;
        }
        
        private void Cut_max_bypass(DBObjectCollection bypass, Line max_bypass)
        {
            bypass.Remove(max_bypass);
            double shrink_len = type3_sep_dis * 0.5;
            var dir_vec = ThMEPHVACService.Get_edge_direction(max_bypass);
            var mid_p = ThMEPHVACService.Get_mid_point(max_bypass);
            var p = mid_p - shrink_len * dir_vec;
            bypass.Add(new Line(max_bypass.StartPoint, p));
            p = mid_p + shrink_len * dir_vec;
            bypass.Add(new Line(p, max_bypass.EndPoint));
        }
        private fmBypass Create_bypass_diag(ThDbModelFan fan)
        {
            var info = Get_duct_info(fan.air_volume, fan.scenario, fan.str_air_volume);
            var fm = new fmBypass(fan.air_volume);
            fm.InitForm(info);
            return fm;
        }
        private fmDuctSpec Create_duct_diag(ThDbModelFan fan)
        {
            bool is_exhaust = !(fan.scenario.Contains("补") || fan.scenario.Contains("送"));
            var info = Get_duct_info(fan.air_volume, fan.scenario, fan.str_air_volume);
            var fm = new fmDuctSpec();
            fm.InitForm(info, is_exhaust);
            return fm;
        }
        private fmFjfFpm Create_integrate_diag(ThDbModelFan fan)
        {
            bool is_exhaust = !(fan.scenario.Contains("补") || fan.scenario.Contains("送"));
            var info = Get_duct_info(fan.air_volume, fan.scenario, fan.str_air_volume);
            var fm = new fmFjfFpm(info, is_exhaust, fan.scenario);
            return fm;
        }
        private DuctSpecModel Get_duct_info(double air_volume, string scenario, string str_air_volume)
        {
            var duct_param = new ThDuctParameter(air_volume, ThFanSelectionUtils.GetDefaultAirSpeed(scenario), true);
            return new DuctSpecModel()
            {
                AirVolume = air_volume,
                StrAirVolume = str_air_volume,
                AirSpeed = ThFanSelectionUtils.GetDefaultAirSpeed(scenario),
                MaxAirSpeed = ThFanSelectionUtils.GetMaxAirSpeed(scenario),
                MinAirSpeed = ThFanSelectionUtils.GetMinAirSpeed(scenario),
                ListOuterTube = new List<string>(duct_param.DuctSizeInfor.DefaultDuctsSizeString),
                ListInnerTube = new List<string>(duct_param.DuctSizeInfor.DefaultDuctsSizeString),
                OuterTube = duct_param.DuctSizeInfor.RecommendOuterDuctSize,
                InnerTube = duct_param.DuctSizeInfor.RecommendInnerDuctSize
            };
        }
    }
}
