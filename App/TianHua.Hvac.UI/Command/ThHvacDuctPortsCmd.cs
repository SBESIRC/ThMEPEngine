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


namespace TianHua.Hvac.UI.Command
{
    public class Duct_port_param
    {
        public int port_num;
        public double air_volumn;
        public double elevation;
        public string scale;
        public string scenario;
        public string port_size;
        public string port_range;
        public string in_duct_size;
    }
    public class ThHvacDuctPortsCmd : IAcadCommand, IDisposable
    {
        public void Dispose() { }

        public void Execute()
        {
            using (var db = AcadDatabase.Active())
            {
                var center_lines = Get_lines_from_prompt("请选择中心线", false);
                if (center_lines.Count == 0)
                    return;
                var start_point = Get_point_from_prompt("选择起点");
                Get_duct_port_info(out Duct_port_param in_param);
                if (in_param.scale == null)
                    return;
                if (in_param.port_range.Contains("侧"))
                    in_param.port_num = (int)Math.Ceiling(in_param.port_num * 0.5);
                var graph_res = new ThDuctPortsAnalysis(center_lines, 
                                                        start_point, 
                                                        in_param.scenario, 
                                                        in_param.port_num, 
                                                        in_param.air_volumn, 
                                                        in_param.in_duct_size);
                var adjust_graph = new ThDuctPortsConstructor(graph_res, in_param.scenario);
                var judger = new ThDuctPortsJudger(graph_res.merged_endlines, adjust_graph.endline_segs);
                var painter = new ThDuctPortsDraw(in_param.scenario, 
                                                  in_param.port_range, 
                                                  judger.align_points, 
                                                  in_param.port_size, 
                                                  in_param.scale, 
                                                  in_param.elevation);
                painter.Draw(graph_res, adjust_graph);
            }
        }

        private void Get_duct_port_info(out Duct_port_param in_param)
        {
            in_param = new Duct_port_param();
            using (var dlg = new fmDuctPorts())
            {
                if (AcadApp.ShowModalDialog(dlg) == DialogResult.OK)
                {
                    in_param.port_num = dlg.port_num;
                    in_param.scenario = dlg.scenario;
                    in_param.scale = dlg.graph_scale;
                    in_param.elevation = dlg.elevation;
                    in_param.port_size = dlg.port_size;
                    in_param.air_volumn = dlg.air_volume;
                    in_param.port_range = dlg.port_range;
                    in_param.in_duct_size = dlg.duct_size;
                }
                else
                    return;
            }
        }

        private DBObjectCollection Get_lines_from_prompt(string prompt, bool only_able)
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
                var objIds = result.Value.GetObjectIds().ToObjectIdCollection();
                return Pre_proc(objIds, only_able);
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
        private DBObjectCollection Pre_proc(ObjectIdCollection objs, bool is_start)
        {
            var lines = objs.Cast<ObjectId>().Select(o => o.GetDBObject()).ToCollection();
            if (is_start)
                return lines;
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
