using System.ComponentModel;

namespace ThMEPLighting.Common
{
    public enum EnumIllumination
    {
        [Description("30")]
        Illumination_30 =30,
        [Description("50")]
        Illumination_50 = 50,
    }
    public enum EnumMaintenanceFactor 
    {
        [Description("0.8")]
        MaintenanceFactor_0_8 = 10,
        [Description("0.7")]
        MaintenanceFactor_0_7 = 11,
        [Description("0.65")]
        MaintenanceFactor_0_65 = 12,
        [Description("0.6")]
        MaintenanceFactor_0_6 = 13,
    }
}
