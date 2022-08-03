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
        public ulong ClapboardId { get; set; }

        /// <summary>
        /// 起点接口Id
        /// </summary>
        public ThTCHTelecInterface StartInterfaceId { get; set; }

        /// <summary>
        /// 终点接口Id
        /// </summary>
        public ThTCHTelecInterface EndInterfaceId { get; set; }
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
        [Description("电力")]
        CableTray = 0,

        [Description("控制")]
        CableTray_Control = 1,

        [Description("消防")]
        CableTray_Fire = 2,

        [Description("其他")]
        CableTray_Other = 3,

        [Description("照明")]
        CableTray_Light = 4,

        [Description("安防")]
        CableTray_PDS = 5,

        [Description("广播")]
        CableTray_SECU = 6,

        [Description("有线电视")]
        CableTray_CATV = 7,

        [Description("综合布线")]
        CableTray_Radio = 8,
    }
}
