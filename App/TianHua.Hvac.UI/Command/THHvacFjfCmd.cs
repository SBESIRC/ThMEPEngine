using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHAVC.CAD;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Model;
using ThCADExtension;
using TianHua.FanSelection.Function;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Hvac.UI.Command
{
    public class THHvacFjfCmd : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ObjectId fan_id = ObjectId.Null;
                DBObjectCollection lineobjects = get_fan_and_centerline(ref fan_id);
                if (fan_id.IsNull || lineobjects.Count == 0)
                    return;
                ThDbModelFan DbFanModel = new ThDbModelFan(fan_id, lineobjects);
                string innerDuctSize = string.Empty;
                string outerDuctSize = string.Empty;
                string airVloume = string.Empty;
                string elevation = string.Empty;
                string textSize = string.Empty;
                using (var dlg = create_duct_diag(DbFanModel))
                {
                    if (AcadApp.ShowModalDialog(dlg) == DialogResult.OK)
                    {
                        innerDuctSize = dlg.SelectedInnerDuctSize;
                        outerDuctSize = dlg.SelectedOuterDuctSize;
                        airVloume = dlg.AirVolume;
                        elevation = dlg.Elevation;
                        textSize = dlg.TextSize;
                    }
                }
                if (string.IsNullOrEmpty(innerDuctSize) || string.IsNullOrEmpty(outerDuctSize))
                {
                    return;
                }
                string DuctSize = innerDuctSize + " " + outerDuctSize;
                if (DbFanModel.FanScenario == "消防加压送风")
                {
                    string tee_width = string.Empty;
                    string tee_pattern = string.Empty;
                    using (var dlg = new fmBypass(airVloume))
                    {
                        if (AcadApp.ShowModalDialog(dlg) == DialogResult.OK)
                        {
                            tee_width = dlg.TeeWidth;
                            tee_pattern = dlg.tee_pattern;
                        }
                        else
                            return;
                    }

                    if (tee_pattern == "RBType4" || tee_pattern == "RBType5")
                    {
                        string line_type = (tee_pattern == "RBType4") ?
                            ThHvacCommon.CONTINUES_LINETYPE :
                            ThHvacCommon.DASH_LINETYPE;
                        double vt_width = 800;
                        ThVTee vt = new ThVTee(600, vt_width, 20);
                        double fan_angle = DbFanModel.FanOutlet.Angle;
                        Point3d valve_pos = DbFanModel.FanInletBasePoint;
                        ThFanInletOutletAnalysisEngine io_anay_res = IOAnalysis(DbFanModel, null);
                        if (io_anay_res == null)
                            return;
                        int wall_num = 0;
                        IODuctHoleAnalysis(DbFanModel, DuctSize, tee_width, false, ref wall_num, null, textSize, null, io_anay_res);
                        if (wall_num != 0)
                        {
                            double angle = fan_angle * Math.PI / 180;
                            vt.RunVTeeDrawEngine(DbFanModel, line_type, angle);
                            Vector3d fan_cp_vec = (DbFanModel.FanOutletBasePoint.GetAsVector() -
                                                   DbFanModel.FanInletBasePoint.GetAsVector()) * 0.5 +
                                                   DbFanModel.FanInletBasePoint.GetAsVector();
                            ThServiceTee.InsertElectricValve(fan_cp_vec, vt_width, angle + 1.5 * Math.PI, false);
                        }
                    }
                    else
                    {
                        // 添加旁通并添加到原Model中
                        bool is_type3 = tee_pattern == "RBType3";
                        DBObjectCollection bypass_lines = get_bypass(is_type3);
                        if (bypass_lines.Count == 0)
                            return;
                        bypass_lines.Cast<DBObject>().ForEachDbObject(o => lineobjects.Add(o));

                        // 将风管在旁通处打断
                        ThLaneLineEngine.extend_distance = 0.0;
                        var results = ThLaneLineEngine.Explode(lineobjects);
                        results = ThLaneLineEngine.Noding(results);
                        results = ThLaneLineEngine.CleanZeroCurves(results);

                        // 根据添加的旁通重新得到model
                        ThDbModelFan DbTeeModel = new ThDbModelFan(fan_id, results);
                        ThFanInletOutletAnalysisEngine io_anay_res = IOAnalysis(DbTeeModel, bypass_lines);
                        if (io_anay_res == null)
                            return;
                        double valve_width = Double.Parse(outerDuctSize.Split('x').First());
                        double bra_width = Double.Parse(tee_width.Split('x').First());
                        double s1 = bra_width + 50;
                        double s2 = (bra_width + valve_width) * 0.5 + 50;
                        double s3 = bra_width * 0.5 + 100;
                        Point3d bypass_start = Point3d.Origin;
                        Point3d bypass_end = Point3d.Origin;
                        double bypass_len = 0;
                        if (io_anay_res.HasInletTee())
                        {
                            //get_out_bypass(bypass_lines,
                            //               io_anay_res.InletTeeCPPositions[0],
                            //               ref bypass_start,
                            //               ref bypass_end,
                            //               ref bypass_len,
                            //               tee_pattern);
                            ThServiceTee.TeeFineTuneDuct(io_anay_res.InletCenterLineGraph, s3, s2, s1, bypass_lines);
                        }
                        if (io_anay_res.HasOutletTee())
                        {
                            get_out_bypass(bypass_lines,
                                           io_anay_res.OutletTeeCPPositions[0],
                                           ref bypass_start,
                                           ref bypass_end,
                                           ref bypass_len,
                                           tee_pattern);
                            ThServiceTee.TeeFineTuneDuct(io_anay_res.OutletCenterLineGraph, s1, s2, s3, bypass_lines);
                        }
                        int wall_num = 0;
                        bool is_type2 = tee_pattern == "RBType2";
                        IODuctHoleAnalysis(DbTeeModel, DuctSize, tee_width, is_type2, ref wall_num, elevation, textSize, bypass_lines, io_anay_res);
                        if (wall_num == 0)
                            return;
                        if (io_anay_res.HasInletTee() || io_anay_res.HasOutletTee())
                        {
                            //将阀插入在距outTee Cp 2000的位置
                            //Vector3d tmp_vec = (detect_p.GetAsVector() - p.GetAsVector()).GetNormal();
                            //Vector2d r_vec = new Vector2d(tmp_vec.X, tmp_vec.Y);
                            //Vector3d dis_vec = tmp_vec * 2000 + p.GetAsVector();
                            Vector3d tmp_vec = (bypass_end.GetAsVector() - bypass_start.GetAsVector()) * 0.5;
                            Vector2d r_vec = new Vector2d(tmp_vec.X, tmp_vec.Y);
                            Vector3d dis_vec = tmp_vec + bypass_start.GetAsVector();
                            double ang = (1.5 * Math.PI - r_vec.Angle);
                            bool has_dul_tee = io_anay_res.HasInletTee() && io_anay_res.HasOutletTee();
                            ThServiceTee.InsertElectricValve(dis_vec, bra_width, ang, has_dul_tee);
                        }
                    }
                }
                else
                {
                    ThFanInletOutletAnalysisEngine io_anay_res = IOAnalysis(DbFanModel, null);
                    int wall_num = 0;
                    IODuctHoleAnalysis(DbFanModel, DuctSize, null, false, ref wall_num, null, null, null, io_anay_res);
                }
            }
        }

        private void get_out_bypass(DBObjectCollection bypass_lines, 
                                    Point3d tee_cp, 
                                    ref Point3d bypass_start,
                                    ref Point3d bypass_end,
                                    ref double bypass_len,
                                    string type)
        {
            if (bypass_lines.Count == 2)
            {
                Line l1 = bypass_lines[0] as Line;
                Line l2 = bypass_lines[1] as Line;
                if (type == "RBType1")
                {
                    bypass_start = tee_cp;
                    if (tee_cp.IsEqualTo(l1.StartPoint))
                    {
                        bypass_end = l1.EndPoint;
                        bypass_len = l1.Length;
                    }
                    else if (tee_cp.IsEqualTo(l1.EndPoint))
                    {
                        bypass_end = l1.StartPoint;
                        bypass_len = l1.Length;
                    }
                    else if (tee_cp.IsEqualTo(l2.StartPoint))
                    {
                        bypass_end = l2.EndPoint;
                        bypass_len = l2.Length;
                    }
                    else if (tee_cp.IsEqualTo(l2.EndPoint))
                    {
                        bypass_end = l2.StartPoint;
                        bypass_len = l2.Length;
                    }
                }
                else if (type == "RBType2")
                {
                    if (tee_cp.IsEqualTo(l1.StartPoint))
                    {
                        // l2是out_bypass
                        bypass_start = l1.EndPoint;
                        if (bypass_start.IsEqualTo(l2.StartPoint))
                            bypass_end = l2.EndPoint;
                        else
                            bypass_end = l2.StartPoint;
                        bypass_len = l2.Length;
                    }
                    else if (tee_cp.IsEqualTo(l1.EndPoint))
                    {
                        // l2是out_bypass
                        bypass_start = l1.StartPoint;
                        if (bypass_start.IsEqualTo(l2.StartPoint))
                            bypass_end = l2.EndPoint;
                        else
                            bypass_end = l2.StartPoint;
                        bypass_len = l2.Length;
                    }
                    else if (tee_cp.IsEqualTo(l2.StartPoint))
                    {
                        // l1是out_bypass
                        bypass_start = l2.EndPoint;
                        if (bypass_start.IsEqualTo(l1.StartPoint))
                            bypass_end = l1.EndPoint;
                        else
                            bypass_end = l1.StartPoint;
                        bypass_len = l1.Length;
                    }
                    else if (tee_cp.IsEqualTo(l2.EndPoint))
                    {
                        // l1是out_bypass
                        bypass_start = l2.StartPoint;
                        if (bypass_start.IsEqualTo(l1.StartPoint))
                            bypass_end = l1.EndPoint;
                        else
                            bypass_end = l1.StartPoint;
                        bypass_len = l1.Length;
                    }
                }
            }
            if (type == "RBType3" && bypass_lines.Count > 2)
            {
                Line l1 = bypass_lines[bypass_lines.Count - 2] as Line;
                Line l2 = bypass_lines[bypass_lines.Count - 1] as Line;
                double tor = 20;
                if (l1.StartPoint.DistanceTo(l2.StartPoint) < tor)
                {
                    bypass_start = l1.EndPoint;
                    bypass_end = l2.EndPoint;
                }
                else if (l1.StartPoint.DistanceTo(l2.EndPoint) < tor)
                {
                    bypass_start = l1.EndPoint;
                    bypass_end = l2.StartPoint;
                }
                else if (l1.EndPoint.DistanceTo(l2.StartPoint) < tor)
                {
                    bypass_start = l1.StartPoint;
                    bypass_end = l2.EndPoint;
                }
                else if (l1.EndPoint.DistanceTo(l2.EndPoint) < tor)
                {
                    bypass_start = l1.StartPoint;
                    bypass_end = l2.StartPoint;
                }
                if (bypass_start.Y < bypass_end.Y)
                {
                    bypass_start = new Point3d(bypass_end.X, bypass_end.Y, 0);
                    bypass_end = new Point3d(bypass_start.X, bypass_start.Y, 0);
                }
                
                bypass_len = l1.Length;
            }
        }
        private ObjectIdCollection get_from_prompt(string prompt, bool only_able)
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
        private ObjectId classify_fan(ObjectIdCollection selections,
                                      DBObjectCollection center_lines)
        {
            ObjectId fan_id = ObjectId.Null;
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

        private DBObjectCollection get_fan_and_centerline(ref ObjectId fan_id)
        {
            var objIds = get_from_prompt("请选择风机和中心线", false);
            if (objIds.Count == 0)
                return new DBObjectCollection();
            var center_lines = new DBObjectCollection();
            fan_id = classify_fan(objIds, center_lines);
            var service = new ThLaneLineCleanService();
            return ThLaneLineEngine.Explode(service.Clean(center_lines));
        }

        private DBObjectCollection get_bypass(bool is_type3)
        {
            var objIds = get_from_prompt("请选择旁通管", true);
            if (objIds.Count == 0)
                return new DBObjectCollection();
            // 暂时只支持选择一个旁通
            Curve c = objIds[0].GetDBObject() as Curve;

            DBObjectCollection center_lines = new DBObjectCollection { c };
            List<Line> lines = ThLaneLineEngine.Explode(center_lines).Cast<Line>().ToList();
            // 给较长的线段上插点
            if (is_type3 && lines.Count > 0)
            {
                Line l = lines[0];
                foreach (Line line in lines)
                    if (line.Length > l.Length)
                        l = line;
                lines.Remove(l);
                Point3d lp = l.StartPoint.Y > l.EndPoint.Y ? l.EndPoint : l.StartPoint;
                Point3d up = l.StartPoint.Y > l.EndPoint.Y ? l.StartPoint : l.EndPoint;
                Vector2d v1 = new Vector2d(lp.X, lp.Y);
                Vector2d v2 = new Vector2d(up.X, up.Y);
                Vector2d v = (v2 - v1) * 0.5;
                Vector2d v_nor = v.GetNormal();
                double len = 5;
                Vector2d vt = v1 + v + len * v_nor;
                lines.Add(new Line(new Point3d(vt.X, vt.Y, 0), up));
                vt = v1 + v - len * v_nor;
                lines.Add(new Line(lp, new Point3d(vt.X, vt.Y, 0)));
            }
            return lines.Select(o => o.ExtendLine(1.0)).ToCollection();
        }

        private DBObjectCollection get_walls()
        {
            var wallobjects = new DBObjectCollection();
            var objIds = get_from_prompt("请选择内侧墙线", false);
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
            return ThLaneLineEngine.Explode(wallobjects);
        }

        private fmDuctSpec create_duct_diag(ThDbModelFan DbFanModel)
        {
            var air_volume = DbFanModel.LowFanVolume == 0 ? DbFanModel.FanVolume : DbFanModel.LowFanVolume;
            ThDuctParameter duct_param = new ThDuctParameter(air_volume, ThFanSelectionUtils.GetDefaultAirSpeed(DbFanModel.FanScenario));
            var ductModel = new DuctSpecModel()
            {
                AirSpeed = ThFanSelectionUtils.GetDefaultAirSpeed(DbFanModel.FanScenario),
                MaxAirSpeed = ThFanSelectionUtils.GetMaxAirSpeed(DbFanModel.FanScenario),
                MinAirSpeed = ThFanSelectionUtils.GetMinAirSpeed(DbFanModel.FanScenario),
                AirVolume = air_volume,

                ListOuterTube = new List<string>(duct_param.DuctSizeInfor.DefaultDuctsSizeString),
                ListInnerTube = new List<string>(duct_param.DuctSizeInfor.DefaultDuctsSizeString),
                OuterTube = duct_param.DuctSizeInfor.RecommendOuterDuctSize,
                InnerTube = duct_param.DuctSizeInfor.RecommendInnerDuctSize
            };
            fmDuctSpec fm = new fmDuctSpec();
            fm.InitForm(ductModel);
            return fm;
        }

        private ThFanInletOutletAnalysisEngine IOAnalysis(ThDbModelFan Model, DBObjectCollection bypass_lines)
        {
            ThFanInletOutletAnalysisEngine io_anay_res = new ThFanInletOutletAnalysisEngine(Model);
            io_anay_res.InletAnalysis(bypass_lines);
            io_anay_res.OutletAnalysis(bypass_lines);
            if (io_anay_res.InletAnalysisResult != AnalysisResultType.OK &&
                io_anay_res.OutletAnalysisResult != AnalysisResultType.OK)
            {
                return null;
            }
            return io_anay_res;
        }

        private void IODuctHoleAnalysis(ThDbModelFan Model,
                                        string duct_size,
                                        string tee_size,
                                        bool is_type2,
                                        ref int wall_num,
                                        string elevation,
                                        string text_size,
                                        DBObjectCollection bypass_line,
                                        ThFanInletOutletAnalysisEngine io_anay_res)
        {
            if (bypass_line != null && bypass_line.Count == 0)
                return;
            string[] str = duct_size.Split(' ');
            string innerDuctSize = str[0];
            string outerDuctSize = str[1];
            ThInletOutletDuctDrawEngine io_draw_eng =
                new ThInletOutletDuctDrawEngine(Model,
                                                innerDuctSize,
                                                outerDuctSize,
                                                tee_size,
                                                elevation,
                                                text_size,
                                                is_type2,
                                                bypass_line,
                                                io_anay_res.InletCenterLineGraph,
                                                io_anay_res.OutletCenterLineGraph);
            var wall_lines = get_walls();
            if (wall_lines.Count == 0)
                return;
            ThHolesAndValvesEngine holesAndValvesEngine =
                new ThHolesAndValvesEngine(Model,
                                           wall_lines,
                                           bypass_line,
                                           io_draw_eng.InletDuctWidth,
                                           io_draw_eng.OutletDuctWidth,
                                           io_draw_eng.TeeWidth,
                                           io_anay_res.InletCenterLineGraph,
                                           io_anay_res.OutletCenterLineGraph);
            if (io_anay_res.InletAnalysisResult == AnalysisResultType.OK)
            {
                if (io_anay_res.HasInletTee())
                {
                    int i = 0;
                    double IDuctWidth = io_draw_eng.InletDuctWidth;
                    io_draw_eng.RunInletDrawEngine(Model, text_size);
                    foreach (Point3d TeeCp in io_anay_res.InletTeeCPPositions)
                    {
                        ThTee e = new ThTee(TeeCp, io_draw_eng.TeeWidth, IDuctWidth, IDuctWidth);
                        Matrix3d mat = Matrix3d.Displacement(TeeCp.GetAsVector());
                        if (bypass_line.Count == 5)
                        {
                            mat *= Matrix3d.Rotation(Math.PI + io_anay_res.ICPAngle[i++], Vector3d.ZAxis, Point3d.Origin);
                        }
                        if (bypass_line.Count == 3)
                        {
                            mat *= Matrix3d.Rotation(io_anay_res.ICPAngle[i++], Vector3d.ZAxis, Point3d.Origin) *
                                   Matrix3d.Mirroring(new Line3d(Point3d.Origin, Vector3d.YAxis));
                        }
                        e.RunTeeDrawEngine(Model, mat);
                    }
                }
                else
                {
                    io_draw_eng.RunInletDrawEngine(Model, text_size);
                }
                holesAndValvesEngine.RunInletValvesInsertEngine();
            }

            if (io_anay_res.OutletAnalysisResult == AnalysisResultType.OK)
            {
                if (io_anay_res.HasOutletTee())
                {
                    int i = 0;
                    double ODuctWidth = io_draw_eng.OutletDuctWidth;
                    io_draw_eng.RunOutletDrawEngine(Model, text_size);
                    foreach (Point3d TeeCp in io_anay_res.OutletTeeCPPositions)
                    {
                        ThTee e = new ThTee(TeeCp, io_draw_eng.TeeWidth, ODuctWidth, ODuctWidth);
                        Matrix3d mat = Matrix3d.Displacement(TeeCp.GetAsVector());
                        double angle = io_anay_res.OCPAngle[i++];
                        if (is_type2)
                        {
                            mat *= Matrix3d.Rotation(-(1.5 * Math.PI - angle), Vector3d.ZAxis, Point3d.Origin);
                        }
                        else
                        {
                            if (bypass_line.Count == 5)
                            {
                                mat *= Matrix3d.Rotation(Math.PI + angle, Vector3d.ZAxis, Point3d.Origin);
                            }
                            if (bypass_line.Count == 3)
                            {
                                mat *= Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin) *
                                       Matrix3d.Mirroring(new Line3d(Point3d.Origin, Vector3d.YAxis));
                            }
                        }
                        e.RunTeeDrawEngine(Model, mat);
                    }
                }
                else
                {
                    io_draw_eng.RunOutletDrawEngine(Model, text_size);
                }
                holesAndValvesEngine.RunOutletValvesInsertEngine();
                wall_num = wall_lines.Count;
            }
        }
    }
}
