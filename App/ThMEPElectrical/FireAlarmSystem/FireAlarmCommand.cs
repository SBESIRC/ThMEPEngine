using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper.Commands;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.ConnectWiring;

using ThMEPElectrical.FireAlarm.ViewModels;
using ThMEPElectrical.FireAlarm;


namespace ThMEPElectrical.FireAlarm.Commands
{
//    public class FireAlarmRouteCableCommand : ThMEPBaseCommand, IDisposable
//    {
//        readonly FireAlarmViewModel _UiConfigs = null;
//        public FireAlarmRouteCableCommand(FireAlarmViewModel uiConfigs)
//        {
//            _UiConfigs = uiConfigs;
//            CommandName = "THHZBJ";
//            ActionName = "连线";
//        }

//        public override void SubExecute()
//        {
//#if (ACAD2016 || ACAD2018)
//            //todo: route cables using _UiConfigs
//            //ConnectWiringService connectWiringService = new ConnectWiringService();
//            //connectWiringService.Routing(_UiConfigs.BusLoopPointMaxCount, "火灾报警");
//#else
//            //
//#endif

//        }

//        public void Dispose()
//        { }
//    }

    public class FireAlarmLayoutCommand : ThMEPBaseCommand, IDisposable
    {
        public FireAlarmLayoutCommand()
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
                    CommandHandlerBase.ExecuteFromCommandLine(false, "ThFABroadcast");
                    break;
                case 2:
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THFADisplay");
                    break;
                case 3:
                    CommandHandlerBase.ExecuteFromCommandLine(false, "ThFATel");
                    break;
                case 4:
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THFAGas");
                    break;
                case 5:
                   CommandHandlerBase.ExecuteFromCommandLine(false, "ThFAManualAlarm");
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
