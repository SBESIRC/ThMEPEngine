using System;
using ThMEPEngineCore.Command;
using TianHua.Structure.WPF.UI.BeamStructure.MainBeamConnect;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Structure.WPF.UI.Command
{
    public class MainBeamCmd : ThMEPBaseCommand, IDisposable
    {
        MainBeamConnectUI _ui;

        public void Dispose()
        {

        }

        public MainBeamCmd()
        {
            this.ActionName = "天华主梁UI";
            this.CommandName = "THZLSCUI";
        }

        public override void SubExecute()
        {
            if(null != _ui && _ui.IsLoaded)
            {
                return;
            }
            _ui = new MainBeamConnectUI();
            var isOk = AcadApp.ShowModalWindow(_ui);
        }
    }
}
