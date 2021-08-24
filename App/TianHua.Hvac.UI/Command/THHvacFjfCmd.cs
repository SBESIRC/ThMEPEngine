using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Linq2Acad;
using NFox.Cad;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.CAD;
using ThMEPHVAC.IO;
using ThMEPHVAC.Model;
using TianHua.FanSelection.Function;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.Geometry;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacFjfCmd : IAcadCommand, IDisposable
    {
        private double type3_sep_dis = 30;
        public void Dispose()
        {
            //
        }
        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var center_lines = Get_fan_and_centerline(out ObjectId fan_id);
                if (fan_id.IsNull || center_lines.Count == 0)
                {
                    ThMEPHVACService.Prompt_msg("未选择中心线或风机");
                    return;
                }
                var wall_lines = Get_walls();
                if (wall_lines.Count == 0)
                {
                    ThMEPHVACService.Prompt_msg("未选择墙线");
                    return;
                }
                var fan = new ThDbModelFan(fan_id);
                var param = Get_duct_info(fan, out string air_volume);
                if (string.IsNullOrEmpty(air_volume))
                    return;
                Get_reverse_info(fan.scenario, param);
                if (fan.scenario == "消防加压送风")
                {
                    Get_bypass_info(fan, ref param);
                    if (string.IsNullOrEmpty(param.bypass_pattern))
                        return;
                    string tee_pattern = param.bypass_pattern;
                    var bypass_lines = Get_bypass(tee_pattern, out Line max_bypass);
                    var anay_res = new ThFanAnalysis(fan, param, bypass_lines, center_lines, wall_lines);
                    if (anay_res.center_lines.Count == 0)
                        return;
                    Record_bypass_alignment_line(max_bypass, param, fan, bypass_lines, anay_res.text_alignment);
                    Merge_bypass(tee_pattern, anay_res.center_lines);
                    var pinter = new ThFanDraw(anay_res);
                    var valve_hole = new ThHolesAndValvesEngine(fan, wall_lines, bypass_lines, param, anay_res.in_lines, anay_res.out_lines);
                    valve_hole.RunInletValvesInsertEngine();
                    valve_hole.RunOutletValvesInsertEngine();
                    Insert_electric_valve(param, fan, max_bypass, pinter);
                    
                    if (tee_pattern == "RBType4" || tee_pattern == "RBType5")
                    {
                        var vt_pinter = new ThDrawVBypass(fan.air_volume, param.scale, fan.scenario, anay_res.move_srt_p, pinter.start_id, param.bypass_size, param.elevation);
                        if (tee_pattern == "RBType4")
                            vt_pinter.Draw_4vertical_bypass(anay_res.vt.vt_elbow, anay_res.in_vt_pos, anay_res.out_vt_pos);
                        else
                            vt_pinter.Draw_5vertical_bypass(anay_res.vt.vt_elbow, anay_res.in_vt_pos, anay_res.out_vt_pos);
                    }
                }
                else
                {
                    var bypass_lines = Get_bypass(param.bypass_pattern, out Line max_bypass);
                    var anay_res = new ThFanAnalysis(fan, param, bypass_lines, center_lines, wall_lines);
                    if (anay_res.center_lines.Count == 0)
                        return;
                    var pinter = new ThFanDraw(anay_res);
                    var valve_hole = new ThHolesAndValvesEngine(fan, wall_lines, bypass_lines, param, anay_res.in_lines, anay_res.out_lines);
                    valve_hole.RunInletValvesInsertEngine();
                    valve_hole.RunOutletValvesInsertEngine();
                }
            }
        }
        private void Record_bypass_alignment_line(Line max_bypass,
                                                  Duct_InParam param, 
                                                  ThDbModelFan fan, 
                                                  DBObjectCollection bypass, 
                                                  List<TextAlignLine> text_alignment)
        {
            if (param.bypass_size == null)
                return;
            if (bypass.Count == 0)
                text_alignment.Add(new TextAlignLine(new Line(fan.FanInletBasePoint, fan.FanOutletBasePoint), true, param.bypass_size));
            else
                text_alignment.Add(new TextAlignLine(max_bypass, true, param.bypass_size));
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
        private void Get_reverse_info(string scenario, Duct_InParam info)
        {
            var jsonReader = new ThDuctInOutMappingJsonReader();
            var str = jsonReader.Mappings.First(d => d.WorkingScenario == scenario).InnerRoomDuctType;
            info.is_io_reverse = str != "进风段";
        }
        private Duct_InParam Get_duct_info(ThDbModelFan DbFanModel, out string air_volume)
        {
            air_volume = string.Empty;
            var info = new Duct_InParam();
            using (var dlg = Create_duct_diag(DbFanModel))
            {
                if (AcadApp.ShowModalDialog(dlg) == DialogResult.OK)
                {
                    info.in_duct_size = dlg.SelectedInnerDuctSize;
                    info.out_duct_size = dlg.SelectedOuterDuctSize;
                    air_volume = dlg.AirVolume;
                    info.elevation = dlg.Elevation;
                    info.scale = dlg.TextSize;
                }
            }
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
        private ObjectIdCollection Get_from_prompt(string prompt, bool only_able)
        {
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = prompt,
                RejectObjectsOnLockedLayers = true,
                SingleOnly = only_able
            };
            var result = Active.Editor.GetSelection(options);
            if (result.Status == PromptStatus.OK)
            {
                return result.Value.GetObjectIds().ToObjectIdCollection();
            }
            else
            {
                return new ObjectIdCollection();
            }
        }
        private ObjectId Classify_fan(ObjectIdCollection selections, out DBObjectCollection center_lines)
        {
            ObjectId fan_id = ObjectId.Null;
            center_lines = new DBObjectCollection();
            foreach (ObjectId oid in selections)
            {
                var obj = oid.GetDBObject();
                if (obj.IsRawModel())
                {
                    fan_id = oid;
                }
                else if (obj is Curve curve)
                {
                    center_lines.Add(curve.Clone() as Curve);
                }
            }
            return fan_id;
        }
        private DBObjectCollection Get_fan_and_centerline(out ObjectId fan_id)
        {
            fan_id = ObjectId.Null;
            var objIds = Get_from_prompt("请选择风机和中心线", false);
            if (objIds.Count == 0)
                return new DBObjectCollection();
            fan_id = Classify_fan(objIds, out DBObjectCollection center_lines);
            return ThMEPHVACLineProc.Pre_proc(center_lines);
        }
        private DBObjectCollection Get_bypass(string tee_pattern, out Line max_bypass_line)
        {
            max_bypass_line = new Line();
            if (tee_pattern == null)
                return new DBObjectCollection();
            if (tee_pattern == "RBType4" || tee_pattern == "RBType5")
                return new DBObjectCollection();
            var objIds = Get_from_prompt("请选择旁通管", false);
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
        private DBObjectCollection Get_walls()
        {
            var wallobjects = new DBObjectCollection();
            var objIds = Get_from_prompt("请选择内侧墙线", false);
            if (objIds.Count == 0)
                return new DBObjectCollection();
            foreach (ObjectId oid in objIds)
            {
                var obj = oid.GetDBObject();
                if (obj is Curve curveobj)
                {
                    wallobjects.Add(curveobj);
                }
            }
            return ThMEPHVACLineProc.Explode(wallobjects);
        }
        private fmBypass Create_bypass_diag(ThDbModelFan fan)
        {
            var info = Get_duct_info(fan.air_volume, fan.scenario);
            var fm = new fmBypass(fan.air_volume);
            fm.InitForm(info);
            return fm;
        }
        private fmDuctSpec Create_duct_diag(ThDbModelFan fan)
        {   
            var info = Get_duct_info(fan.air_volume, fan.scenario);
            var fm = new fmDuctSpec();
            fm.InitForm(info);
            return fm;
        }
        private DuctSpecModel Get_duct_info(double air_volume, string scenario)
        {
            var duct_param = new ThDuctParameter(air_volume, ThFanSelectionUtils.GetDefaultAirSpeed(scenario), true);
            return new DuctSpecModel()
            {
                AirSpeed = ThFanSelectionUtils.GetDefaultAirSpeed(scenario),
                MaxAirSpeed = ThFanSelectionUtils.GetMaxAirSpeed(scenario),
                MinAirSpeed = ThFanSelectionUtils.GetMinAirSpeed(scenario),
                AirVolume = air_volume,
                ListOuterTube = new List<string>(duct_param.DuctSizeInfor.DefaultDuctsSizeString),
                ListInnerTube = new List<string>(duct_param.DuctSizeInfor.DefaultDuctsSizeString),
                OuterTube = duct_param.DuctSizeInfor.RecommendOuterDuctSize,
                InnerTube = duct_param.DuctSizeInfor.RecommendInnerDuctSize
            };
        }
    }
}
