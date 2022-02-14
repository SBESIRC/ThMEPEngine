using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.ViewModel;

namespace ThMEPLighting.IlluminationLighting
{
    public class ThAFASIlluminateCmd
    {
        [CommandMethod("TIANHUACAD", "THFAIlluminationNoUI", CommandFlags.Modal)]
        public void THFAIlluminationNoUI()
        {
            var selectFloorRoom = ThAFASUtils.SettingInt("\n选楼层布置(0) 选房间布置(1)", 0);
            var floorUpDown = ThAFASUtils.SettingInt("\n住宅地下(0) 住宅地上(1)", 1);
            var beam = ThAFASUtils.SettingInt("\n不考虑梁（0）考虑梁（1）", 1);
            var wallThick = ThAFASUtils.SettingDouble("\n板厚", 100);
            var radiusN = ThAFASUtils.SettingDouble("\n正常照明灯具布置半径(mm)", 3000);
            var radiusE = ThAFASUtils.SettingDouble("\n应急照明灯具布置半径(mm)", 3000);
            var layoutEmg = ThAFASUtils.SettingInt("\n布置应急照明灯 否（0）是（1）", 1);

            FireAlarmSetting.Instance.Scale = 100;
            FireAlarmSetting.Instance.SelectFloorRoom = selectFloorRoom;
            FireAlarmSetting.Instance.FloorUpDown = floorUpDown;
            FireAlarmSetting.Instance.Beam = beam;
            FireAlarmSetting.Instance.RoofThickness = wallThick;
            FireAlarmSetting.Instance.BufferDist = 500;
            FireAlarmSetting.Instance.IlluRadiusNormal = radiusN;
            FireAlarmSetting.Instance.IlluRadiusEmg = radiusE;
            FireAlarmSetting.Instance.IlluLightType = 0;
            FireAlarmSetting.Instance.IlluIfLayoutEmg = layoutEmg == 1 ? true : false;
            FireAlarmSetting.Instance.IlluIfEmgAsNormal = false;

            FireAlarmSetting.Instance.LayoutItemList.Clear();
            FireAlarmSetting.Instance.LayoutItemList.Add((int)ThFaCommon.LayoutItemType.NormalLighting);
            FireAlarmSetting.Instance.LayoutItemList.Add((int)ThFaCommon.LayoutItemType.EmergencyLighting);


            ThAFASDataPass.Instance = new ThAFASDataPass();
            ThAFASUtils.AFASPrepareStep();
            if (ThAFASDataPass.Instance.SelectPts == null || ThAFASDataPass.Instance.SelectPts.Count == 0)
            {
                return;
            }

            using (var cmd = new IlluminationLightingCmd())
            {
                cmd.Execute();
            }

            ThAFASDataPass.Instance = null;
        }
    }
}
