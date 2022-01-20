using System;
using ThMEPEngineCore.Command;
using TianHua.Structure.WPF.UI.BeamStructure.SecondaryBeamConnect;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Structure.WPF.UI.Command
{
    public class SecondaryBeamCmd : ThMEPBaseCommand, IDisposable
    {
        SecondaryBeamConnectUI _ui;
        public void Dispose()
        {
            //
        }

        public SecondaryBeamCmd()
        {
            this.ActionName = "天华次梁UI";
            this.CommandName = "THCLSCUI";
        }

        public override void SubExecute()
        {
            if (null != _ui && _ui.IsLoaded)
                return;
            _ui = new SecondaryBeamConnectUI();
            var isOk = AcadApp.ShowModalWindow(_ui);
        }
    }
}
