using System;
using System.Windows.Forms;
using System.Collections.Generic;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.Geometry;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Model;
using TianHua.FanSelection.Function;

namespace TianHua.Hvac.UI.Command
{
    public struct Duct_InParam
    {
        public string bypass_size;
        public string bypass_pattern;
        public string room_duct_size;
        public string other_duct_size;
        public string scale;
        public string room_elevation;
        public string other_elevation;
    }
    public class ThHvacFjfCmd : IAcadCommand, IDisposable
    {
        ThHvacCmdService cmdService;
        public ThHvacFjfCmd() { }
        public ThHvacFjfCmd(bool isIntegrate)
        {
            cmdService = new ThHvacCmdService(isIntegrate);
        }
        public void Dispose() { }
        public void Execute()
        {
            var fan_id = cmdService.GetFan();
            if (fan_id == ObjectId.Null)
            {
                ThMEPHVACService.PromptMsg("未选择到风机");
                return;
            }
            var fan = new ThDbModelFan(fan_id);
            if (fan.airVolume <= 0)
            {
                ThMEPHVACService.PromptMsg("风机风量为0");
                return;
            }
            var centerLines = cmdService.GetCenterLines(fan.FanInletBasePoint, fan.FanOutletBasePoint);
            if (centerLines.Count == 0)
                return;
            var wallLines = cmdService.GetWalls();
            if (wallLines.Count == 0)
                ThMEPHVACService.PromptMsg("未选择墙线");
            var ductParam = Get_duct_info(fan, out string airVolume, out ThMEPHVACParam fanParam);
            if (string.IsNullOrEmpty(airVolume))
            {
                ThMEPHVACService.PromptMsg("风机风量为0");
                return;
            }
            if (fan.scenario == "消防加压送风")
            {
                Get_bypass_info(fan, ref ductParam);
                if (string.IsNullOrEmpty(ductParam.bypass_pattern))
                {
                    ThMEPHVACService.PromptMsg("未选择旁通模式");
                    return;
                }
                var bypassLines = Get_bypass(ductParam.bypass_pattern);
                Get_rid_bypass_of_centerline(ref centerLines, bypassLines);
                // 两侧都生成
                //cmdService.PressurizedAirSupply(ductParam, fan, centerLines, wallLines, fanParam, ref bypassLines, true, true);
            }
            else
            {
                //cmdService.NotPressurizedAirSupply(ductParam, fan, centerLines, wallLines, fanParam, true, true);
            }
        }
        
        private void Get_rid_bypass_of_centerline(ref DBObjectCollection center_lines, DBObjectCollection bypass_lines)
        {
            var tor = new Tolerance(1.5, 1.5);
            foreach (Line l in bypass_lines)
            {
                foreach (Line shadow in center_lines)
                {
                    if (ThMEPHVACService.IsSameLine(shadow, l, tor))
                    {
                        center_lines.Remove(shadow);
                        break;
                    }
                }
            }
        }
        private void Fix_duct_size_info(ref Duct_InParam info)
        {
            var w1 = ThMEPHVACService.GetWidth(info.room_duct_size);
            var w2 = ThMEPHVACService.GetWidth(info.other_duct_size);
            var big_size = w1 > w2 ? info.room_duct_size : info.other_duct_size;
            var small_size = w1 < w2 ? info.room_duct_size : info.other_duct_size;
            info.room_duct_size = big_size;
            info.other_duct_size = small_size;
        }
        private Duct_InParam Get_duct_info(ThDbModelFan fan, out string airVolume, out ThMEPHVACParam fpm_param)
        {
            airVolume = string.Empty;
            fpm_param = new ThMEPHVACParam();
            if (cmdService.isIntegrate)
            {
                using (var dlg = Create_integrate_diag(fan))
                {
                    if (AcadApp.ShowModalDialog(dlg) == DialogResult.OK)
                    {
                        var fjf_param = Get_fjf_param(dlg, fan.isExhaust, out airVolume);
                        Fix_duct_size_info(ref fjf_param);
                        fpm_param = Get_fpm_param(dlg, fan.isExhaust, fjf_param.room_duct_size);
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
                        var fjf_param = Get_fjf_param(dlg, fan.isExhaust, out airVolume);
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
            info.portNum = dlg.port_num;
            info.scenario = dlg.scenario;
            info.scale = dlg.scale;
            var elevation = is_exhaust ? dlg.elevation1.ToString() : dlg.elevation2.ToString();
            info.elevation = Double.Parse(elevation);
            info.portSize = dlg.port_size;
            info.portName = dlg.port_name;
            info.airVolume = dlg.air_volume;
            info.highAirVolume = dlg.high_air_volume;
            info.portRange = dlg.port_range;
            info.inDuctSize = room_duct_size;//room 侧
            info.airSpeed = dlg.air_speed;
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
        private DBObjectCollection Get_bypass(string tee_pattern)
        {
            using (var db = AcadDatabase.Active())
            {
                if (tee_pattern == null)
                    return new DBObjectCollection();
                if (tee_pattern == "RBType4" || tee_pattern == "RBType5")
                    return new DBObjectCollection();
                return cmdService.GetBypass();
            }
        }
        private fmBypass Create_bypass_diag(ThDbModelFan fan)
        {
            var info = Get_duct_info(fan.airVolume, fan.scenario, fan.strAirVolume);
            var fm = new fmBypass(fan.airVolume);
            fm.InitForm(info);
            return fm;
        }
        private fmDuctSpec Create_duct_diag(ThDbModelFan fan)
        {
            bool is_exhaust = !(fan.scenario.Contains("补") || fan.scenario.Contains("送"));
            var info = Get_duct_info(fan.airVolume, fan.scenario, fan.strAirVolume);
            var fm = new fmDuctSpec();
            fm.InitForm(info, is_exhaust);
            return fm;
        }
        private fmFjfFpm Create_integrate_diag(ThDbModelFan fan)
        {
            bool is_exhaust = !(fan.scenario.Contains("补") || fan.scenario.Contains("送"));
            var info = Get_duct_info(fan.airVolume, fan.scenario, fan.strAirVolume);
            var fm = new fmFjfFpm(info, is_exhaust, fan.scenario);
            return fm;
        }
        private DuctSpecModel Get_duct_info(double airVolume, string scenario, string strAirVolume)
        {
            //var duct_param = new ThDuctParameter(airVolume, "");// 不用了
            return new DuctSpecModel()
            {
                AirVolume = airVolume,
                StrAirVolume = strAirVolume,
                AirSpeed = ThFanSelectionUtils.GetDefaultAirSpeed(scenario),
                MaxAirSpeed = ThFanSelectionUtils.GetMaxAirSpeed(scenario),
                MinAirSpeed = ThFanSelectionUtils.GetMinAirSpeed(scenario),
                ListOuterTube = new List<string>(),
                ListInnerTube = new List<string>(),
                OuterTube = "",
                InnerTube = ""
            };
        }
    }
}
