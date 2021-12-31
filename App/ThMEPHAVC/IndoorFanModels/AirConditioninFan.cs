namespace ThMEPHVAC.IndoorFanModels
{
    /// <summary>
    /// 吊顶一体式空调
    /// </summary>
    public class AirConditioninFan : IndoorFanBase
    {
        /// <summary>
        /// 风机信息 - 全压
        /// </summary>
        public string FanFullPressure { get; set; }
        /// <summary>
        /// 风机信息 - 余压
        /// </summary>
        public string FanResidualPressure { get; set; }
        /// <summary>
        /// 盘管排数
        /// </summary>
        public string FanCoilRow { get; set; }
        /// <summary>
        /// 风机信息 风机台数
        /// </summary>
        public string AirConditionCount { get; set; }
        /// <summary>
        /// 冷却工况 - 冷量
        /// </summary>
        public string CoolCoolingCapacity { get; set; }
        /// <summary>
        /// 冷却工况 - 进风参数干球温度
        /// </summary>
        public string CoolAirInletDryBall { get; set; }
        /// <summary>
        /// 冷却工况 - 进风参数 湿球温度
        /// </summary>
        public string CoolAirInletWetBall { get; set; }
        /// <summary>
        /// 冷却工况 - 进口水温
        /// </summary>
        public string CoolEnterPortWaterTEMP { get; set; }
        /// <summary>
        /// 冷却工况 - 出口水温
        /// </summary>
        public string CoolExitWaterTEMP { get; set; }
        /// <summary>
        /// 冷却工况 - 流量
        /// </summary>
        public string CoolFlow { get; set; }
        /// <summary>
        /// 冷却工况 - 水侧阻力
        /// </summary>
        public string CoolHydraulicResistance { get; set; }
        /// <summary>
        /// 加热工况 - 热量
        /// </summary>
        public string HotHeatingCapacity { get; set; }
        /// <summary>
        /// 加热工况 - 进风温度
        /// </summary>
        public string HotAirInletTEMP { get; set; }
        /// <summary>
        /// 加热工况 - 进口水温
        /// </summary>
        public string HotEnterPortWaterTEMP { get; set; }
        /// <summary>
        /// 加热工况 - 出口水温
        /// </summary>
        public string HotExitWaterTEMP { get; set; }
        /// <summary>
        /// 加热工况 - 工作压力
        /// </summary>
        public string HotWorkXeF { get; set; }
        /// <summary>
        /// 加热工况 - 流量
        /// </summary>
        public string HotFlow { get; set; }
        /// <summary>
        /// 分支水管 - 冷热水管管径
        /// </summary>
        public string BruchCollHotWaterPipeSize { get; set; }
        /// <summary>
        /// 分支水管 - 冷凝水管管径
        /// </summary>
        public string BruchCondensationPipeSize { get; set; }
        /// <summary>
        /// 过滤器
        /// </summary>
        public string Filter { get; set; }
        /// <summary>
        /// 减震方式
        /// </summary>
        public string DampingMode { get; set; }
    }
}
