namespace ThMEPHVAC.IndoorFanModels
{
    /// <summary>
    /// VRF室内机
    /// </summary>
    public class VRFFan : IndoorFanBase
    {
        /// <summary>
        /// 制冷工况 制冷量
        /// </summary>
        public string CoolRefrigeratingCapacity { get; set; }
        /// <summary>
        /// 制冷工况 内机进风参数干球温度
        /// </summary>
        public string CoolAirInletDryBall { get; set; }
        /// <summary>
        /// 制冷工况 内机进风参数湿球温度
        /// </summary>
        public string CoolAirInletWetBall { get; set; }
        /// <summary>
        /// 制冷工况 室外温度
        /// </summary>
        public string CoolOutdoorTemperature { get; set; }

        /// <summary>
        /// 制热工况 制热量
        /// </summary>
        public string HotRefrigeratingCapacity { get; set; }

        /// 制热工况 内机进风温度干球
        /// </summary>
        public string HotAirInletDryBall { get; set; }
        /// <summary>
        /// 制冷工况 室外温度
        /// </summary>
        public string HotOutdoorTemperature { get; set; }
    }
}
