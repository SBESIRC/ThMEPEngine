namespace ThMEPHVAC.IndoorFanModels
{
    /// <summary>
    /// 风机盘管风机数据
    /// </summary>
    public class CoilUnitFan : IndoorFanBase
    {
        public string FanLayout { get; set; }
        /// <summary>
        /// 冷却盘管全热
        /// </summary>
        public string CoolTotalHeat { get; set; }
        /// <summary>
        /// 冷却盘管显热
        /// </summary>
        public string CoolShowHeat { get; set; }
        /// <summary>
        /// 冷却盘管进风参数干球温度
        /// </summary>
        public string CoolAirInletDryBall { get; set; }
        /// <summary>
        /// 冷却盘管进风参数相对湿度
        /// </summary>
        public string CoolAirInletHumidity { get; set; }
        /// <summary>
        /// 冷却盘管进口水温
        /// </summary>
        public string CoolEnterPortWaterTEMP { get; set; }
        /// <summary>
        /// 冷却盘管出口水温
        /// </summary>
        public string CoolExitWaterTEMP { get; set; }
        /// <summary>
        /// 冷却盘管接管尺寸
        /// </summary>
        public string CoolPipeSize { get; set; }
        /// <summary>
        /// 冷却盘管工作压力
        /// </summary>
        public string CoolWorkXeF { get; set; }
        /// <summary>
        /// 冷却盘管压降
        /// </summary>
        public string CoolXeFDrop { get; set; }
        /// <summary>
        /// 冷却盘管流量
        /// </summary>
        public string CoolFlow { get; set; }
        /// <summary>
        /// 加热盘管热量
        /// </summary>
        public string HotHeat { get; set; }
        /// <summary>
        /// 加热盘管进风温度干球
        /// </summary>
        public string HotAirInletDryBall { get; set; }
        /// <summary>
        /// 加热盘管进口水温
        /// </summary>
        public string HotEnterPortWaterTEMP { get; set; }
        /// <summary>
        /// 加热盘管出口水温
        /// </summary>
        public string HotExitWaterTEMP { get; set; }
        /// <summary>
        /// 加热盘管接管尺寸
        /// </summary>
        public string HotPipSize { get; set; }
        /// <summary>
        /// 加热盘管工作压力
        /// </summary>
        public string HotWorkXeF { get; set; }
        /// <summary>
        /// 加热盘管压降
        /// </summary>
        public string HotXeFDrop { get; set; }
        /// <summary>
        /// 加热盘管流量
        /// </summary>
        public string HotFlow { get; set; }
    }
}
