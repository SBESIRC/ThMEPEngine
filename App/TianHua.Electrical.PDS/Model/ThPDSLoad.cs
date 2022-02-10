using Autodesk.AutoCAD.Geometry;

namespace TianHua.Electrical.PDS.Model
{
    /// <summary>
    /// 负载
    /// </summary>
    public class ThPDSLoad
    {
        /// <summary>
        /// 负载编号
        /// </summary>
        public string LoadID { get; set; }

        /// <summary>
        /// 额定电压
        /// </summary>
        public double KV { get; set; }

        /// <summary>
        /// 负载类型
        /// </summary>
        public string LoadType { get; set; }

        /// <summary>
        /// 是否是消防设备
        /// </summary>
        public string FireLoad { get; set; }

        /// <summary>
        /// 上级配电箱编号
        /// </summary>
        public string SourcePanelID { get; set; }

        /// <summary>
        /// 回路ID
        /// </summary>
        public string CircuitID { get; set; }

        /// <summary>
        /// 回路编号
        /// </summary>
        public string CircuitNumber { get; set; }

        /// <summary>
        /// 用户自定义描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 主用设备数量
        /// </summary>
        public int PrimaryAvail { get; set; }

        /// <summary>
        /// 备用设备数量
        /// </summary>
        public int SpareAvail { get; set; }

        /// <summary>
        /// 安装功率
        /// </summary>
        public double InstalledCapacity { get; set; }

        /// <summary>
        /// 相数
        /// </summary>
        public double Phase { get; set; }

        /// <summary>
        /// 需要系数
        /// </summary>
        public double DemandFactor { get; set; }

        /// <summary>
        /// 功率因数
        /// </summary>
        public double PowerFactor { get; set; }

        /// <summary>
        /// 所属DWG
        /// </summary>
        public string ReferenceDWG { get; set; }

        /// <summary>
        /// 楼层
        /// </summary>
        public int FloorNumber { get; set; }

        /// <summary>
        /// 房间
        /// </summary>
        public string RoomType { get; set; }

        /// <summary>
        /// 基点坐标
        /// </summary>
        public Point3d BasePoint { get; set; }
    }
}
