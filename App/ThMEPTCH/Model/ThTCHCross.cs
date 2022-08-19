using Autodesk.AutoCAD.Geometry;

namespace ThMEPTCH.Model
{
    public class ThTCHCross
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
        /// 弯通风格
        /// </summary>
        public string CrossStyle { get; set; }

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
        /// 长度
        /// </summary>
        public double Length { get; set; }

        /// <summary>
        /// 是否有盖板
        /// </summary>
        public bool Cover { get; set; }

        /// <summary>
        /// 插入点
        /// </summary>
        public Point3d MidPosition { get; set; }

        /// <summary>
        /// 是否斜向构件
        /// </summary>
        public bool InclineFit { get; set; }

        /// <summary>
        /// 隔板Id
        /// </summary>
        public ThTCHTelecClapboard Clapboard { get; set; }

        /// <summary>
        /// 起点接口Id
        /// </summary>
        public ThTCHTelecInterface MajInterfaceId { get; set; }

        /// <summary>
        /// 分支方向接口id
        /// </summary>
        public ThTCHTelecInterface MinInterfaceId { get; set; }

        /// <summary>
        /// 第三方向接口id
        /// </summary>
        public ThTCHTelecInterface Min2InterfaceId { get; set; }

        /// <summary>
        /// 第四方向接口id
        /// </summary>
        public ThTCHTelecInterface Min3InterfaceId { get; set; }
    }
}
