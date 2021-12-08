using System;
using AcHelper.Commands;
using ThMEPEngineCore.Command;
using ThMEPElectrical.AFAS.ViewModel;

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
            switch (FireAlarmSetting.Instance.LayoutItem)
            {
                case 0:
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THFASmoke");
                    break;
                case 1:
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THFABroadcast");
                    break;
                case 2:
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THFADisplay");
                    break;
                case 3:
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THFATel");
                    break;
                case 4:
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THFAGas");
                    break;
                case 5:
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THFAManualAlarm");
                    break;
                case 6:
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THFAMonitor");
                    break;
                default:
                    break;
            }
        }
    }
}
