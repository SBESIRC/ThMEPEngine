using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Command;
using TianHua.Electrical.ViewModels;
namespace TianHua.Electrical.Commands
{
    public class FireAlarmRouteCableCommand : ThMEPBaseCommand,IDisposable
    {
        readonly FireAlarmViewModel _UiConfigs = null;
        public FireAlarmRouteCableCommand(FireAlarmViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
            CommandName = "THFireAlarmRouteCable";
            ActionName = "连线";
        }

        public override void SubExecute()
        {
            //todo: route cables using _UiConfigs
        }

        public void Dispose()
        { }
    }

    public class FireAlarmLayoutCommand : ThMEPBaseCommand, IDisposable
    {
        readonly FireAlarmViewModel _UiConfigs = null;
        public FireAlarmLayoutCommand(FireAlarmViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
            CommandName = "THFireAlarmLayout";
            ActionName = "布置";
        }

        public override void SubExecute()
        {
            //todo: layout fire alarm components using _UiConfigs

        }
        public void Dispose()
        { }
    }
}
