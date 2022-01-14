using System.ComponentModel;

namespace ThMEPHVAC.IndoorFanModels
{
    public enum EnumFanType
    {
        [Description("两管制风机盘管")]
        FanCoilUnitTwoControls = 10,
        [Description("四管制风机盘管")]
        FanCoilUnitFourControls = 11,
        [Description("吊顶一体式空调箱")]
        IntegratedAirConditionin = 20,
        [Description("VRF室内机（管道机）")]
        VRFConditioninConduit = 30,
        [Description("VRF室内机（四面出风型）")]
        VRFConditioninFourSides = 31,
    }
}
