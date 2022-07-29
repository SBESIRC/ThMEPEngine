using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.IO.JSON;
using ThMEPHVAC.Command;
using ThMEPHVAC.Service;
using ThMEPHVAC.SmokeProofSystem.ExportExcelService;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels.ThSmokeProofMappingModel;

namespace ThMEPHVAC
{
    class ThSmokeProofSystemCmd
    {
        //[CommandMethod("TIANHUACAD", "THssssss", CommandFlags.Modal)]
        //public void THSmokeProofSystem()
        //{
        //    using (var cmd = new SmokeProofSystemCmd())
        //    {
        //        cmd.Execute();
        //    }
        //}

        [CommandMethod("TIANHUACAD", "THSPEXPORT", CommandFlags.Modal)]
        public void ThSomkeProofExport()
        {
            ExportExcel exportExcel = new ExportExcel();
            exportExcel.Export();
        }

        [CommandMethod("TIANHUACAD", "THLXSPS", CommandFlags.Modal)]
        public void ThLXUcsCompass()
        {
            var sltTableType = ThMEPHVACStaticService.Instance.smokeCalculateViewModel.SelectTableItem;
            var smViewModel = MappingSmokeVM();
            string attriVal = GetWindVolumeAttri(sltTableType.Title, out string model);
            while (true)
            {
                using (AcadDatabase acadDatabase = AcadDatabase.Active())
                {
                    Active.Database.ImportCompassBlock(
                        ThMEPHAVCCommon.SMOKE_PROOF_BLOCK_NAME,
                        ThMEPHAVCCommon.SMOKE_PROOF_LAYER_NAME);
                    var attri = new Dictionary<string, string>() { { "系统编号", ThMEPHVACStaticService.Instance.smokeCalculateViewModel.SystemName } };
                    attri.Add("系统风量", attriVal);
                    attri.Add("房间功能", sltTableType.Title);
                    var objId = Active.Database.InsertCompassBlock(
                        ThMEPHAVCCommon.SMOKE_PROOF_BLOCK_NAME,
                        ThMEPHAVCCommon.SMOKE_PROOF_LAYER_NAME,
                        attri);
                    SetModelData(objId, smViewModel, model, attriVal);
                    var ucs2Wcs = Active.Editor.UCS2WCS();
                    var compass = acadDatabase.Element<BlockReference>(objId, true);
                    compass.TransformBy(ucs2Wcs);
                    var jig = new ThCompassDrawJig(Point3d.Origin.TransformBy(ucs2Wcs));
                    jig.AddEntity(compass);
                    PromptResult pr = Active.Editor.Drag(jig);
                    if (pr.Status != PromptStatus.OK)
                    {
                        compass.Erase();
                        break;
                    }
                    jig.TransformEntities();
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THMODIFYBLOCK", CommandFlags.Modal)]
        public void ThModifyBlockInfo()
        {
            var blockId = ThMEPHVACStaticService.Instance.BlockId;
            if (blockId != null)
            {
                var sltTableType = ThMEPHVACStaticService.Instance.smokeCalculateViewModel.SelectTableItem;
                var smViewModel = MappingSmokeVM();
                string attriVal = GetWindVolumeAttri(sltTableType.Title, out string model);
                using (AcadDatabase acadDatabase = AcadDatabase.Active())
                {
                    var attri = new Dictionary<string, string>() { { "系统编号", ThMEPHVACStaticService.Instance.smokeCalculateViewModel.SystemName } };
                    attri.Add("系统风量", attriVal);
                    attri.Add("房间功能", sltTableType.Title);
                    blockId.UpdateAttributesInBlock(attri);
                    SetModelData(blockId, smViewModel, model, attriVal);
                }
            }
        }

        /// <summary>
        /// 将主界面ui映射成普通ui，并序列化
        /// </summary>
        /// <returns></returns>
        private string MappingSmokeVM()
        {
            var model = ThMEPHVACStaticService.Instance.smokeCalculateViewModel;
            SmokeCalculateMappingModel calculateMappingModel = new SmokeCalculateMappingModel()
            {
                AirSupplyTitle = model.AirSupplySelectTableItem.Title,
                ScenarioTitle = model.SelectTableItem.Title,
            };
            return JsonHelper.SerializeObject(calculateMappingModel);
        }

        /// <summary>
        /// 获取风量值
        /// </summary>
        /// <param name="sceneType"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private string GetWindVolumeAttri(string sceneType, out string model)
        {
            string val = "";
            model = "";
            switch (sceneType)
            {
                case "消防电梯前室":
                    val = ThMEPHVACStaticService.Instance.fireElevatorFrontRoomViewModel.LjTotal.ToString();
                    model = JsonHelper.SerializeObject(ThMEPHVACStaticService.Instance.fireElevatorFrontRoomViewModel);
                    break;
                case "独立或合用前室（楼梯间自然）":
                    val = ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel.LjTotal.ToString();
                    model = JsonHelper.SerializeObject(ThMEPHVACStaticService.Instance.separateOrSharedNaturalViewModel);
                    break;
                case "独立或合用前室（楼梯间送风）":
                    val = ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel.LjTotal.ToString();
                    model = JsonHelper.SerializeObject(ThMEPHVACStaticService.Instance.separateOrSharedWindViewModel);
                    break;
                case "楼梯间（前室不送风）":
                    val = ThMEPHVACStaticService.Instance.staircaseNoWindViewModel.LjTotal.ToString();
                    model = JsonHelper.SerializeObject(ThMEPHVACStaticService.Instance.staircaseNoWindViewModel);
                    break;
                case "楼梯间（前室送风）":
                    val = ThMEPHVACStaticService.Instance.staircaseWindViewModel.LjTotal.ToString();
                    model = JsonHelper.SerializeObject(ThMEPHVACStaticService.Instance.staircaseWindViewModel);
                    break;
                case "封闭避难层（间）、避难走道":
                    val = ThMEPHVACStaticService.Instance.evacuationWalkViewModel.WindVolume.ToString();
                    model = JsonHelper.SerializeObject(ThMEPHVACStaticService.Instance.evacuationWalkViewModel);
                    break;
                case "避难走道前室":
                    val = ThMEPHVACStaticService.Instance.evacuationFrontViewModel.OpenDorrAirSupply.ToString();
                    model = JsonHelper.SerializeObject(ThMEPHVACStaticService.Instance.evacuationFrontViewModel);
                    break;
                default:
                    break;
            }
            return val;
        }

        /// <summary>
        /// 插入xdata信息
        /// </summary>
        /// <param name="objId"></param>
        /// <param name="smViewModel"></param>
        /// <param name="scenario"></param>
        private void SetModelData(ObjectId objId, string smViewModel, string scenario, string volume)
        {
            var flexData = FlexDataStoreExtensions.FlexDataStore(objId);
            flexData.SetValue(FlexDataKeyType.MianVm.ToString(), smViewModel);
            flexData.SetValue(FlexDataKeyType.UserControlVm.ToString(), scenario);
            flexData.SetValue(FlexDataKeyType.Volume.ToString(), volume);
        }
    }
}
