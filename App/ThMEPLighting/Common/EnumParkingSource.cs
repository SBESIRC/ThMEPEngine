using System.ComponentModel;

namespace ThMEPLighting.Common
{
    public enum EnumParkingSource
    {
        [Description("仅图层")]
        OnlyLayerName = 1,
        [Description("仅块名称")]
        OnlyBlockName = 2,
        [Description("块名称和图层")]
        BlokcAndLayer = 3,
    }
}
