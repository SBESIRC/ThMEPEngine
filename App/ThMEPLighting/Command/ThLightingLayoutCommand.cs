using System;
using System.Collections.Generic;
using System.Linq;

using Linq2Acad;

using ThMEPEngineCore.Command;
using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.Model;
using ThMEPLighting.ViewModel;

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
                IlluminateUIToSetting();
                FireAlarmSetting.Instance.LayoutItemList.Clear();
                FireAlarmSetting.Instance.LayoutItemList.Add((int)ThFaCommon.LayoutItemType.NormalLighting);
                FireAlarmSetting.Instance.LayoutItemList.Add((int)ThFaCommon.LayoutItemType.EmergencyLighting);

                using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (AcadDatabase acadDatabase = AcadDatabase.Active())
                {
                    ThAFASDataPass.Instance = new ThAFASDataPass();

                    ThAFASUtils.AFASPrepareStep();

                    if (ThAFASDataPass.Instance.SelectPts == null || ThAFASDataPass.Instance.SelectPts.Count == 0)
                    {
                        return;
                    }

                    var cmd = new IlluminationLighting.IlluminationLightingCmd();
                    cmd.Execute();

                    ThAFASDataPass.Instance = null;
                }
            }
            else if (_UiConfigs.LightingLayoutType == LightingLayoutTypeEnum.GarageLighting)
            {
                var cmd = new Garage.ThGarageLightingCmd(_UiConfigs);
                cmd.Execute();
            }
        }

        private void IlluminateUIToSetting()
        {
            FireAlarmSetting.Instance.Scale = _UiConfigs.ScaleSelectIndex == 0 ? 100 : 150;
            FireAlarmSetting.Instance.Beam = _UiConfigs.ShouldConsiderBeam == true ? 1 : 0;
            FireAlarmSetting.Instance.RoofThickness = _UiConfigs.RoofThickness;
            FireAlarmSetting.Instance.BufferDist = _UiConfigs.BufferDist;

            FireAlarmSetting.Instance.IlluRadiusNormal = _UiConfigs.RadiusNormal;
            FireAlarmSetting.Instance.IlluRadiusEmg = _UiConfigs.RadiusEmg;
            FireAlarmSetting.Instance.IlluLightType = (int)_UiConfigs.LightingType;
            FireAlarmSetting.Instance.IlluIfLayoutEmg = _UiConfigs.IfLayoutEmgChecked;
            FireAlarmSetting.Instance.IlluIfEmgAsNormal = _UiConfigs.IfEmgUsedForNormal;
        }
    }
}

