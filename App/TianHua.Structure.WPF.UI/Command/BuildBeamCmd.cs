using System;
using ThMEPEngineCore.Command;
using TianHua.Structure.WPF.UI.BeamStructure.BuildBeam;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Structure.WPF.UI.Command
{
    public class BuildBeamCmd : ThMEPBaseCommand, IDisposable
    {
        BuildBeamUI _ui;
        public void Dispose()
        {
            //
        }

        public BuildBeamCmd()
        {
            this.ActionName = "天华双线UI";
            this.CommandName = "THSXSCUI";
        }

        public override void SubExecute()
        {
            if (null != _ui && _ui.IsLoaded)
                return;
            _ui = new BuildBeamUI();
            var isOk = AcadApp.ShowModalWindow(_ui);
        }
    }
}
