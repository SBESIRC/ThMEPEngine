namespace ThMEPWSS.FlushPoint.Model
{
    public class ThFlushPointParameter
    {
        /// <summary>
        /// 出图比例
        /// </summary>
        public string PlotScale { get; set; } = "";
        /// <summary>
        /// 楼层标识
        /// </summary>
        public string FloorSign { get; set; } = "";
        /// <summary>
        /// 保护半径
        /// </summary>
        public double ProtectRadius { get; set; }
        /// <summary>
        /// 保护目标->停车区域
        /// </summary>
        public bool ParkingAreaOfProtectTarget { get; set; }
        /// <summary>
        /// 保护目标->隔油池、污水提升等必布空间
        /// </summary>
        public bool NecessaryArrangeSpaceOfProtectTarget { get; set; }
        /// <summary>
        /// 保护目标->其他空间
        /// </summary>
        public bool OtherSpaceOfProtectTarget { get; set; }
        /// <summary>
        /// 布置策略->必布空间的点位可以保护停车区域和其他空间
        /// </summary>
        public bool NecesaryArrangeSpacePointsOfArrangeStrategy { get; set; }
        /// <summary>
        /// 布置策略->停车区域的点位可以保护其他空间
        /// </summary>
        public bool ParkingAreaPointsOfArrangeStrategy { get; set; }
        /// <summary>
        /// 布置位置->区域满布
        /// </summary>
        public bool AreaFullLayoutOfArrangePosition { get; set; }
        /// <summary>
        /// 布置位置->仅排水设施附近
        /// </summary>
        public bool OnlyDrainageFaclityNearbyOfArrangePosition { get; set; }
        /// <summary>
        /// 点位标识->靠近排水设施
        /// </summary>
        public bool CloseDrainageFacility { get; set; }
        // <summary>
        /// 点位标识->远离排水设施
        /// </summary>
        public bool FarwayDrainageFacility { get; set; }
    }
}
