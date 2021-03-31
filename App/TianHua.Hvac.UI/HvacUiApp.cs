using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHAVC.CAD;
using ThMEPHVAC.CAD;
using ThMEPHVAC.IO;
using ThMEPHVAC.Model;
using TianHua.FanSelection.Function;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Hvac.UI
{
    public class HvacUiApp : IExtensionApplication
    {
        public void Initialize()
        {
            
        }

        public void Terminate()
        {
            
        }
        private ObjectIdCollection get_from_prompt(string prompt)
        {
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = prompt,
                RejectObjectsOnLockedLayers = true,
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
            var objIds = get_from_prompt("请选择风机和中心线");
            if (objIds.Count == 0)
                return new DBObjectCollection();
            var tmp = new DBObjectCollection();
            var center_lines = new DBObjectCollection();
            
            fan_id = classify_fan(objIds, tmp);
            ThLaneLineSimplifier.RemoveDangles(tmp, 100.0).ForEach(l => center_lines.Add(l));
            return center_lines;
        }

        private DBObjectCollection get_bypass()
        {
            var objIds = get_from_prompt("请选择旁通管");
            if (objIds.Count == 0)
                return new DBObjectCollection();
            Curve c = objIds[0].GetDBObject() as Curve;
            DBObjectCollection tmp = new DBObjectCollection();
            tmp.Add(c);
            List<Line> lines = ThLaneLineSimplifier.RemoveDangles(tmp, 100);
            if (lines.Count == 2)
            {
                foreach (Line l in lines)
                {
                    Point3d sp = l.StartPoint;
                    Point3d ep = l.EndPoint;
                    if (sp.X == ep.X)
                    {
                        if (sp.Y < ep.Y)
                        {
                            Point3d intP = new Point3d(sp.X, sp.Y + 3000, 0);
                            lines.Add(new Line(sp, intP + new Vector3d(0, -10, 0)));
                            lines.Add(new Line(intP + new Vector3d(0, 10, 0), ep));
                        }
                        else
                        {
                            Point3d intP = new Point3d(sp.X, ep.Y + 3000, 0);
                            lines.Add(new Line(ep, intP + new Vector3d(0, -10, 0)));
                            lines.Add(new Line(intP + new Vector3d(0, 10, 0), sp));
                        }
                        lines.Remove(l);
                        break;
                    }
                }
            }
            tmp.Clear();
            lines.ForEachDbObject(o => tmp.Add(o));

            return tmp;
        }

        private DBObjectCollection get_walls()
        {
            var wallobjects = new DBObjectCollection();
            var objIds = get_from_prompt("请选择内侧墙线");
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
            var wall_lines = new DBObjectCollection();
            ThLaneLineSimplifier.RemoveDangles(wallobjects, 100.0).ForEach(l => wall_lines.Add(l));
            return wall_lines;
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

        private Point3d get_valve_pos(DBObjectCollection bypass_lines, 
                                      double oft)
        {
            Line bl = bypass_lines[0] as Line;
            double min_y = (bl.EndPoint.Y < bl.StartPoint.Y) ? bl.EndPoint.Y : bl.StartPoint.Y;
            Point3d valve_pos = bl.StartPoint;
            if (oft > 0)
            {
                // 找到y值最低的点
                foreach (Line l in bypass_lines)
                {
                    if (l.EndPoint.X == l.StartPoint.X)
                    {
                        double y = l.EndPoint.Y < l.StartPoint.Y ? l.EndPoint.Y : l.StartPoint.Y;
                        if (y < min_y)
                        {
                            min_y = y;
                            valve_pos = new Point3d(l.EndPoint.X, y, 0);
                        }
                    }
                }
            }
            else
            {
                valve_pos = (bl.EndPoint.Y > bl.StartPoint.Y) ? bl.EndPoint : bl.StartPoint;
            }

            return valve_pos + new Vector3d(0, oft, 0);
        }

        private ThFanInletOutletAnalysisEngine IOAnalysis(ThDbModelFan Model)
        {
            ThFanInletOutletAnalysisEngine io_anay_res = new ThFanInletOutletAnalysisEngine(Model);
            io_anay_res.InletAnalysis();
            io_anay_res.OutletAnalysis();
            if (io_anay_res.InletAnalysisResult != AnalysisResultType.OK &&
                io_anay_res.OutletAnalysisResult != AnalysisResultType.OK)
            {
                return null;
            }
            return io_anay_res;
        }

        private void IODuctHoleAnalysis(ThDbModelFan Model,
                                        double angle,
                                        string innerDuctSize,
                                        string outerDuctSize,
                                        string TeeSize,
                                        bool is_type2,
                                        DBObjectCollection bypass_line,
                                        ThFanInletOutletAnalysisEngine io_anay_res)
        {
            if (bypass_line != null && bypass_line.Count == 0)
                return;

            ThInletOutletDuctDrawEngine io_draw_eng =
                new ThInletOutletDuctDrawEngine(Model,
                                                innerDuctSize,
                                                outerDuctSize,
                                                TeeSize,
                                                bypass_line, 
                                                io_anay_res.InletCenterLineGraph,
                                                io_anay_res.OutletCenterLineGraph);
            var wall_lines = get_walls();
            if (wall_lines.Count == 0)
                return;
            ThHolesAndValvesEngine holesAndValvesEngine =
                new ThHolesAndValvesEngine(Model,
                                           wall_lines,
                                           io_draw_eng.InletDuctWidth,
                                           io_draw_eng.OutletDuctWidth,
                                           io_anay_res.InletCenterLineGraph,
                                           io_anay_res.OutletCenterLineGraph);
            if (io_anay_res.InletAnalysisResult == AnalysisResultType.OK)
            {
                if (io_anay_res.HasInletTee())
                {
                    double IDuctWidth = io_draw_eng.InletDuctWidth;
                    double x = IDuctWidth / 2;
                    double y = x + 100;
                    io_draw_eng.RunInletDrawEngine(Model);
                    foreach (Point3d TeeCp in io_anay_res.InletTeeCPPositions)
                    {
                        ThTee e = new ThTee(TeeCp, io_draw_eng.TeeWidth, IDuctWidth, IDuctWidth);
                        Matrix3d mat = Matrix3d.Displacement(TeeCp.GetAsVector() + new Vector3d(0, -2 * y, 0)) *
                                       Matrix3d.Mirroring(new Line3d(new Point3d(x, y, 0), new Point3d(-x, y, 0)));
                        e.RunTeeDrawEngine(Model, mat);
                    }
                }
                else
                {
                    io_draw_eng.RunInletDrawEngine(Model);
                }
                holesAndValvesEngine.RunInletValvesInsertEngine();
            }

            if (io_anay_res.OutletAnalysisResult == AnalysisResultType.OK)
            {
                if (io_anay_res.HasOutletTee())
                {
                    double ODuctWidth = io_draw_eng.OutletDuctWidth;
                    io_draw_eng.RunOutletDrawEngine(Model);
                    foreach (Point3d TeeCp in io_anay_res.OutletTeeCPPositions)
                    {
                        ThTee e = new ThTee(TeeCp, io_draw_eng.TeeWidth, ODuctWidth, ODuctWidth);
                        Matrix3d mat = Matrix3d.Displacement(TeeCp.GetAsVector());
                        if (is_type2)
                        {
                            mat = mat * Matrix3d.Rotation(angle, Vector3d.ZAxis, new Point3d(0, 0, 0));
                        }
                        else 
                        {
                            mat = mat * Matrix3d.Mirroring(new Line3d(new Point3d(0, -1, 0), new Point3d(0, 1, 0))) *
                                        Matrix3d.Rotation(angle, Vector3d.ZAxis, new Point3d(0, 0, 0));
                        }
                        e.RunTeeDrawEngine(Model, mat);
                    }
                }
                else
                {
                    io_draw_eng.RunOutletDrawEngine(Model);
                }
                holesAndValvesEngine.RunOutletValvesInsertEngine();
            }
        }


        [CommandMethod("TIANHUACAD", "THFJF", CommandFlags.Modal)]
        public void Thfjf()
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
                using (var dlg = create_duct_diag(DbFanModel))
                {
                    if (AcadApp.ShowModalDialog(dlg) == DialogResult.OK)
                    {
                        innerDuctSize = dlg.SelectedInnerDuctSize;
                        outerDuctSize = dlg.SelectedOuterDuctSize;
                        airVloume = dlg.AirVolume;
                    }
                }
                if (string.IsNullOrEmpty(innerDuctSize) || string.IsNullOrEmpty(outerDuctSize))
                {
                    return;
                }

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
                        ThVTee vt = new ThVTee(600, 800, 20);
                        
                        vt.RunVTeeDrawEngine(DbFanModel, line_type);
                        Point3d valve_pos = DbFanModel.FanInletBasePoint + new Vector3d(45, -475, 0);
                        ThFanInletOutletAnalysisEngine io_anay_res = IOAnalysis(DbFanModel);
                        IODuctHoleAnalysis(DbFanModel, 0, innerDuctSize, outerDuctSize, tee_width, false, null, io_anay_res);
                        ThServiceTee.InsertElectricValve(valve_pos, 800, Math.PI);
                    }
                    else
                    {
                        // 添加旁通并添加到原Model中
                        bool is_type2 = tee_pattern == "RBType2";
                        DBObjectCollection bypass_line = get_bypass();
                        if (bypass_line.Count == 0)
                            return;
                        bypass_line.Cast<DBObject>().ForEachDbObject(o => lineobjects.Add(o));
                        
                        List<Line> bypass_lines = ThLaneLineSimplifier.RemoveDangles(lineobjects, 5);
                        double valve_oft = is_type2 ? -3000: 3000;
                        double angle = is_type2 ? -Math.PI / 2 : Math.PI / 2;
                        Point3d valve_pos = get_valve_pos(bypass_line, valve_oft);

                        lineobjects.Clear();
                        bypass_lines.ForEachDbObject(o => lineobjects.Add(o));

                        // 根据添加的旁通重新得到model
                        ThDbModelFan DbTeeModel = new ThDbModelFan(fan_id, lineobjects);
                        ThFanInletOutletAnalysisEngine io_anay_res = IOAnalysis(DbTeeModel);

                        double valve_width = Double.Parse(outerDuctSize.Split('x').First());
                        double bra_width = Double.Parse(tee_width.Split('x').First());
                        double IShrink = is_type2 ? bra_width + 50 : bra_width + 50;
                        double OShrinkb = is_type2 ? (bra_width + valve_width) * 0.5 + 50 : bra_width * 0.5 + 100;
                        double OShrinkm = is_type2 ? bra_width * 0.5 + 100 : (bra_width + valve_width) * 0.5 + 50;
                        if (io_anay_res.HasInletTee())
                        {
                            // Swap Ishrink & ObShrink
                            ThServiceTee.TeeFineTuneDuct(io_anay_res.InletCenterLineGraph,
                                                         OShrinkb,
                                                         OShrinkm,
                                                         IShrink);
                        }
                        if (io_anay_res.HasOutletTee())
                        {
                            ThServiceTee.TeeFineTuneDuct(io_anay_res.OutletCenterLineGraph,
                                                         IShrink,
                                                         OShrinkb,
                                                         OShrinkm);
                        }
                        IODuctHoleAnalysis(DbTeeModel, angle, innerDuctSize, outerDuctSize, tee_width, is_type2, bypass_line, io_anay_res);
                        ThServiceTee.InsertElectricValve(valve_pos, bra_width, Math.PI);
                    }
                }
                else
                {
                    ThFanInletOutletAnalysisEngine io_anay_res = IOAnalysis(DbFanModel);
                    IODuctHoleAnalysis(DbFanModel, 0, innerDuctSize, outerDuctSize, null, false, null, io_anay_res);
                }
            }
        }
    }
}
