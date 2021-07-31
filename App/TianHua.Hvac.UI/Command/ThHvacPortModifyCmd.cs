using System;
using System.Windows.Forms;
using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPHVAC.Model;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacPortModifyCmd : IAcadCommand, IDisposable
    {
        private static DuctPortsParam in_param = new DuctPortsParam();
        public void Dispose() { }
        public void Execute()
        {
            var id = Get_start_node("选择起始节点");
            if (id == null)
                return;
            if (!Get_duct_port_info())
                return;
            var modifyer = new ThDuctPortsModifyPort(id, in_param);
            if (!modifyer.is_success)
            {
                ThDuctPortsService.Prompt_msg("中心线包含不同类型风口");
                return;
            }
            var graph_res = new ThDuctPortsAnalysis(modifyer.center_line, modifyer.exclude_line, Point3d.Origin, in_param);
            graph_res.Get_start_line(modifyer.center_line, Point3d.Origin, out Point3d search_point, out Line start_l);
            graph_res.Set_duct_info(search_point, start_l, modifyer);
            graph_res.Set_special_shape_info(search_point);
            if (graph_res.merged_endlines.Count == 0)
            {
                ThDuctPortsService.Prompt_msg("选择错误起始点");
                return;
            }
            var adjust_graph = new ThDuctPortsConstructor(graph_res, in_param);
            var judger = new ThDuctPortsJudger(modifyer.start_p, graph_res.is_recreate, graph_res.merged_endlines, adjust_graph.endline_segs);
            var painter = new ThDuctPortsDraw(modifyer.start_p, in_param, judger.dir_align_points, judger.ver_align_points);
            painter.Draw(graph_res, adjust_graph);
        }
        private ObjectId[] Get_start_node(string prompt)
        {
            var options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = prompt,
                RejectObjectsOnLockedLayers = true,
                AllowSubSelections = false,
                SingleOnly = true
            };
            var result = Active.Editor.GetSelection(options);
            return (result.Status == PromptStatus.OK) ? result.Value.GetObjectIds() : null;
        }
        private bool Get_duct_port_info()
        {
            var dlg = new fmDuctPorts(in_param);
            if (AcadApp.ShowModalDialog(dlg) == DialogResult.OK)
            {
                in_param.port_num = dlg.port_num;
                in_param.scenario = dlg.scenario;
                in_param.scale = dlg.graph_scale;
                in_param.elevation = dlg.elevation;
                in_param.port_size = dlg.port_size;
                in_param.port_name = dlg.port_name;
                in_param.air_volume = dlg.air_volume;
                in_param.port_range = dlg.port_range;
                in_param.in_duct_size = dlg.duct_size;
                in_param.air_speed = dlg.air_speed;
                if (in_param.scale == null)
                    return false;
                if (in_param.port_range.Contains("侧"))
                    in_param.port_num = (int)Math.Ceiling(in_param.port_num * 0.5);
                return true;
            }
            return false;
        }
    }
}
