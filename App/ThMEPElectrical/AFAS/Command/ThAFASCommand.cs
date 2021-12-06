using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper.Commands;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.ConnectWiring;

using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.AFAS;


namespace ThMEPElectrical.AFAS.Command
{
    public class ThAFASCommand : ThMEPBaseCommand, IDisposable
    {
        public ThAFASCommand()
        {
            CommandName = "THFireAlarmLayout";
            ActionName = "布置";
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

        public void Dispose()
        { }
    }
}
