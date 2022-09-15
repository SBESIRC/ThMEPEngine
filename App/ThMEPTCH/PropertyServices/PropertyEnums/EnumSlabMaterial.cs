using System.ComponentModel;

namespace ThMEPTCH.PropertyServices.PropertyEnums
{
    public enum EnumSlabMaterial
    {
        [Description("未选择")]
        UnSelect = -1,
        [Description("钢筋混凝土")]
        ReinforcedConcrete = 1,
    }

    public enum EnumTCHWallMaterial
    {
        [Description("未选择")]
        UnSelect = -1,
        [Description("加气混凝土")]
        Aeratedconcrete = 1,
    }
}
