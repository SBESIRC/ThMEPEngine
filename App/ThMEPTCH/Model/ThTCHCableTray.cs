using System.ComponentModel;

namespace ThMEPTCH.Model
{
    public class ThTCHCableTray
    {
        /// <summary>
        /// 实体Id
        /// </summary>
        public ThTCHTelecObject ObjectId { get; set; }

        /// <summary>
        /// 桥架型号
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 桥架类型
        /// </summary>
        public CableTrayStyle Style { get; set; }

        /// <summary>
        /// 桥架系统关键字
        /// </summary>
        public CableTraySystem CableTraySystem { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 是否有盖板
        /// </summary>
        public bool Cover { get; set; }

        /// <summary>
        /// 隔板Id
        /// </summary>
        public ThTCHTelecClapboard Clapboard { get; set; }

        /// <summary>
        /// 起点接口Id
        /// </summary>
        public ThTCHTelecInterface StartInterface { get; set; }

        /// <summary>
        /// 终点接口Id
        /// </summary>
        public ThTCHTelecInterface EndInterface { get; set; }
    }

    public enum CableTrayStyle
    {
        [Description("槽式桥架")]
        Trough = 0,

        [Description("盘式桥架")]
        Disk = 1,

        [Description("梯式桥架")]
        Ladder = 2,
    }

    public enum CableTraySystem
    {
        [Description("低压非消防")]
        CABLETRAY = 0,

        [Description("弱电")]
        CABLETRAY_WEAK = 1,

        [Description("低压消防")]
        CABLETRAY_FIRE = 2,

        [Description("高压电力")]
        CABLETRAY_OTHER = 3,

        [Description("照明")]
        CABLETRAY_LIGH = 4,

        [Description("弱电消防")]
        CABLETRAY_SECU = 5,

        [Description("夜景照明")]
        CABLETRAY_RADIO = 6,

        [Description("UPS电源")]
        CABLETRAY_CATV = 7,

        [Description("综合布线")]
        CABLETRAY_PDS = 8,
    }
}
