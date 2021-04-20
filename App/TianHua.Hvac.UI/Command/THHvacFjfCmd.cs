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
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Model;
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
                DBObjectCollection lineobjects = Get_fan_and_centerline(ref fan_id);
                if (fan_id.IsNull || lineobjects.Count == 0)
                    return;
                ThDbModelFan DbFanModel = new ThDbModelFan(fan_id, lineobjects);

                Duct_InParam info = Get_duct_info(DbFanModel, out string air_volume);
                if (string.IsNullOrEmpty(air_volume))
                    return;
                if (DbFanModel.FanScenario == "消防加压送风")
                {
                    Get_bypass_info(DbFanModel, air_volume, ref info);
                    if (string.IsNullOrEmpty(info.tee_pattern))
                        return;
                    string tee_pattern = info.tee_pattern;
                    if (tee_pattern == "RBType4" || tee_pattern == "RBType5")
                    {
                        ThFanInletOutletAnalysisEngine io_anay_res = Io_analysis(DbFanModel, null);
                        if (io_anay_res == null)
                            return;
                        var wall_lines = Get_walls();
                        if (wall_lines.Count == 0)
                            return;
                        IO_duct_hole_analysis(DbFanModel, info, 0, wall_lines, null, io_anay_res);

                        Draw_VT_Prepare(info, DbFanModel, out double vt_width, out string line_type, out Vector2d rot_vec, out Vector3d dis_vec);
                        if (string.IsNullOrEmpty(line_type))
                            return;
                        ThVTee vt = new ThVTee(600, vt_width, 20);
                        vt.RunVTeeDrawEngine(DbFanModel, info, line_type, rot_vec, dis_vec);
                        ThServiceTee service = new ThServiceTee();
                        service.Run_insert_text_info(DbFanModel, info, rot_vec, dis_vec);
                        service.Insert_electric_valve(dis_vec, vt_width, rot_vec.Angle + 1.5 * Math.PI);
                    }
                    else
                    {
                        // 添加旁通并添加到原Model中
                        DBObjectCollection bypass_lines = Get_bypass(tee_pattern, out Line max_bypass);
                        if (bypass_lines.Count == 0)
                            return;
                        DBObjectCollection scatter_lines = Rebuild_graph(bypass_lines, lineobjects);
                        if (scatter_lines.Count == 0)
                            return;

                        // 根据添加的旁通重新得到model
                        ThDbModelFan DbTeeModel = new ThDbModelFan(fan_id, scatter_lines);
                        ThFanInletOutletAnalysisEngine io_anay_res = Io_analysis(DbTeeModel, bypass_lines);
                        if (io_anay_res == null)
                            return;

                        double bra_width = Double.Parse(info.tee_info.Split('x').First());
                        if (io_anay_res.HasInletTee())
                            Adjust_duct_shrink(true, bra_width, info.in_duct_info, io_anay_res.InTeesInfo, bypass_lines, io_anay_res);
                        if (io_anay_res.HasOutletTee())
                            Adjust_duct_shrink(false, bra_width, info.out_duct_info, io_anay_res.OutTeesInfo, bypass_lines, io_anay_res);
                        Line bypass_duct = max_bypass;
                        if (tee_pattern == "RBType2")
                            bypass_duct = io_anay_res.LastBypass;

                        var wall_lines = Get_walls();
                        if (wall_lines.Count == 0)
                            return;

                        IO_duct_hole_analysis(DbTeeModel, info, max_bypass.Length, wall_lines, bypass_lines, io_anay_res);
                        Shrink_bypass(ref bypass_duct, io_anay_res);
                        if (io_anay_res.HasInletTee() || io_anay_res.HasOutletTee())
                        {
                            if (tee_pattern == "RBType2")
                                bypass_duct = io_anay_res.LastBypass;
                            else if (tee_pattern == "RBType1")
                                bypass_duct = io_anay_res.MaxBypass;
                            else
                                bypass_duct = max_bypass;
                            Point3d bypass_start = bypass_duct.StartPoint;
                            Point3d bypass_end = bypass_duct.EndPoint;
                            Vector3d bypass_vec = (bypass_end.GetAsVector() - bypass_start.GetAsVector());
                            Vector3d dis_vec = bypass_start.GetAsVector() + bypass_vec * 0.5;

                            Vector2d elev_dir = new Vector2d(bypass_vec.X, bypass_vec.Y);
                            double angle = elev_dir.Angle + Math.PI * 1.5;
                            ThServiceTee service = new ThServiceTee();
                            service.Run_insert_text_info(DbTeeModel, info, 
                                                         new Vector2d(bypass_vec.X, bypass_vec.Y),
                                                         dis_vec);
                            service.Insert_electric_valve(dis_vec, bra_width, angle);
                        }
                    }
                }
                else
                {
                    ThFanInletOutletAnalysisEngine io_anay_res = Io_analysis(DbFanModel, null);
                    IO_duct_hole_analysis(DbFanModel, info, 0, null, null, io_anay_res);
                }
            }
        }

        private void Draw_VT_Prepare(Duct_InParam info,
                                     ThDbModelFan DbFanModel,
                                     out double vt_width,
                                     out string line_type,
                                     out Vector2d rot_vec,
                                     out Vector3d dis_vec)
        {
            string[] s = info.tee_info.Split('x');
            if (s.Length == 0)
            {
                vt_width = 0;
                line_type = string.Empty;
                rot_vec = new Vector2d();
                dis_vec = new Vector3d();
                return;
            }
            vt_width = Double.Parse(s[0]);
            Vector3d dir_vec = (DbFanModel.FanOutletBasePoint.GetAsVector() -
                                DbFanModel.FanInletBasePoint.GetAsVector()) * 0.5;
            dis_vec = dir_vec + DbFanModel.FanInletBasePoint.GetAsVector();
            rot_vec = new Vector2d(dir_vec.X, dir_vec.Y);
            line_type = (info.tee_pattern == "RBType4") ? ThHvacCommon.CONTINUES_LINETYPE : ThHvacCommon.DASH_LINETYPE;
        }

        private void Adjust_duct_shrink(bool is_in,
                                        double bra_width,
                                        string duct_size,
                                        List<TeeInfo> InTeesInfo,
                                        DBObjectCollection bypass_lines,
                                        ThFanInletOutletAnalysisEngine io_anay_res)
        {
            double valve_width = Double.Parse(duct_size.Split('x').First());
            double s1 = (valve_width + bra_width) * 0.5 + 50;
            double s2 = valve_width * 0.5 + 100;
            double s3 = valve_width + 50;
            foreach (TeeInfo tee_info in InTeesInfo)
            {
                if (tee_info.tee_type == TeeType.TEE_ON_THE_RIGHT_OF_INNER)
                {
                    if (is_in)
                        ThServiceTee.Fine_tee_duct(io_anay_res.InletCenterLineGraph, s1, s3, s2, bypass_lines);
                    else
                        ThServiceTee.Fine_tee_duct(io_anay_res.OutletCenterLineGraph, s1 + 65, s2, s3, bypass_lines);
                }
                else if (tee_info.tee_type == TeeType.TEE_ON_THE_LEFT_OF_INNER)
                {
                    if (is_in)
                        ThServiceTee.Fine_tee_duct(io_anay_res.InletCenterLineGraph, s3, s2, s3, bypass_lines);
                    else
                        ThServiceTee.Fine_tee_duct(io_anay_res.OutletCenterLineGraph, s3, s2, s3, bypass_lines);
                }
                else if (tee_info.tee_type == TeeType.TEE_COLLINEAR_WITH_INNER)
                {
                    if (is_in)
                        ThServiceTee.Fine_tee_duct(io_anay_res.InletCenterLineGraph, s3, s2, s3, bypass_lines);
                    else
                        ThServiceTee.Fine_tee_duct(io_anay_res.OutletCenterLineGraph, s3, s2, s3, bypass_lines);
                }
                else
                {
                    s1 = (valve_width + bra_width) * 0.5 + 50;
                    s2 = bra_width * 0.5 + 100;
                    s3 = bra_width + 50;
                    if (is_in)
                        ThServiceTee.Fine_tee_duct(io_anay_res.InletCenterLineGraph, s3, s1, s2, bypass_lines);
                    else
                        ThServiceTee.Fine_tee_duct(io_anay_res.OutletCenterLineGraph, s3, s1, s2, bypass_lines);
                }
                
            }
        }
        private Duct_InParam Get_duct_info(ThDbModelFan DbFanModel, out string air_volume)
        {
            air_volume = string.Empty;
            Duct_InParam info = new Duct_InParam();
            using (var dlg = Create_duct_diag(DbFanModel))
            {
                if (AcadApp.ShowModalDialog(dlg) == DialogResult.OK)
                {
                    info.in_duct_info = dlg.SelectedInnerDuctSize;
                    info.out_duct_info = dlg.SelectedOuterDuctSize;
                    air_volume = dlg.AirVolume;
                    info.elevation_info = dlg.Elevation;
                    info.text_size_info = dlg.TextSize;
                }
            }
            return info;
        }
        private void Get_bypass_info(ThDbModelFan DbFanModel, string air_volume, ref Duct_InParam info)
        {
            using (var dlg = Create_bypass_diag(DbFanModel, air_volume))
            {
                if (AcadApp.ShowModalDialog(dlg) == DialogResult.OK)
                {
                    info.tee_info = dlg.TeeWidth;
                    info.tee_pattern = dlg.tee_pattern;
                }
                else
                    return;
            }
        }

        private DBObjectCollection Rebuild_graph(DBObjectCollection bypass_lines,
                                                 DBObjectCollection lineobjects)
        {
            if (bypass_lines.Count == 0)
                return new DBObjectCollection();
            bypass_lines.Cast<DBObject>().ForEachDbObject(o => lineobjects.Add(o));

            // 将风管在旁通处打断
            ThLaneLineEngine.extend_distance = 0.0;
            var results =ThLaneLineEngine.Explode(lineobjects);
            results = ThLaneLineEngine.Noding(results);
            return ThLaneLineEngine.CleanZeroCurves(results);
        }

        private void Shrink_bypass(ref Line bypass, ThFanInletOutletAnalysisEngine graph)
        {
            double tor = 1.5;
            double len1 = bypass.Length;
            double len2 = len1 * 0.5;

            if (graph.HasOutletTee())
            {
                foreach (var l in graph.OutletCenterLineGraph.Edges)
                {
                    if (Math.Abs(l.EdgeLength - len1) < tor || Math.Abs(l.EdgeLength - len2) < tor)
                    {
                        Point3d sp = bypass.StartPoint;
                        Point3d ep = bypass.EndPoint;
                        Vector3d cp_vec = new Vector3d((sp.X + ep.X) * 0.5, (sp.Y + ep.Y) * 0.5, 0);
                        Vector3d s_vec = new Vector3d(sp.X, sp.Y, 0);
                        Vector3d e_vec = new Vector3d(ep.X, ep.Y, 0);
                        s_vec = (cp_vec - s_vec).GetNormal() * l.TargetShrink + s_vec;
                        e_vec = (cp_vec - e_vec).GetNormal() * l.SourceShrink + e_vec;
                        bypass = new Line(new Point3d(s_vec.X, s_vec.Y, 0),
                                          new Point3d(e_vec.X, e_vec.Y, 0));
                    }
                }
            }
            else
            {
                foreach (var l in graph.InletCenterLineGraph.Edges)
                {
                    if (Math.Abs(l.EdgeLength - len1) < tor || Math.Abs(l.EdgeLength - len2) < tor)
                    {
                        Point3d sp = bypass.StartPoint;
                        Point3d ep = bypass.EndPoint;
                        Vector3d cp_vec = new Vector3d((sp.X + ep.X) * 0.5, (sp.Y + ep.Y) * 0.5, 0);
                        Vector3d s_vec = new Vector3d(sp.X, sp.Y, 0);
                        Vector3d e_vec = new Vector3d(ep.X, ep.Y, 0);
                        s_vec = (cp_vec - s_vec).GetNormal() * l.SourceShrink + s_vec;
                        e_vec = (cp_vec - e_vec).GetNormal() * l.TargetShrink + e_vec;
                        bypass = new Line(new Point3d(s_vec.X, s_vec.Y, 0),
                                          new Point3d(e_vec.X, e_vec.Y, 0));
                    }
                }
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
        private ObjectId Classify_fan(ObjectIdCollection selections,
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

        private DBObjectCollection Get_fan_and_centerline(ref ObjectId fan_id)
        {
            var objIds = Get_from_prompt("请选择风机和中心线", false);
            if (objIds.Count == 0)
                return new DBObjectCollection();
            var center_lines = new DBObjectCollection();
            fan_id = Classify_fan(objIds, center_lines);
            var service = new ThLaneLineCleanService();
            return ThLaneLineEngine.Explode(service.Clean(center_lines));
        }

        private DBObjectCollection Pre_proc_bypass(ObjectIdCollection bypass)
        {
            var service = new ThLaneLineCleanService();
            var results = service.Clean(bypass.Cast<ObjectId>().Select(o => o.GetDBObject()).ToCollection());
            return results.LineMerge();
        }

        private DBObjectCollection Get_bypass(string tee_pattern, out Line max_bypass)
        {
            // 暂时只支持选取一根连通的旁通线
            var objIds = Get_from_prompt("请选择旁通管", false);
            if (objIds.Count == 0)
            {
                max_bypass = new Line();
                return new DBObjectCollection();
            }
            DBObjectCollection center_lines = Pre_proc_bypass(objIds);
            if (center_lines.Count != 1)
            {
                max_bypass = new Line();
                return new DBObjectCollection();
            }

            List<Line> lines = ThLaneLineEngine.Explode(center_lines).Cast<Line>().ToList();
            Line l = lines[0];
            foreach (Line line in lines)
                if (line.Length > l.Length)
                    l = line;
            max_bypass = l;
            // 给较长的线段上插点
            if (tee_pattern == "RBType3" && lines.Count > 0)
            {
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

            return lines.Select(o => o.ExtendLine(2.0)).ToCollection();
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
            return ThLaneLineEngine.Explode(wallobjects);
        }

        private fmBypass Create_bypass_diag(ThDbModelFan DbFanModel, string airVloume)
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
            fmBypass fm = new fmBypass(airVloume);
            fm.InitForm(ductModel);
            return fm;

        }

        private fmDuctSpec Create_duct_diag(ThDbModelFan DbFanModel)
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

        private ThFanInletOutletAnalysisEngine Io_analysis(ThDbModelFan Model, DBObjectCollection bypass_lines)
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

        private void IO_duct_hole_analysis(ThDbModelFan Model,
                                        Duct_InParam pst_param,
                                        double selected_bypass_len,
                                        DBObjectCollection wall_lines,
                                        DBObjectCollection bypass_line,
                                        ThFanInletOutletAnalysisEngine io_anay_res)
        {
            string text_size = pst_param.text_size_info;

            if ((bypass_line != null && bypass_line.Count == 0) || (wall_lines == null))
                return;

            ThInletOutletDuctDrawEngine io_draw_eng =
                new ThInletOutletDuctDrawEngine(Model, pst_param,
                                                selected_bypass_len, bypass_line,
                                                io_anay_res.InletCenterLineGraph,
                                                io_anay_res.OutletCenterLineGraph);
            
            ThHolesAndValvesEngine holesAndValvesEngine =
                new ThHolesAndValvesEngine(Model, wall_lines, bypass_line, io_draw_eng, io_anay_res.InletCenterLineGraph, io_anay_res.OutletCenterLineGraph);
            if (io_anay_res.InletAnalysisResult == AnalysisResultType.OK)
            {
                io_draw_eng.RunInletDrawEngine(Model, text_size);
                if (io_anay_res.HasInletTee())
                    Draw_tee(true, io_draw_eng.TeeWidth, io_draw_eng.InletDuctWidth, Model, io_anay_res.InTeesInfo);
                holesAndValvesEngine.RunInletValvesInsertEngine();
            }

            if (io_anay_res.OutletAnalysisResult == AnalysisResultType.OK)
            {
                io_draw_eng.RunOutletDrawEngine(Model, text_size);
                if (io_anay_res.HasOutletTee())
                    Draw_tee(false, io_draw_eng.TeeWidth, io_draw_eng.OutletDuctWidth, Model, io_anay_res.OutTeesInfo);
                holesAndValvesEngine.RunOutletValvesInsertEngine();
                
            }
        }

        private void Draw_tee(bool is_in,
                              double tee_width,
                              double duct_width,
                              ThDbModelFan Model,
                              List<TeeInfo> tees_info)
        {
            foreach (TeeInfo tee_info in tees_info)
            {
                Vector3d dir = tee_info.dir;
                Point3d tee_cp = tee_info.position;
                Matrix3d mat = Matrix3d.Displacement(tee_cp.GetAsVector());
                ThTee e;
                if (tee_info.tee_type == TeeType.TEE_COLLINEAR_WITH_INNER)
                {
                    if (is_in)
                        mat *= Matrix3d.Rotation(tee_info.angle.Angle - Math.PI * 0.5, Vector3d.ZAxis, Point3d.Origin);
                    else
                        mat *= Matrix3d.Rotation(tee_info.angle.Angle - Math.PI * 0.5, Vector3d.ZAxis, Point3d.Origin) *
                               Matrix3d.Mirroring(new Line3d(Point3d.Origin, Vector3d.YAxis));
                    e = new ThTee(tee_cp, duct_width, duct_width, tee_width);
                    e.RunTeeDrawEngine(Model, mat);
                }
                else if (tee_info.tee_type == TeeType.TEE_ON_THE_LEFT_OF_INNER)
                {
                    if (is_in)
                        mat *= Matrix3d.Rotation(tee_info.angle.Angle - Math.PI * 0.5, Vector3d.ZAxis, Point3d.Origin) *
                               Matrix3d.Mirroring(new Line3d(Point3d.Origin, Vector3d.YAxis));
                    else
                        mat *= Matrix3d.Rotation(tee_info.angle.Angle - Math.PI * 0.5, Vector3d.ZAxis, Point3d.Origin) *
                               Matrix3d.Mirroring(new Line3d(Point3d.Origin, Vector3d.YAxis));
                    e = new ThTee(tee_cp, duct_width, duct_width, tee_width);
                    e.RunTeeDrawEngine(Model, mat);
                }
                else if (tee_info.tee_type == TeeType.TEE_ON_THE_RIGHT_OF_INNER)
                {
                    if (is_in)
                        mat *= Matrix3d.Rotation(tee_info.angle.Angle - Math.PI * 0.5, Vector3d.ZAxis, Point3d.Origin) *
                               Matrix3d.Mirroring(new Line3d(Point3d.Origin, Vector3d.YAxis));
                    else
                        mat *= Matrix3d.Rotation(tee_info.angle.Angle - Math.PI * 0.5, Vector3d.ZAxis, Point3d.Origin);
                    e = new ThTee(tee_cp, duct_width, duct_width, tee_width);
                    e.RunTeeDrawEngine(Model, mat);
                }
                else if (tee_info.tee_type == TeeType.TEE_VERTICAL_WITH_OTHERS)
                {
                    if (dir.Z < 0)
                        mat *= Matrix3d.Rotation(tee_info.angle.Angle + Math.PI, Vector3d.ZAxis, Point3d.Origin) *
                               Matrix3d.Mirroring(new Line3d(Point3d.Origin, Vector3d.YAxis));
                    else
                        mat *= Matrix3d.Rotation(tee_info.angle.Angle, Vector3d.ZAxis, Point3d.Origin);
                    e = new ThTee(tee_cp, tee_width, duct_width, duct_width);
                    e.RunTeeDrawEngine(Model, mat);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
