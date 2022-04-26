namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSLocation
    {
        public ThPDSLocation()
        {
            ReferenceDWG = "";
            FloorNumber = "";
            RoomType = "";
            BasePoint = new ThPDSPoint3d();
            StoreyBasePoint = new ThPDSPoint3d();
        }

        /// <summary>
        /// 所属DWG
        /// </summary>
        public string ReferenceDWG { get; set; }

        /// <summary>
        /// 楼层
        /// </summary>
        public string FloorNumber { get; set; }

        /// <summary>
        /// 房间
        /// </summary>
        public string RoomType { get; set; }

        /// <summary>
        /// 基点坐标
        /// </summary>
        public ThPDSPoint3d BasePoint { get; set; }

        /// <summary>
        /// 楼层基点
        /// </summary>
        public ThPDSPoint3d StoreyBasePoint { get; set; }
    }
}
