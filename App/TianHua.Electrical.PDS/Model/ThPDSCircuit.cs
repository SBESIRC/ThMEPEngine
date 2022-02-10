using Autodesk.AutoCAD.Geometry;

namespace TianHua.Electrical.PDS.Model
{
    /// <summary>
    /// 回路类型
    /// </summary>
    public enum ThPDSCircuitType
    {
        /// <summary>
        /// 照明
        /// </summary>
        Lighting,

        /// <summary>
        /// 动力
        /// </summary>
        PowerEquipment,

        /// <summary>
        /// 应急照明
        /// </summary>
        EmergencyLighting,

        /// <summary>
        /// 消防动力
        /// </summary>
        EmergencyPowerEquipment,

        /// <summary>
        /// 消防应急照明
        /// </summary>
        FireEmergencyLighting,

        /// <summary>
        /// 控制
        /// </summary>
        Control
    }

    /// <summary>
    /// 回路
    /// </summary>
    public class ThPDSCircuit
    {
        /// <summary>
        /// 回路ID
        /// </summary>
        public string UID { get; set; }

        /// <summary>
        /// 回路编号
        /// </summary>
        public string Number { get; set; }

        /// <summary>
        /// 回路类型
        /// </summary>
        public ThPDSCircuitType Type { get; set; }

        /// <summary>
        /// 额定电压
        /// </summary>
        public double KV { get; set; }

        /// <summary>
        /// 上级配电箱编号
        /// </summary>
        public string SourcePanelID { get; set; }

        /// <summary>
        /// 负载编号
        /// </summary>
        public string LoadID { get; set; }

        /// <summary>
        /// 用户自定义描述
        /// </summary>
        public string Description { get; set; }

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
