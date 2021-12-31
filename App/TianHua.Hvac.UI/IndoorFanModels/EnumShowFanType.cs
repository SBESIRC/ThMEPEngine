using System.ComponentModel;

namespace TianHua.Hvac.UI.IndoorFanModels
{
    public enum EnumShowFanType
    {
        [Description("风机盘管")]
        FanCoilUnit = 10,
        [Description("吊顶一体式空调箱")]
        IntegratedAirConditionin = 20,
        [Description("VRF室内机")]
        VRFConditionin = 30,
    }
}
