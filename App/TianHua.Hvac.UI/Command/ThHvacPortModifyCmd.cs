using System;
using System.Windows.Forms;
using Linq2Acad;
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
        private static readonly DuctPortsParam in_param = new DuctPortsParam();
        public void Dispose() { }
        public void Execute()
        {
            var id = Get_start_node("选择起始节点");
            if (id == null)
                return;
            if (!Get_duct_port_info())
                return;
            var modifyer = new ThDuctPortsModifyPort(id, in_param);
            if (modifyer.status != ModifyerStatus.OK)
            {
                Prompt_error(modifyer.status);
                Modify_port_num();
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
            Modify_port_num();
        }
        private void Modify_port_num()
        {
            if (in_param.port_range.Contains("侧"))
                in_param.port_num *= 2;
        }
        private void Prompt_error(ModifyerStatus status)
        {
            switch (status)
            {
                case ModifyerStatus.NO_PORT:
                    ThDuctPortsService.Prompt_msg("没有与中心管道相交的风口");
                    break;
                case ModifyerStatus.MULTI_PORT_RANGE:
                    ThDuctPortsService.Prompt_msg("有多种类型的风口与中心管道相交");
                    break;
                case ModifyerStatus.PORT_CROSS_MULTI_ENTITY:
                    ThDuctPortsService.Prompt_msg("风口与多个组相交(风口处于弯头三通四通附近)");
                    break;
            }
        }
        private ObjectId[] Get_start_node(string prompt)
        {
            using (var db = AcadDatabase.Active())
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
                if (result.Status != PromptStatus.OK)
                    return null;
                var id = result.Value.GetObjectIds();
                if (id.Length > 1)
                {
                    ThDuctPortsService.Prompt_msg("请选择AI-风管起点");
                    return null;
                }
                if (id[0].GetEntity() is BlockReference)
                {
                    var blk = id[0].GetEntity() as BlockReference;
                    if (blk.Name != "AI-风管起点")
                    {
                        ThDuctPortsService.Prompt_msg("请选择AI-风管起点");
                        return null;
                    }
                }
                else
                {
                    ThDuctPortsService.Prompt_msg("请选择AI-风管起点");
                    return null;
                }
                return (result.Status == PromptStatus.OK) ? result.Value.GetObjectIds() : null;
            }  
        }
        private bool Get_duct_port_info()
        {
            in_param.is_redraw = true;
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
                in_param.is_redraw = dlg.is_redraw;
                return true;
            }
            return false;
        }
    }
}
