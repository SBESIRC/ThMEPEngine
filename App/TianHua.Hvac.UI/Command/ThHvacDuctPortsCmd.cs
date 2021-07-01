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
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Service;
using ThMEPHVAC.Model;
using Autodesk.AutoCAD.Geometry;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.Runtime;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacDuctPortsCmd : IAcadCommand, IDisposable
    {
        private static readonly DuctPortsParam in_param = new DuctPortsParam();
        public void Dispose() { }

        public void Execute()
        {
            Get_center_line_start_point(out Point3d start_point, out DBObjectCollection center_lines);
            if (center_lines.Count == 0)
                return;
            Get_exclude_line("请选择不布置风口的线", out DBObjectCollection exclude_line);
            if (exclude_line.Count >= center_lines.Count)
            {
                Prompt_msg("没有选择要布置风口的管段");
                return;
            }
            if (!Get_duct_port_info())
                return;
            if (in_param.scale == null)
                return;
            if (in_param.port_range.Contains("侧"))
                in_param.port_num = (int)Math.Ceiling(in_param.port_num * 0.5);
            var graph_res = new ThDuctPortsAnalysis(center_lines, exclude_line, start_point, in_param);
            if (graph_res.merged_endlines.Count == 0)
            {
                Prompt_msg("选择错误起始点");
                return;
            }
            var adjust_graph = new ThDuctPortsConstructor(graph_res, in_param);
            var judger = new ThDuctPortsJudger(graph_res.merged_endlines, adjust_graph.endline_segs);
            var painter = new ThDuctPortsDraw(in_param, judger.dir_align_points, judger.ver_align_points);
            painter.Draw(graph_res, adjust_graph);
        }
        private void Prompt_msg(string message)
        {
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(message);
        }
        private void Get_center_line_start_point(out Point3d start_point, out DBObjectCollection center_lines)
        {
            using (var db = AcadDatabase.Active())
            {
                start_point = Get_point_from_prompt("选择起点");
                if (start_point.DistanceTo(Point3d.Origin) < 1e-3)
                {
                    center_lines = new DBObjectCollection();
                    return;
                }
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Line)).DxfName,
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var sf = ThSelectionFilterTool.Build(dxfNames);
                center_lines = Get_center_line("请选择中心线", out string layer, sf);
                if (center_lines.Count == 0)
                    return;
                ThDuctPortsDrawService.Draw_lines(center_lines, Matrix3d.Identity, layer, out _);
            }
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
                in_param.air_volumn = dlg.air_volume;
                in_param.port_range = dlg.port_range;
                in_param.in_duct_size = dlg.duct_size;
                in_param.air_speed = dlg.air_speed;
                return true;
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
                    exclude_line = objIds.Cast<ObjectId>().Select(o => o.GetDBObject()).ToCollection();
                }
            }
        }
        private DBObjectCollection Get_center_line(string prompt, out string layer, SelectionFilter sf)
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
                var lines = Pre_proc(coll);
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
        private DBObjectCollection Pre_proc(ObjectIdCollection objs)
        {
            var lines = objs.Cast<ObjectId>().Select(o => o.GetDBObject()).ToCollection();
            var service = new ThLaneLineCleanService();
            var res = ThLaneLineEngine.Explode(service.Clean(lines));
            var extendLines = new DBObjectCollection();
            foreach (Line line in res)
            {
                extendLines.Add(line.ExtendLine(1.0));
            }
            lines = ThLaneLineEngine.Noding(extendLines);
            lines = ThLaneLineEngine.CleanZeroCurves(lines);
            lines = lines.LineMerge();
            lines = ThLaneLineEngine.Explode(lines);
            return lines;
        }
    }
}
