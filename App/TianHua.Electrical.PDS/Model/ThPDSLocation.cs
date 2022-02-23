using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSLocation
    {
        /// <summary>
        /// 所属DWG
        /// </summary>
        public Database ReferenceDWG { get; set; }

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
