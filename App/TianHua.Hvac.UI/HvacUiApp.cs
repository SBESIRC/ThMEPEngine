using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHAVC.CAD;
using ThMEPHVAC;
using ThMEPHVAC.CAD;
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

        [CommandMethod("TIANHUACAD", "THselDB", CommandFlags.Modal)]
        public void THSelDB()
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

                var lineobjects = new DBObjectCollection();
                ObjectId modelobjectid = ObjectId.Null;
                foreach (var oid in fanselectionresult.Value.GetObjectIds().ToList())
                {
                    var obj = oid.GetDBObject();
                    if (obj.IsModel())
                    {
                        modelobjectid = oid;
                    }
                    else
                    {
                        lineobjects.Add(obj);
                    }
                }
                ThDbModelFan DbFanModel = new ThDbModelFan(modelobjectid, lineobjects);

                ThFanInletOutletAnalysisEngine inAndOutAnalysisEngine = new ThFanInletOutletAnalysisEngine(DbFanModel);
                inAndOutAnalysisEngine.InletAnalysis();
                inAndOutAnalysisEngine.OutletAnalysis();
                if (inAndOutAnalysisEngine.InletAnalysisResult != AnalysisResultType.OK || inAndOutAnalysisEngine.OutletAnalysisResult != AnalysisResultType.OK)
                {
                    //
                }
                ThDuctSelectionEngine ductselectionengine = new ThDuctSelectionEngine(DbFanModel);

                var ductModel = new DuctSpecModel()
                {
                    AirSpeed = ThFanSelectionUtils.GetDefaultAirSpeed(DbFanModel.FanScenario),
                    AirVolume = DbFanModel.FanVolume,
                    ListOuterTube = ductselectionengine.GetDefaultDuctsSizeString(),
                    ListInnerTube = ductselectionengine.GetDefaultDuctsSizeString(),
                    OuterTube = ductselectionengine.RecommendOuterDuctSize,
                    InnerTube = ductselectionengine.RecommendInnerDuctSize
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
                    wallobjects.Add(obj);
                }
                ThHolesAndValvesEngine holesAndValvesEngine = new ThHolesAndValvesEngine(DbFanModel, wallobjects, inoutductdrawengine.InletDuctWidth, inoutductdrawengine.OutletDuctWidth, inAndOutAnalysisEngine.InletCenterLineGraph, inAndOutAnalysisEngine.OutletCenterLineGraph);

                if (inAndOutAnalysisEngine.InletAnalysisResult != AnalysisResultType.OK || inAndOutAnalysisEngine.OutletAnalysisResult != AnalysisResultType.OK)
                {
                    Active.Editor.WriteMessage(inAndOutAnalysisEngine.InletAnalysisResult + "," + inAndOutAnalysisEngine.OutletAnalysisResult);
                    var acuteAnglePositions = inAndOutAnalysisEngine.InletAcuteAnglePositions.Union(inAndOutAnalysisEngine.OutletAcuteAnglePositions);
                    foreach (var point in acuteAnglePositions)
                    {
                        acadDatabase.ModelSpace.Add(new Circle(point, Vector3d.ZAxis, 600));
                    }
                }

            }
        }

    }
}
