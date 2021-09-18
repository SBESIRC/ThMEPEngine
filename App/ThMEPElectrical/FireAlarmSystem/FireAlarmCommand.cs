using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Command;
using ThMEPElectrical.FireAlarm.ViewModels;

using ThMEPElectrical;

namespace ThMEPElectrical.FireAlarm.Commands
{
    public class FireAlarmRouteCableCommand : ThMEPBaseCommand, IDisposable
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
            if (_UiConfigs.IsSmokeTempratureSensorChecked)
            {
                var smokdCmd = new ThMEPElectrical.FireAlarmSmokeHeat.ThFireAlarmSmokeHeatCmd(_UiConfigs);
                smokdCmd.Execute();
            }
            else if (_UiConfigs.IsFloorLoopChecked)
            {
                //楼层显示器
                var displayCmd = new ThMEPElectrical.FireAlarmFixLayout.Command.ThFireAlarmDisplayDeviceLayoutCmd(_UiConfigs);
                displayCmd.Execute();
            }
            else if (_UiConfigs.IsFireMonitorModuleChecked)
            {
                //防火门监控
                var monitorCmd = new ThMEPElectrical.FireAlarmFixLayout.Command.ThFireAlarmFireProofMonitorLayoutCmd (_UiConfigs);
                monitorCmd.Execute();
            }
            else if (_UiConfigs.IsFireProtectionPhoneChecked)
            {
                //消防电话
                var fireTelCmd = new ThMEPElectrical.FireAlarmFixLayout.Command.ThFireAlarmFireTelLayoutCmd (_UiConfigs);
                fireTelCmd.Execute();
            }
        }
        public void Dispose()
        { }
    }
}
