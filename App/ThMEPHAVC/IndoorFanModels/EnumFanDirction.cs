using System.ComponentModel;

namespace ThMEPHVAC.IndoorFanModels
{
    public enum EnumFanDirction
    {
        [Description("南")]
        South = 0,
        [Description("北")]
        North = 1,
        [Description("西")]
        West = 2,
        [Description("东")]
        East = 3,
        [Description("沿用已布置")]
        Routesare = 99,
    }
}
