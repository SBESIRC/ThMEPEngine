﻿using System;
using ThMEPEngineCore.Command;
using ThMEPLighting.Lighting.ViewModels;

namespace ThMEPLighting.Command
{
    public class ThLightingLayoutCommand : ThMEPBaseCommand, IDisposable
    {
        readonly LightingViewModel _UiConfigs = null;
        public ThLightingLayoutCommand(LightingViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
            CommandName = "THZM";
            ActionName = "布置";
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            if (_UiConfigs.LightingLayoutType == LightingLayoutTypeEnum.IlluminationLighting)
            {
                var cmd = new IlluminationLighting.IlluminationLightingCmd(_UiConfigs);
                cmd.Execute();
            }
            else if (_UiConfigs.LightingLayoutType == LightingLayoutTypeEnum.GarageLighting)
            {
                var cmd = new Garage.ThGarageLightingCmd(_UiConfigs);
                cmd.Execute();
            }
        }
    }
}

