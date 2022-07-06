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
                                ////sendCommand是异步调用。cmd cammand flag需要是session包一层命令强制同步调用。
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
          
        }
    }
}
