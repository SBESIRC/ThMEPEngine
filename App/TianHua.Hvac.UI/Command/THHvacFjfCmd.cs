using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHAVC.CAD;
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
                        ThFanInletOutletAnalysisEngine io_anay_res = IOAnalysis(DbFanModel);
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
                        Line bypass = new Line();
                        DBObjectCollection bypass_line = get_bypass(ref bypass);
                        if (bypass_line.Count == 0)
                            return;

                        bypass_line.Cast<DBObject>().ForEachDbObject(o => lineobjects.Add(o));
                        List<Line> tmp = ThLaneLineSimplifier.RemoveDangles(lineobjects, 5);
                        lineobjects.Clear();
                        tmp.ForEachDbObject(o => lineobjects.Add(o));

                        // 根据添加的旁通重新得到model
                        ThDbModelFan DbTeeModel = new ThDbModelFan(fan_id, lineobjects);
                        ThFanInletOutletAnalysisEngine io_anay_res = IOAnalysis(DbTeeModel);
                        if (io_anay_res == null)
                            return;
                        // 此支路一定存在旁通(不会访问越界)
                        Point3d p = io_anay_res.OutletTeeCPPositions[0];
                        Point3d detect_p = p.Equals(bypass.StartPoint) ? bypass.EndPoint : bypass.StartPoint;
                        double valve_width = Double.Parse(outerDuctSize.Split('x').First());
                        double bra_width = Double.Parse(tee_width.Split('x').First());
                        double s1 = bra_width + 50;
                        double s2 = (bra_width + valve_width) * 0.5 + 50;
                        double s3 = bra_width * 0.5 + 100;
                        if (io_anay_res.HasInletTee())
                        {
                            ThServiceTee.TeeFineTuneDuct(io_anay_res.InletCenterLineGraph, s3, s2, s1);
                        }
                        if (io_anay_res.HasOutletTee())
                        {
                            
                            //根据旁通角度分
                            if (tee_pattern == "RBType3")
                            {
                                if (detect_p.X < p.X)
                                {
                                    // 旁通向上
                                    ThServiceTee.TeeFineTuneDuct(io_anay_res.OutletCenterLineGraph, s1, s2, s3);
                                }
                                else
                                {
                                    // 旁通向下
                                    ThServiceTee.TeeFineTuneDuct(io_anay_res.OutletCenterLineGraph, s1, s3, s2);
                                }
                            }
                            else
                            {
                                if (detect_p.Y > p.Y)
                                    ThServiceTee.TeeFineTuneDuct(io_anay_res.OutletCenterLineGraph, s1, s3, s2);
                                else
                                    ThServiceTee.TeeFineTuneDuct(io_anay_res.OutletCenterLineGraph, s1, s2, s3);
                            }
                            
                        }
                        int wall_num = 0;
                        bool is_type2 = tee_pattern == "RBType2";
                        IODuctHoleAnalysis(DbTeeModel, DuctSize, tee_width, is_type2, ref wall_num, elevation, textSize, bypass_line, io_anay_res);
                        if (wall_num != 0)
                        {
                            Vector3d tmp_vec = (detect_p.GetAsVector() - p.GetAsVector()).GetNormal();
                            Vector2d r_vec = new Vector2d(tmp_vec.X, tmp_vec.Y);
                            Vector3d dis_vec = tmp_vec * 2000 + p.GetAsVector();
                            double ang = (2 * Math.PI - r_vec.Angle);
                            ThServiceTee.InsertElectricValve(dis_vec, bra_width, r_vec.Angle + Math.PI * 0.5, false);
                        }
                    }
                }
                else
                {
                    ThFanInletOutletAnalysisEngine io_anay_res = IOAnalysis(DbFanModel);
                    int wall_num = 0;
                    IODuctHoleAnalysis(DbFanModel, DuctSize, null, false, ref wall_num, null, null, null, io_anay_res);
                }
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
            var tmp = new DBObjectCollection();
            var center_lines = new DBObjectCollection();

            fan_id = classify_fan(objIds, tmp);
            ThLaneLineSimplifier.RemoveDangles(tmp, 100.0).ForEach(l => center_lines.Add(l));
            return center_lines;
        }

        private DBObjectCollection get_bypass(ref Line tee_line)
        {
            var objIds = get_from_prompt("请选择旁通管", true);
            if (objIds.Count == 0)
                return new DBObjectCollection();
            Curve c = objIds[0].GetDBObject() as Curve;
            DBObjectCollection tmp = new DBObjectCollection();
            tmp.Add(c);
            List<Line> lines = ThLaneLineSimplifier.RemoveDangles(tmp, 100);
            if (lines.Count == 2)
            {
                // 给较长的线段上插点
                Line l = lines[0].Length > lines[1].Length ? lines[0] : lines[1];
                tee_line = l;
                lines.Remove(l);
                Point3d lp = l.StartPoint.Y > l.EndPoint.Y ? l.EndPoint : l.StartPoint;
                Point3d up = l.StartPoint.Y > l.EndPoint.Y ? l.StartPoint : l.EndPoint;
                Vector2d v1 = new Vector2d(lp.X, lp.Y);
                Vector2d v2 = new Vector2d(up.X, up.Y);
                Vector2d v = (v2 - v1) * 0.5;
                double len = 10;
                double angle = v.Angle > 0.5 * Math.PI ? (v.Angle - 0.5 * Math.PI) : v.Angle;
                double s_val = Math.Sin(angle);
                double c_val = Math.Cos(angle);
                v += v1;
                Point3d p = new Point3d(v.X, v.Y, 0) + new Vector3d(-len * s_val, len * c_val, 0);
                lines.Add(new Line(up, new Point3d(p.X, p.Y, 0)));
                p = new Point3d(v.X, v.Y, 0) + new Vector3d(len * s_val, -len * c_val, 0);
                lines.Add(new Line(new Point3d(p.X, p.Y, 0), lp));
            }
            else if (lines.Count == 1)
            {
                tee_line = lines[0];
            }
            tmp.Clear();
            lines.ForEachDbObject(o => tmp.Add(o));

            return tmp;
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

        // is_type2也是是否要修改管道描述信息的标志
        private void IODuctHoleAnalysis(ThDbModelFan Model,
                                        string DuctSize,
                                        string tee_size,
                                        bool is_type2,
                                        ref int wall_num,
                                        string elevation,
                                        string textSize,
                                        DBObjectCollection bypass_line,
                                        ThFanInletOutletAnalysisEngine io_anay_res)
        {
            if (bypass_line != null && bypass_line.Count == 0)
                return;
            string[] str = DuctSize.Split(' ');
            string innerDuctSize = str[0];
            string outerDuctSize = str[1];
            ThInletOutletDuctDrawEngine io_draw_eng =
                new ThInletOutletDuctDrawEngine(Model,
                                                innerDuctSize,
                                                outerDuctSize,
                                                tee_size,
                                                elevation,
                                                textSize,
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
                    io_draw_eng.RunInletDrawEngine(Model, textSize);
                    foreach (Point3d TeeCp in io_anay_res.InletTeeCPPositions)
                    {
                        ThTee e = new ThTee(TeeCp, io_draw_eng.TeeWidth, IDuctWidth, IDuctWidth);

                        Matrix3d mat = Matrix3d.Displacement(TeeCp.GetAsVector()) * 
                                       Matrix3d.Rotation(io_anay_res.ICPAngle[i++]-0.5*Math.PI, Vector3d.ZAxis, Point3d.Origin) * 
                                       Matrix3d.Mirroring(new Line3d(Point3d.Origin, Vector3d.YAxis));
                        e.RunTeeDrawEngine(Model, mat);
                    }
                }
                else
                {
                    io_draw_eng.RunInletDrawEngine(Model, textSize);
                }
                holesAndValvesEngine.RunInletValvesInsertEngine();
            }

            if (io_anay_res.OutletAnalysisResult == AnalysisResultType.OK)
            {
                if (io_anay_res.HasOutletTee())
                {
                    int i = 0;
                    double ODuctWidth = io_draw_eng.OutletDuctWidth;
                    io_draw_eng.RunOutletDrawEngine(Model, textSize);
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
                            mat *= Matrix3d.Mirroring(new Line3d(new Point3d(0, -1, 0), Point3d.Origin)) *
                                   Matrix3d.Rotation(1.5 * Math.PI - angle, Vector3d.ZAxis, Point3d.Origin);
                        }
                        e.RunTeeDrawEngine(Model, mat);
                    }
                }
                else
                {
                    io_draw_eng.RunOutletDrawEngine(Model, textSize);
                }
                holesAndValvesEngine.RunOutletValvesInsertEngine();
                wall_num = wall_lines.Count;
            }
        }
    }
}
