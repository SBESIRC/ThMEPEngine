using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.ApplicationServices;
using AcHelper;
using AcHelper.Commands;
using Linq2Acad;
using ThMEPEngineCore.Command;
using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.Data;

namespace ThMEPElectrical.AFAS.Command
{
    public class ThAFASCommand : ThMEPBaseCommand, IDisposable
    {
        public ThAFASCommand()
        {
            CommandName = "THHZBJ";
            ActionName = "布置";
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThAFASDataPass.Instance = new ThAFASDataPass();

                //var selectPts = ThAFASSelectFrameUtil.GetFrameBlk();
                //if (selectPts.Count == 0)
                //{
                //    return;
                //}

                //var transformer = ThAFASUtils.GetTransformer(selectPts);

                //////////导入所有块，图层信息
                //var extractBlkList = ThFaCommon.BlkNameList;
                //ThFireAlarmInsertBlk.PrepareInsert(extractBlkList, ThFaCommon.Blk_Layer.Select(x => x.Value).Distinct().ToList());

                //////////清除所选的块
                //var cleanBlkList = FireAlarmSetting.Instance.LayoutItemList.SelectMany(x => ThFaCommon.LayoutBlkList[x]).ToList();
                //var previousEquipmentData = new ThAFASBusinessDataSetFactory()
                //{
                //    BlkNameList = cleanBlkList,
                //    //  InputExtractors = extractors,
                //};
                //previousEquipmentData.SetTransformer(transformer);
                //var localEquipmentData = previousEquipmentData.Create(acadDatabase.Database, selectPts);
                //var cleanEquipment = localEquipmentData.Container;
                //ThAFASUtils.CleanPreviousEquipment(cleanEquipment);

                /////////////获取数据元素,已转回原位置附近////////
                //var extractors = ThAFASUtils.GetBasicArchitectureData(selectPts, transformer);
                //ThAFASDataPass.Instance.Extractors = extractors;
                //ThAFASDataPass.Instance.Transformer = transformer;
                //ThAFASDataPass.Instance.SelectPts = selectPts;

                ThAFASUtils.AFASPrepareStep();
                if (ThAFASDataPass.Instance.SelectPts == null || ThAFASDataPass.Instance.SelectPts.Count == 0)
                {
                    return;
                }

                Document document = Active.Document;

                for (int i = 0; i < FireAlarmSetting.Instance.LayoutItemList.Count; i++)
                {
                    var layout = FireAlarmSetting.Instance.LayoutItemList[i];
                    switch (layout)
                    {
                        case (int)ThFaCommon.LayoutItemType.Smoke:
                            {
                                ////加\n自动回车 否则用户要敲回车键。
                                ////CommandHandlerBase.ExecuteFromCommandLine(false, "THFASmoke") 是异步，这里不适用
                                ////Active.Editor.Command(new Object[] { "THFASmoke" });没有ui模式可以，有ui模式会报错
                                ////sendCommand同步调用。cmd cammand flag需要是session。
                                document.SendCommand("THFASmoke" + "\n");
                                break;
                            }
                        case (int)ThFaCommon.LayoutItemType.Broadcast:
                            {
                                document.SendCommand("THFABroadcast" + "\n");
                                break;
                            }
                        case (int)ThFaCommon.LayoutItemType.Display:
                            {
                                document.SendCommand("THFADisplay" + "\n");
                                break;
                            }
                        case (int)ThFaCommon.LayoutItemType.Tel:
                            {
                                document.SendCommand("THFATel" + "\n");
                                break;
                            }
                        case (int)ThFaCommon.LayoutItemType.Gas:
                            {
                                document.SendCommand("THFAGas" + "\n");
                                break;
                            }
                        case (int)ThFaCommon.LayoutItemType.ManualAlarm:
                            {
                                document.SendCommand("THFAManualAlarm" + "\n");
                                break;
                            }
                        case (int)ThFaCommon.LayoutItemType.Monitor:
                            {
                                document.SendCommand("THFAMonitor" + "\n");
                                break;
                            }
                        default:
                            break;
                    }
                }
            }

            ThAFASDataPass.Instance = null;
            //FireAlarmSetting.Instance.LayoutItemList.Clear();

        }
    }
}
