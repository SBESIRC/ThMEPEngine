using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.ApplicationServices;
using AcHelper;
using AcHelper.Commands;
using Linq2Acad;

using ThMEPEngineCore.Command;
using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.Data;

using ThMEPLighting.Lighting.ViewModels;

namespace ThMEPLighting.Command
{
    public class ThLightingLayoutCommand : ThMEPBaseCommand, IDisposable
    {
        readonly LightingViewModel _UiConfigs = null;
        public ThLightingLayoutCommand(LightingViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
            CommandName = "THZM";
            ActionName = "布置";
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            if (_UiConfigs.LightingLayoutType == LightingLayoutTypeEnum.IlluminationLighting)
            {
                using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (AcadDatabase acadDatabase = AcadDatabase.Active())
                {
                    ThAFASDataPass.Instance = new ThAFASDataPass();

                    var selectPts = ThAFASUtils.GetFrameBlk();
                    if (selectPts.Count == 0)
                    {
                        return;
                    }

                    var transformer = ThAFASUtils.GetTransformer(selectPts);

                    ////////导入所有块，图层信息
                    var extractBlkList = ThFaCommon.BlkNameList;
                    ThFireAlarmInsertBlk.PrepareInsert(extractBlkList, ThFaCommon.Blk_Layer.Select(x => x.Value).Distinct().ToList());

                    ////////清除所选的块
                    var cleanBlkList = new List<string>() { ThFaCommon.BlkName_CircleCeiling,
                                                        ThFaCommon.BlkName_DomeCeiling,
                                                        ThFaCommon.BlkName_InductionCeiling,
                                                        ThFaCommon.BlkName_Downlight,
                                                       };
                    if (_UiConfigs.IfLayoutEmgChecked)
                    {
                        cleanBlkList.Add(ThFaCommon.BlkName_EmergencyLight);
                    }
                    var previousEquipmentData = new ThAFASBusinessDataSetFactory()
                    {
                        BlkNameList = cleanBlkList,
                        //  InputExtractors = extractors,
                    };
                    previousEquipmentData.SetTransformer(transformer);
                    var localEquipmentData = previousEquipmentData.Create(acadDatabase.Database, selectPts);
                    var cleanEquipment = localEquipmentData.Container;
                    ThAFASUtils.CleanPreviousEquipment(cleanEquipment);

                    ///////////获取数据元素,已转回原位置附近////////
                    var extractors = ThAFASUtils.GetBasicArchitectureData(selectPts, transformer);
                    ThAFASDataPass.Instance.Extractors = extractors;
                    ThAFASDataPass.Instance.Transformer = transformer;
                    ThAFASDataPass.Instance.SelectPts = selectPts;

                    Document document = Active.Document;

                    var cmd = new IlluminationLighting.IlluminationLightingCmd(_UiConfigs);
                    cmd.Execute();

                    ThAFASDataPass.Instance = null;
                }
            }
            else if (_UiConfigs.LightingLayoutType == LightingLayoutTypeEnum.GarageLighting)
            {
                var cmd = new Garage.ThGarageLightingCmd(_UiConfigs);
                cmd.Execute();
            }
        }
    }
}

