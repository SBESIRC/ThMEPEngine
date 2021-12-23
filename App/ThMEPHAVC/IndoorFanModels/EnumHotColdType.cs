using System.ComponentModel;

namespace ThMEPHVAC.IndoorFanModels
{
    /// <summary>
    /// 风机风类型
    /// </summary>
    public enum EnumHotColdType
    {
        [Description("冷量")]
        Cold = 1,
        [Description("热量")]
        Hot = 2,
    }
}
