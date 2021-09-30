using System;
using ThMEPEngineCore.Command;
using ThMEPLighting.Lighting.ViewModels;
#if (ACAD2016 || ACAD2018)
using ThMEPEngineCore.ConnectWiring;
#endif

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
#if (ACAD2016 || ACAD2018)
            //todo: route cables using _UiConfigs
            ConnectWiringService connectWiringService = new ConnectWiringService();
            connectWiringService.Routing(25, "照明");
#else
            
#endif
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
            if (_UiConfigs.IsIlluminationLightChecked == true)
            {
                var cmd = new IlluminationLighting.IlluminationLightingCmd(_UiConfigs);
                cmd.Execute();
            }
        }
        public void Dispose()
        {
        }
    }
}

