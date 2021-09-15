using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.Lighting.ViewModels;
using ThMEPEngineCore.Command;
namespace ThMEPLighting.Lighting.Commands
{
    public class LightingRouteCableCommand : ThMEPBaseCommand, IDisposable
    {
        readonly LightingViewModel _UiConfigs = null;
        public LightingRouteCableCommand(LightingViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
            CommandName = "THLightingRouteCable";
            ActionName = "连线";
        }

        public override void SubExecute()
        {
            //todo: route cables using _UiConfigs
        }
        public void Dispose()
        { }
    }

    public class LightingLayoutCommand : ThMEPBaseCommand, IDisposable
    {
        readonly LightingViewModel _UiConfigs = null;
        public LightingLayoutCommand(LightingViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
            CommandName = "THLightingLayout";
            ActionName = "布置";
        }

        public override void SubExecute()
        {
            //todo: layout lighting components using _UiConfigs

        }
        public void Dispose()
        { }
    }
}

