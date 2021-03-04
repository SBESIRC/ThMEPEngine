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
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHAVC.CAD;
using ThMEPHVAC;
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
                ThDbModelFan DbFanModel = new ThDbModelFan(modelobjectid, simplifierCenterLines);
                ThFanInletOutletAnalysisEngine inAndOutAnalysisEngine = new ThFanInletOutletAnalysisEngine(DbFanModel);
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

                ThInletOutletDuctDrawEngine inoutductdrawengine = new ThInletOutletDuctDrawEngine(DbFanModel, fmDuct.SelectedInnerDuctSize, fmDuct.SelectedOuterDuctSize, inAndOutAnalysisEngine.InletCenterLineGraph, inAndOutAnalysisEngine.OutletCenterLineGraph);

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
                ThHolesAndValvesEngine holesAndValvesEngine = new ThHolesAndValvesEngine(DbFanModel, simplifierWallLines, inoutductdrawengine.InletDuctWidth, inoutductdrawengine.OutletDuctWidth, inAndOutAnalysisEngine.InletCenterLineGraph, inAndOutAnalysisEngine.OutletCenterLineGraph);
                
                if (inAndOutAnalysisEngine.InletAnalysisResult == AnalysisResultType.OK)
                {
                    inoutductdrawengine.RunInletDrawEngine(DbFanModel);
                    holesAndValvesEngine.RunInletValvesInsertEngine();
                }
                if (inAndOutAnalysisEngine.OutletAnalysisResult == AnalysisResultType.OK)
                {
                    inoutductdrawengine.RunOutletDrawEngine(DbFanModel);
                    holesAndValvesEngine.RunOutletValvesInsertEngine();
                }

                Active.Editor.WriteMessage(inAndOutAnalysisEngine.InletAnalysisResult + "," + inAndOutAnalysisEngine.OutletAnalysisResult);
            }
        }

    }
}
