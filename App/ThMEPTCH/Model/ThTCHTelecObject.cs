using System.ComponentModel;

namespace ThMEPTCH.Model
{
    public class ThTCHTelecObject
    {
        /// <summary>
        /// 实体类型
        /// </summary>
        public TelecObjectType Type { get; set; }
    }

    public enum TelecObjectType
    {
        [Description("桥架")]
        CableTray = 0,

        [Description("弯通")]
        Elbow = 1,

        [Description("三通")]
        Tee = 2,

        [Description("四通")]
        Cross = 3,

        [Description("变径")]
        Reduce = 4,

        [Description("乙字弯")]
        Offset = 5,

        [Description("桥架标注")]
        CabDim = 6,
    }
}
