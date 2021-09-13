using System;
using System.Linq;
using System.Windows.Forms;
using Linq2Acad;
using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPHVAC.Model;
using Autodesk.AutoCAD.Geometry;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.Runtime;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacDuctPortsCmd : IAcadCommand, IDisposable
    {
        private bool is_integrate;
        private Point3d start_point;
        private ThMEPHVACParam inte_param;
        private DBObjectCollection inte_center_line;
        private static readonly ThMEPHVACParam in_param = new ThMEPHVACParam();
        public ThHvacDuctPortsCmd() { }
        public ThHvacDuctPortsCmd(bool is_integrate,
                                  Point3d start_point,
                                  ThMEPHVACParam inte_param,
                                  DBObjectCollection center_line)
        {
            this.inte_param = inte_param;
            this.start_point = start_point;
            inte_center_line = center_line;
            this.is_integrate = is_integrate;
        }
        public void Dispose() { }

        public void Execute()
        {
            Get_center_line_start_point(out DBObjectCollection center_lines);
            if (center_lines.Count == 0)
                return;
            Get_exclude_line("请选择不布置风口的线", out DBObjectCollection exclude_line);
            if (exclude_line.Count >= center_lines.Count)
            {
                ThMEPHVACService.Prompt_msg("没有选择要布置风口的管段");
                return;
            }
            if (!Get_duct_port_info())
                return;
            ThDuctPortsDrawService.Move_to_origin(start_point, exclude_line);
            var graph_res = new ThDuctPortsAnalysis(center_lines, exclude_line, Point3d.Origin, in_param);
            graph_res.Do_anay(in_param.port_num, new ThDuctPortsModifyPort(), center_lines);
            if (graph_res.merged_endlines.Count == 0)
            {
                ThMEPHVACService.Prompt_msg("选择错误起始点");
                return;
            }
            var adjust_graph = new ThDuctPortsConstructor(graph_res, in_param);
            var judger = new ThDuctPortsJudger(start_point, graph_res.is_recreate, graph_res.merged_endlines, adjust_graph.endline_segs);
            var painter = new ThDuctPortsDraw(start_point, in_param, judger.dir_align_points, judger.ver_align_points);
            painter.Draw(graph_res, adjust_graph);
            Modify_port_num();
        }
        private void Modify_port_num()
        {
            if (in_param.port_range.Contains("侧"))
                in_param.port_num *= 2;
        }
        private void Get_center_line_start_point(out DBObjectCollection center_lines)
        {
            using (var db = AcadDatabase.Active())
            {
                if (is_integrate)
                {
                    center_lines = inte_center_line;
                }
                else
                {
                    Proc_not_integer(out center_lines);
                }
            }
        }
        private void Proc_not_integer(out DBObjectCollection center_lines)
        {
            start_point = Get_point_from_prompt("选择起点");
            var dxfNames = new string[]
            {
                RXClass.GetClass(typeof(Line)).DxfName,
                RXClass.GetClass(typeof(Polyline)).DxfName,
            };
            if (start_point == null)
            {
                center_lines = new DBObjectCollection();
                return;
            }
            var sf = ThSelectionFilterTool.Build(dxfNames);
            center_lines = Get_center_line("请选择中心线", sf, out string layer);
            if (center_lines.Count == 0)
                return;
            var mat = Matrix3d.Displacement(start_point.GetAsVector());
            ThDuctPortsDrawService.Draw_lines(center_lines, mat, layer, out _);
        }
        private bool Get_duct_port_info()
        {
            in_param.is_redraw = false;
            if (is_integrate)
            {
                // 从集成面板获取参数
                if (inte_param.air_volume > 0)
                {
                    in_param.port_num = inte_param.port_num;
                    in_param.scenario = inte_param.scenario;
                    in_param.scale = inte_param.scale;
                    in_param.elevation = inte_param.elevation;
                    in_param.port_size = inte_param.port_size;
                    in_param.port_name = inte_param.port_name;
                    in_param.air_volume = inte_param.air_volume;
                    in_param.port_range = inte_param.port_range;
                    in_param.in_duct_size = inte_param.in_duct_size;
                    in_param.air_speed = inte_param.air_speed;
                    if (in_param.scale == null)
                        return false;
                    if (in_param.port_range.Contains("侧"))
                        in_param.port_num = (int)Math.Ceiling(in_param.port_num * 0.5);
                    return true;
                }
                return false;
            }
            else
            {
                // 从风平面面板获取参数
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
            }
            return false;
        }
        private void Get_exclude_line(string prompt, out DBObjectCollection exclude_line)
        {
            using (var db = AcadDatabase.Active())
            {
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = prompt,
                    RejectObjectsOnLockedLayers = true,
                };
                var result = Active.Editor.GetSelection(options);
                exclude_line = new DBObjectCollection();
                if (result.Status == PromptStatus.OK)
                {
                    var objIds = result.Value.GetObjectIds();
                    exclude_line = objIds.Cast<ObjectId>().Select(o => o.GetDBObject().Clone() as Line).ToCollection();
                }
            }
        }
        private DBObjectCollection Get_center_line(string prompt, SelectionFilter sf, out string layer)
        {
            layer = "0";
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = prompt,
                RejectObjectsOnLockedLayers = true,
            };
            var result = Active.Editor.GetSelection(options, sf);
            if (result.Status == PromptStatus.OK)
            {
                var objIds = result.Value.GetObjectIds();
                var coll = objIds.ToObjectIdCollection();
                layer = ThDuctPortsDrawService.Get_cur_layer(coll);
                var lines = coll.Cast<ObjectId>().Select(o => o.GetDBObject().Clone() as Curve).ToCollection();
                ThDuctPortsDrawService.Move_to_origin(start_point, lines);
                lines = ThMEPHVACLineProc.Pre_proc(lines);
                if (lines.Polygonize().Count != 0)
                {
                    ThMEPHVACService.Prompt_msg("中心线包含环");
                    return new DBObjectCollection();
                }
                ThDuctPortsDrawService.Remove_ids(objIds);
                return lines;
            }
            else
            {
                return new DBObjectCollection();
            }
        }
        private Point3d Get_point_from_prompt(string prompt)
        {
            var startRes = Active.Editor.GetPoint(prompt);
            return new Point3d (startRes.Value.X, startRes.Value.Y, 0);
        }
    }
}
