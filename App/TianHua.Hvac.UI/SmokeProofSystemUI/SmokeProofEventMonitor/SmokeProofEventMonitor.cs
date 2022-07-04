using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.IO.JSON;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels.ThSmokeProofMappingModel;
using TianHua.Hvac.UI.SmokeProofSystemUI.ViewModelConverters;

namespace TianHua.Hvac.UI.SmokeProofSystemUI.SmokeProofEventMonitor
{
    class SmokeProofEventMonitor
    {
        private static string _customCmd = null;
        private static bool _runCustomCommand = false;
        private static ObjectId _selectedEntId = ObjectId.Null;
        public static void Application_BeginDoubleClick(object sender, BeginDoubleClickEventArgs e)
        {
            _customCmd = null;
            _selectedEntId = ObjectId.Null;

            //Get entity which user double-clicked on
            PromptSelectionResult res = Active.Editor.SelectAtPickBox(e.Location);
            if (res.Status == PromptStatus.OK)
            {
                ObjectId[] ids = res.Value.GetObjectIds();

                //Only when there is one entity selected, we go ahead to see
                //if there is a custom command supposed to target at this entity
                if (ids.Length == 1)
                {
                    _selectedEntId = ids[0];
                    using (AcadDatabase acadDatabase = AcadDatabase.Use(_selectedEntId.Database))
                    {
                        GetXRecordData(_selectedEntId);
                        _customCmd = "THSMKPS";
                    }

                    //Find mapped custom command name
                    if (System.Convert.ToInt32(Application.GetSystemVariable("DBLCLKEDIT")) == 0)
                    {
                        //Since "Double click editing" is not enabled, we'll
                        //go ahead to launch our custom command
                        LaunchCustomCommand(Active.Editor);
                    }
                    else
                    {
                        //Since "Double Click Editing" is enabled, a command
                        //defined in CUI/CUIX will be fired. Let the code return
                        //and wait the DocumentLockModeChanged and
                        //DocumentLockModeChangeVetoed event handlers do their job
                        return;
                    }
                }
            }
        }

        private static void LaunchCustomCommand(Editor ed)
        {
            //Create implied a selection set
            ed.SetImpliedSelection(new ObjectId[] { _selectedEntId });

            string cmd = _customCmd;

            _customCmd = null;
            _selectedEntId = ObjectId.Null;

            //Start the custom command which has UsePickSet flag set
            Active.Document.SendStringToExecute(string.Format("{0} ", cmd), true, false, false);
        }

        private static void GetXRecordData(ObjectId obj)
        {
            var flexData = FlexDataStoreExtensions.FlexDataStore(obj);
            var mainVal = flexData.GetValue(FlexDataKeyType.MianVm.ToString());
            var scenarioVal = flexData.GetValue(FlexDataKeyType.UserControlVm.ToString());

            var model = JsonHelper.DeserializeJsonToObject<SmokeCalculateMappingModel>(mainVal);
            ThMEPHVACStaticService.Instance.smokeCalculateMappingModel = model;

            switch (ThMEPHVACStaticService.Instance.smokeCalculateMappingModel.ScenarioTitle)
            {
                case "消防电梯前室":
                    var fireElevatorFrontRoomViewModel = JsonHelper.DeserializeJsonToObject<FireElevatorFrontRoomViewModel>(scenarioVal);
                    SmkViewModelConverter.ConvertFireElevatorFrontRoomViewModel(fireElevatorFrontRoomViewModel);
                    break;
                case "独立或合用前室（楼梯间自然）":
                    var separateOrSharedNaturalViewModel = JsonHelper.DeserializeJsonToObject<SeparateOrSharedNaturalViewModel>(scenarioVal);
                    SmkViewModelConverter.ConvertSeparateOrSharedNaturalViewModel(separateOrSharedNaturalViewModel);
                    break;
                case "独立或合用前室（楼梯间送风）":
                    var separateOrSharedWindViewModel = JsonHelper.DeserializeJsonToObject<SeparateOrSharedWindViewModel>(scenarioVal);
                    SmkViewModelConverter.ConvertSeparateOrSharedWindViewModel(separateOrSharedWindViewModel);
                    break;
                case "楼梯间（前室不送风）":
                    var staircaseNoWindViewModel = JsonHelper.DeserializeJsonToObject<StaircaseNoWindViewModel>(scenarioVal);
                    SmkViewModelConverter.ConvertStaircaseNoWindViewModel(staircaseNoWindViewModel);
                    break;
                case "楼梯间（前室送风）":
                    var staircaseWindViewModel = JsonHelper.DeserializeJsonToObject<StaircaseWindViewModel>(scenarioVal);
                    SmkViewModelConverter.ConvertStaircaseWindViewModel(staircaseWindViewModel);
                    break;
                case "封闭避难层（间）、避难走道":
                    var evacuationWalkViewModel = JsonHelper.DeserializeJsonToObject<EvacuationWalkViewModel>(scenarioVal);
                    SmkViewModelConverter.ConvertEvacuationWalkViewModel(evacuationWalkViewModel);
                    break;
                case "避难走道前室":
                    var evacuationFrontViewModel = JsonHelper.DeserializeJsonToObject<EvacuationFrontViewModel>(scenarioVal);
                    SmkViewModelConverter.ConvertEvacuationFrontViewModel(evacuationFrontViewModel);
                    break;
                default:
                    break;
            }
        }

        public static void DocumentManager_DocumentLockModeChanged(object sender, DocumentLockModeChangedEventArgs e)
        {
            _runCustomCommand = false;
            if (!e.GlobalCommandName.StartsWith("#"))
            {
                // Lock状态，可以看做命令开始状态
                var cmdName = e.GlobalCommandName;

                // 过滤"EATTEDIT"命令
                if (!cmdName.ToUpper().Equals("EATTEDIT"))
                {
                    return;
                }

                if (!_selectedEntId.IsNull &&
                    !string.IsNullOrEmpty(_customCmd) &&
                    !cmdName.ToUpper().Equals(_customCmd.ToUpper()))
                {
                    e.Veto();
                    _runCustomCommand = true;
                }
            }
        }

        public static void DocumentManager_DocumentLockModeChangeVetoed(object sender, DocumentLockModeChangeVetoedEventArgs e)
        {
            if (_runCustomCommand)
            {
                //Start custom command
                LaunchCustomCommand(Active.Editor);
            }
        }
    }
}
