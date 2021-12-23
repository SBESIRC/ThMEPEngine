using System.ComponentModel;

namespace ThMEPHVAC.IndoorFanModels
{
    /// <summary>
    /// 接回风口形式
    /// </summary>
    public enum EnumAirReturnType
    {
        [Description("回风管")]
        AirReturnPipe = 1,
        [Description("回风箱")]
        AirReturnBox = 2,
    }
}
