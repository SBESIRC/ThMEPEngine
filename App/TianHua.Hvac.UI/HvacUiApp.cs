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

        [CommandMethod("TIANHUACAD", "THFJF", CommandFlags.Modal)]
        public void Thfjf()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                double SepDis = 3000;
                var fanopt = new PromptSelectionOptions()
                {
                    MessageForAdding = "请选择风机和中心线"
                };
                var fanselectionresult = Active.Editor.GetSelection(fanopt);
                if (fanselectionresult.Status != PromptStatus.OK)
                {
                    return;
                }

                ObjectId modelobjectid = ObjectId.Null;
                var lineobjects = new DBObjectCollection();
                foreach (var oid in fanselectionresult.Value.GetObjectIds())
                {
                    var obj = oid.GetDBObject();
                    if (obj.IsRawModel())
                    {
                        modelobjectid = oid;
                    }
                    else if (obj is Curve curve)
                    {
                        lineobjects.Add(curve.Clone() as Curve);
                    }
                }
                if (modelobjectid.IsNull)
                {
                    return;
                }
                
                var simplifierCenterLines = new DBObjectCollection();
                ThLaneLineSimplifier.RemoveDangles(lineobjects,100.0).ForEach(l=> simplifierCenterLines.Add(l));
                ThServiceTee.SeperateIODuct(simplifierCenterLines, SepDis);// 将连接IO的线变为两条线
                ThDbModelFan DbFanModel = new ThDbModelFan(modelobjectid, simplifierCenterLines);
                ThFanInletOutletAnalysisEngine inAndOutAnalysisEngine = 
                    new ThFanInletOutletAnalysisEngine(DbFanModel);
                inAndOutAnalysisEngine.InletAnalysis();
                inAndOutAnalysisEngine.OutletAnalysis();
                if (inAndOutAnalysisEngine.InletAnalysisResult != AnalysisResultType.OK && inAndOutAnalysisEngine.OutletAnalysisResult != AnalysisResultType.OK)
                {
                    return;
                }
                var calculatevolume = DbFanModel.LowFanVolume == 0 ? DbFanModel.FanVolume : DbFanModel.LowFanVolume;
                ThDuctSelectionEngine ductselectionengine = new ThDuctSelectionEngine(calculatevolume, ThFanSelectionUtils.GetDefaultAirSpeed(DbFanModel.FanScenario));

                //进出口段与机房内外段的对应关系
                var jsonReader = new ThDuctInOutMappingJsonReader();
                var innerRomDuctPosition = jsonReader.Mappings.First(d => d.WorkingScenario == DbFanModel.FanScenario).InnerRoomDuctType;

                var ductModel = new DuctSpecModel()
                {
                    AirSpeed = ThFanSelectionUtils.GetDefaultAirSpeed(DbFanModel.FanScenario),
                    MaxAirSpeed = ThFanSelectionUtils.GetMaxAirSpeed(DbFanModel.FanScenario),
                    MinAirSpeed = ThFanSelectionUtils.GetMinAirSpeed(DbFanModel.FanScenario),
                    AirVolume = calculatevolume,
 
                    ListOuterTube = new List<string>(ductselectionengine.DuctSizeInfor.DefaultDuctsSizeString),
                    ListInnerTube = new List<string>(ductselectionengine.DuctSizeInfor.DefaultDuctsSizeString),
                    OuterTube = ductselectionengine.DuctSizeInfor.RecommendOuterDuctSize,
                    InnerTube = ductselectionengine.DuctSizeInfor.RecommendInnerDuctSize,
 
                    InnerAnalysisType = innerRomDuctPosition == "进风段"? inAndOutAnalysisEngine.InletAnalysisResult : inAndOutAnalysisEngine.OutletAnalysisResult,
                    OuterAnalysisType = innerRomDuctPosition == "进风段" ? inAndOutAnalysisEngine.OutletAnalysisResult : inAndOutAnalysisEngine.InletAnalysisResult,
 
                };

                fmDuctSpec fmDuct = new fmDuctSpec();
                fmDuct.InitForm(ductModel);
                if (fmDuct.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                double valve_width = 1250;
                double w1 = valve_width / 2 + 100;
                double w2 = valve_width + 50;
                double IShrink = (inAndOutAnalysisEngine.IsTeeDirUp) ? w1 : w2;
                double OShrink1 = w2;
                double OShrink2 = (inAndOutAnalysisEngine.IsTeeDirUp) ? w2 : w1;
                if (inAndOutAnalysisEngine.HasInletTee())
                {
                    ThServiceTee.TeeRefineDuct(
                                        inAndOutAnalysisEngine.InletCenterLineGraph,
                                        IShrink,
                                        OShrink1,
                                        OShrink2);
                }
                if (inAndOutAnalysisEngine.HasOutletTee())
                {
                    ThServiceTee.TeeRefineDuct(
                                        inAndOutAnalysisEngine.OutletCenterLineGraph,
                                        IShrink,
                                        OShrink1,
                                        OShrink2);
                }
                ThInletOutletDuctDrawEngine inoutductdrawengine = 
                    new ThInletOutletDuctDrawEngine(DbFanModel, 
                                                    fmDuct.SelectedInnerDuctSize, 
                                                    fmDuct.SelectedOuterDuctSize, 
                                                    inAndOutAnalysisEngine.InletCenterLineGraph, 
                                                    inAndOutAnalysisEngine.OutletCenterLineGraph);

                var wallopt = new PromptSelectionOptions()
                {
                    MessageForAdding = "请选择内侧墙线"
                };
                var wallselectionresult = Active.Editor.GetSelection(wallopt);
                if (wallselectionresult.Status != PromptStatus.OK)
                {
                    return;
                }
                var wallobjects = new DBObjectCollection();
                foreach (var oid in wallselectionresult.Value.GetObjectIds().ToList())
                {
                    var obj = oid.GetDBObject();
                    if (obj is Curve curveobj)
                    {
                        wallobjects.Add(curveobj);
                    }
                }
                var simplifierWallLines = new DBObjectCollection();
                ThLaneLineSimplifier.RemoveDangles(wallobjects, 100.0).ForEach(l=> simplifierWallLines.Add(l));
                ThHolesAndValvesEngine holesAndValvesEngine = 
                    new ThHolesAndValvesEngine(DbFanModel, 
                                               simplifierWallLines, 
                                               inoutductdrawengine.InletDuctWidth, 
                                               inoutductdrawengine.OutletDuctWidth, 
                                               inAndOutAnalysisEngine.InletCenterLineGraph, 
                                               inAndOutAnalysisEngine.OutletCenterLineGraph);
                
                if (inAndOutAnalysisEngine.InletAnalysisResult == AnalysisResultType.OK)
                {
                    if (inAndOutAnalysisEngine.HasInletTee())
                    {   
                        double IDuctWidth = inoutductdrawengine.InletDuctWidth;
                        double x = IDuctWidth / 2;
                        double y = x + 100;
                        ThTee e = new ThTee(inAndOutAnalysisEngine.InletTeeCP,
                                            IDuctWidth,
                                            IDuctWidth,
                                            IDuctWidth);
                        inoutductdrawengine.RunInletDrawEngine(DbFanModel);
                        Matrix3d mat = Matrix3d.Displacement(inAndOutAnalysisEngine.InletTeeCP.GetAsVector() + new Vector3d(0, -2 * y, 0)) *
                                       Matrix3d.Mirroring(new Line3d(new Point3d(x, y, 0), new Point3d(-x, y, 0)));
                        e.RunTeeDrawEngine(DbFanModel, mat);
                    }
                    else
                    {
                        inoutductdrawengine.RunInletDrawEngine(DbFanModel);
                    }
                    holesAndValvesEngine.RunInletValvesInsertEngine();
                }
                if (inAndOutAnalysisEngine.OutletAnalysisResult == AnalysisResultType.OK)
                {
                    if (inAndOutAnalysisEngine.HasOutletTee())
                    {
                        double angle = (inAndOutAnalysisEngine.IsTeeDirUp) ? Math.PI / 2 : -Math.PI / 2;
                        double valve_oft = (inAndOutAnalysisEngine.IsTeeDirUp) ? SepDis : -SepDis;
                        double ODuctWidth = inoutductdrawengine.OutletDuctWidth;
                        ThTee e = new ThTee(inAndOutAnalysisEngine.OutletTeeCP,
                                            ODuctWidth,
                                            ODuctWidth,
                                            ODuctWidth);
                        inoutductdrawengine.RunOutletDrawEngine(DbFanModel);
                        Matrix3d mat = 
                            Matrix3d.Displacement(inAndOutAnalysisEngine.OutletTeeCP.GetAsVector()) *
                            Matrix3d.Rotation(angle, Vector3d.ZAxis, new Point3d(0, 0, 0));
                        e.RunTeeDrawEngine(DbFanModel, mat);
                        ThServiceTee.InsertElectricValve(
                            inAndOutAnalysisEngine.OutletTeeCP + new Vector3d(0, valve_oft, 0),
                            valve_width,
                            Math.PI);
                    }
                    else 
                    {
                        inoutductdrawengine.RunOutletDrawEngine(DbFanModel);
                    }
                    holesAndValvesEngine.RunOutletValvesInsertEngine();
                }

                // 创建一个VTeeEngine
                // 用VTeeEngine.RunVTeeDrawEngine(DbFanModel)
                //ThVTee a = new ThVTee(600, 800, 20);
                //a.RunVTeeDrawEngine(DbFanModel, ThHvacCommon.CONTINUES_LINETYPE);
                //a.RunVTeeDrawEngine(DbFanModel, ThHvacCommon.VTEE_LINETYPE);
                //ThServiceTee.InsertElectricValve(
                //                      DbFanModel.FanInletBasePoint + new Vector3d(45, -475, 0),
                //                      800, 
                //                      Math.PI);
                Active.Editor.WriteMessage(inAndOutAnalysisEngine.InletAnalysisResult + "," + inAndOutAnalysisEngine.OutletAnalysisResult);
            }
        }
    }
}
