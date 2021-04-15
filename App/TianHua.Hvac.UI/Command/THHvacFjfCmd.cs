﻿using AcHelper;
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
        private Tolerance Tor;
        private struct SizeParam
        {
            public string duct_size;
            public string tee_size;
            public string elevation;
            public string text_size;
            public string tee_pattern;
        }
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            Tor = new Tolerance(1.5, 1.5);
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ObjectId fan_id = ObjectId.Null;
                DBObjectCollection lineobjects = Get_fan_and_centerline(ref fan_id);
                if (fan_id.IsNull || lineobjects.Count == 0)
                    return;
                ThDbModelFan DbFanModel = new ThDbModelFan(fan_id, lineobjects);
                string innerDuctSize = string.Empty;
                string outerDuctSize = string.Empty;
                string airVloume = string.Empty;
                string elevation = string.Empty;
                string textSize = string.Empty;
                using (var dlg = Create_duct_diag(DbFanModel))
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
                        string line_type = (tee_pattern == "RBType4") ? ThHvacCommon.CONTINUES_LINETYPE : ThHvacCommon.DASH_LINETYPE;
                        double vt_width = 800;
                        ThVTee vt = new ThVTee(600, vt_width, 20);
                        double fan_angle = DbFanModel.FanOutlet.Angle;
                        Point3d valve_pos = DbFanModel.FanInletBasePoint;
                        ThFanInletOutletAnalysisEngine io_anay_res = Io_analysis(DbFanModel, null);
                        if (io_anay_res == null)
                            return;
                        int wall_num = 0;
                        SizeParam info = new SizeParam
                        {
                            duct_size = DuctSize,
                            elevation = elevation,
                            tee_size = tee_width,
                            text_size = textSize,
                            tee_pattern = tee_pattern
                        };
                        IODuctHoleAnalysis(DbFanModel, ref wall_num, info, null, null, io_anay_res);
                        if (wall_num != 0)
                        {
                            double angle = fan_angle * Math.PI / 180;
                            Vector3d fan_cp_vec = (DbFanModel.FanOutletBasePoint.GetAsVector() -
                                                   DbFanModel.FanInletBasePoint.GetAsVector()) * 0.5 +
                                                   DbFanModel.FanInletBasePoint.GetAsVector();
                            vt.RunVTeeDrawEngine(DbFanModel, line_type, angle, fan_cp_vec);
                            
                            ThServiceTee.Insert_electric_valve(fan_cp_vec, vt_width, angle + 1.5 * Math.PI);
                        }
                    }
                    else
                    {
                        // 添加旁通并添加到原Model中
                        DBObjectCollection bypass_lines = Get_bypass(tee_pattern, out Line max_bypass, out Line last_bypass);
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
                        ThFanInletOutletAnalysisEngine io_anay_res = Io_analysis(DbTeeModel, bypass_lines);
                        if (io_anay_res == null)
                            return;
                        double valve_width = Double.Parse(outerDuctSize.Split('x').First());
                        double bra_width = Double.Parse(tee_width.Split('x').First());
                        double s1 = bra_width + 50;
                        double s2 = (bra_width + valve_width) * 0.5 + 50;
                        double s3 = bra_width * 0.5 + 100;
                        if (tee_pattern == "RBType3")
                        {
                            if (io_anay_res.HasInletTee())
                                ThServiceTee.Fine_tee_duct(io_anay_res.InletCenterLineGraph, s3, s2, s1, bypass_lines);
                            if (io_anay_res.HasOutletTee())
                                ThServiceTee.Fine_tee_duct(io_anay_res.OutletCenterLineGraph, s1, s2, s3, bypass_lines);                            
                        }
                        if (tee_pattern == "RBType1")
                        {
                            if (io_anay_res.HasInletTee())
                                ThServiceTee.Fine_tee_duct(io_anay_res.InletCenterLineGraph, s3, s2, s1, bypass_lines);
                            if (io_anay_res.HasOutletTee())
                                ThServiceTee.Fine_tee_duct(io_anay_res.OutletCenterLineGraph, s1, s2, s3, bypass_lines);
                        }
                        if (tee_pattern == "RBType2")
                        {
                            if (io_anay_res.HasInletTee())
                                ThServiceTee.Fine_tee_duct(io_anay_res.InletCenterLineGraph, s3, s2, s1, bypass_lines);
                            if (io_anay_res.HasOutletTee())
                                ThServiceTee.Fine_tee_duct(io_anay_res.OutletCenterLineGraph, s1, s2, s3, bypass_lines);
                            max_bypass = last_bypass;
                        }
                        int wall_num = 0;
                        SizeParam info = new SizeParam
                        {
                            duct_size = DuctSize,
                            elevation = elevation,
                            tee_size = tee_width,
                            text_size = textSize,
                            tee_pattern = tee_pattern
                        };
                        IODuctHoleAnalysis(DbTeeModel, ref wall_num, info, max_bypass, bypass_lines, io_anay_res);
                        if (wall_num == 0)
                            return;
                        if (io_anay_res.HasInletTee() || io_anay_res.HasOutletTee())
                        {
                            Point3d bypass_start = max_bypass.StartPoint;
                            Point3d bypass_end = max_bypass.EndPoint;
                            Vector3d bypass_vec = (bypass_end.GetAsVector() - bypass_start.GetAsVector());
                            Vector3d dis_vec = bypass_start.GetAsVector() + bypass_vec * 0.5;
                            
                            double angle = 0;
                            if (tee_pattern == "RBType3")
                            {
                                if (io_anay_res.HasInletTee())
                                {
                                    angle = io_anay_res.InTeesInfo[0].angle.Angle + Math.PI * 0.5;
                                }
                                if (io_anay_res.HasOutletTee())
                                {
                                    angle = io_anay_res.OutTeesInfo[0].angle.Angle - Math.PI * 0.5;
                                }
                                Vector3d v = new Vector3d(io_anay_res.OutTeesInfo[0].angle.X,
                                                          io_anay_res.OutTeesInfo[0].angle.Y, 0);
                                angle += Math.PI;
                                if (Math.Abs(v.GetNormal().DotProduct(bypass_vec.GetNormal())) < 0.1)
                                    angle -= Math.PI;
                            }
                            else if (tee_pattern == "RBType1")
                            {
                                if (io_anay_res.HasInletTee())
                                {
                                    angle = io_anay_res.InTeesInfo[0].angle.Angle;
                                }
                                if (io_anay_res.HasOutletTee())
                                {
                                    angle = io_anay_res.OutTeesInfo[0].angle.Angle + Math.PI * 0.5;
                                }
                            }
                            else if (tee_pattern == "RBType2")
                            {
                                if (io_anay_res.HasInletTee())
                                {
                                    angle = io_anay_res.InTeesInfo[0].angle.Angle + Math.PI * 0.5;
                                }
                                if (io_anay_res.HasOutletTee())
                                {
                                    angle = io_anay_res.OutTeesInfo[0].angle.Angle;
                                }
                            }
                            ThServiceTee.Insert_electric_valve(dis_vec, bra_width, angle);
                            //elevation tee_width;
                        }
                    }
                }
                else
                {
                    ThFanInletOutletAnalysisEngine io_anay_res = Io_analysis(DbFanModel, null);
                    int wall_num = 0;
                    SizeParam info = new SizeParam
                    {
                        duct_size = DuctSize
                    };
                    IODuctHoleAnalysis(DbFanModel, ref wall_num, info, null, null, io_anay_res);
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

        private DBObjectCollection Get_bypass(string tee_pattern, out Line max_bypass, out Line last_bypass)
        {
            //l 是需要插入旁通文本的线
            var objIds = Get_from_prompt("请选择旁通管", true);
            if (objIds.Count == 0)
            {
                max_bypass = new Line();
                last_bypass = new Line();
                return new DBObjectCollection();
            }
            // 暂时只支持选择一个旁通
            Curve c = objIds[0].GetDBObject() as Curve;

            DBObjectCollection center_lines = new DBObjectCollection { c };
            List<Line> lines = ThLaneLineEngine.Explode(center_lines).Cast<Line>().ToList();
            Line l = lines[0];
            foreach (Line line in lines)
                if (line.Length > l.Length)
                    l = line;
            max_bypass = l;
            last_bypass = lines[lines.Count - 1];
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
            
            return lines.Select(o => o.ExtendLine(1.0)).ToCollection();
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

        private void IODuctHoleAnalysis(ThDbModelFan Model,
                                        ref int wall_num,
                                        SizeParam pst_param,
                                        Line selected_bypass,
                                        DBObjectCollection bypass_line,
                                        ThFanInletOutletAnalysisEngine io_anay_res)
        {
            string tee_size = pst_param.tee_size;
            string duct_size = pst_param.duct_size;
            string elevation = pst_param.elevation;
            string text_size = pst_param.text_size;
            string tee_pattern = pst_param.tee_pattern;

            if (bypass_line != null && bypass_line.Count == 0)
                return;
            string[] str = duct_size.Split(' ');
            string innerDuctSize = str[0];
            string outerDuctSize = str[1];

            ThInletOutletDuctDrawEngine io_draw_eng =
                new ThInletOutletDuctDrawEngine(Model, innerDuctSize, outerDuctSize, tee_size, elevation, text_size,
                                                selected_bypass, bypass_line,
                                                io_anay_res.InletCenterLineGraph,
                                                io_anay_res.OutletCenterLineGraph);
            var wall_lines = Get_walls();
            if (wall_lines.Count == 0)
                return;
            ThHolesAndValvesEngine holesAndValvesEngine =
                new ThHolesAndValvesEngine(Model, wall_lines, bypass_line, io_draw_eng, io_anay_res.InletCenterLineGraph, io_anay_res.OutletCenterLineGraph);
            if (io_anay_res.InletAnalysisResult == AnalysisResultType.OK)
            {
                if (io_anay_res.HasInletTee())
                {
                    double IDuctWidth = io_draw_eng.InletDuctWidth;
                    io_draw_eng.RunInletDrawEngine(Model, text_size);
                    foreach (TeeInfo tee_info in io_anay_res.InTeesInfo)
                    {
                        Vector3d dir = tee_info.dir;
                        Point3d tee_cp = tee_info.position;
                        Matrix3d mat = Matrix3d.Displacement(tee_cp.GetAsVector());
                        if (tee_pattern == "RBType3")
                        { 
                            if (bypass_line.Count == 3)
                                mat *= Matrix3d.Rotation(tee_info.angle.Angle, Vector3d.ZAxis, Point3d.Origin);
                            else if (bypass_line.Count == 5)
                                mat *= Matrix3d.Rotation(tee_info.angle.Angle + Math.PI, Vector3d.ZAxis, Point3d.Origin);
                        }
                        if (tee_pattern == "RBType1")
                        {
                            if (dir.Z < 0)
                                mat *= Matrix3d.Rotation(tee_info.angle.Angle, Vector3d.ZAxis, Point3d.Origin);
                            else
                                mat *= Matrix3d.Rotation(tee_info.angle.Angle + Math.PI, Vector3d.ZAxis, Point3d.Origin);
                        }
                        if (dir.Z < 0)
                            mat *= Matrix3d.Mirroring(new Line3d(Point3d.Origin, Vector3d.YAxis));
                        ThTee e = new ThTee(tee_cp, io_draw_eng.TeeWidth, IDuctWidth, IDuctWidth);
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
                    double ODuctWidth = io_draw_eng.OutletDuctWidth;
                    io_draw_eng.RunOutletDrawEngine(Model, text_size);
                    foreach (TeeInfo tee_info in io_anay_res.OutTeesInfo)
                    {
                        Vector3d dir = tee_info.dir;
                        Point3d tee_cp = tee_info.position;
                        Matrix3d mat = Matrix3d.Displacement(tee_cp.GetAsVector());
                        if (tee_pattern == "RBType3")
                        {
                            if (bypass_line.Count == 3)
                                mat *= Matrix3d.Rotation(tee_info.angle.Angle + Math.PI, Vector3d.ZAxis, Point3d.Origin);
                            else if (bypass_line.Count == 5)
                                mat *= Matrix3d.Rotation(tee_info.angle.Angle, Vector3d.ZAxis, Point3d.Origin);
                        }
                        if (tee_pattern == "RBType1")
                        {
                            if (dir.Z > 0)
                                mat *= Matrix3d.Rotation(tee_info.angle.Angle, Vector3d.ZAxis, Point3d.Origin);
                            else
                                mat *= Matrix3d.Rotation(tee_info.angle.Angle + Math.PI, Vector3d.ZAxis, Point3d.Origin);
                        }
                        if (tee_pattern == "RBType2")
                        {
                            if (dir.Z > 0)
                                mat *= Matrix3d.Rotation(tee_info.angle.Angle, Vector3d.ZAxis, Point3d.Origin);
                            else
                                mat *= Matrix3d.Rotation(tee_info.angle.Angle + Math.PI, Vector3d.ZAxis, Point3d.Origin);
                        }
                        if (dir.Z < 0)
                            mat *= Matrix3d.Mirroring(new Line3d(Point3d.Origin, Vector3d.YAxis));
                        ThTee e = new ThTee(tee_cp, io_draw_eng.TeeWidth, ODuctWidth, ODuctWidth);
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
