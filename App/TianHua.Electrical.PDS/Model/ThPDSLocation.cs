using System;

namespace TianHua.Electrical.PDS.Model
{
    [Serializable]
    public class ThPDSLocation
    {
        public ThPDSLocation()
        {
            ReferenceDWG = "";
            FloorNumber = "";
            IsStandardStorey = false;
            RoomType = "";
            BasePoint = new ThPDSPoint3d();
            MinPoint = new ThPDSPoint3d();
            MaxPoint = new ThPDSPoint3d();
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
        /// 是否为标准层
        /// </summary>
        public bool IsStandardStorey { get; set; }

        /// <summary>
        /// 房间
        /// </summary>
        public string RoomType { get; set; }

        /// <summary>
        /// 基点坐标
        /// </summary>
        public ThPDSPoint3d BasePoint { get; set; }

        /// <summary>
        /// 图元范围最小点
        /// </summary>
        public ThPDSPoint3d MinPoint { get; set; }

        /// <summary>
        /// 图元范围最大点
        /// </summary>
        public ThPDSPoint3d MaxPoint { get; set; }

        /// <summary>
        /// 楼层基点
        /// </summary>
        public ThPDSPoint3d StoreyBasePoint { get; set; }
    }
}
