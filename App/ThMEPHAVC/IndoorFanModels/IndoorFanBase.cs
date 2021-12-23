namespace ThMEPHVAC.IndoorFanModels
{
    public abstract class IndoorFanBase
    {
        /// <summary>
        /// 设备编号
        /// </summary>
        public string FanNumber { get; set; }
        /// <summary>
        /// 风机风量(制冷量)
        /// </summary>
        public string FanAirVolume { get; set; }
        /// <summary>
        /// 机外静压
        /// </summary>
        public string ExternalStaticVoltage { get; set; }
        /// <summary>
        /// 电源
        /// </summary>
        public string PowerSupply { get; set; }
        /// <summary>
        /// 功率
        /// </summary>
        public string Power { get; set; }
        /// <summary>
        /// 送风管尺寸
        /// </summary>
        public string AirSupplyuctSize { get; set; }
        /// <summary>
        /// 噪声
        /// </summary>
        public string Noise { get; set; }
        /// <summary>
        /// 外形尺寸-长度
        /// </summary>
        public string OverallDimensionLength { get; set; }
        /// <summary>
        /// 外形尺寸-宽度
        /// </summary>
        public string OverallDimensionWidth { get; set; }
        /// <summary>
        /// 外形尺寸-高度
        /// </summary>
        public string OverallDimensionHeight { get; set; }
        /// <summary>
        /// 重量
        /// </summary>
        public string Weight { get; set; }
        /// <summary>
        /// 回风口尺寸
        /// </summary>
        public string ReturnAirOutletSize { get; set; }
        /// <summary>
        /// 送风口形式
        /// </summary>
        public string AirSupplyOutletType { get; set; }
        /// <summary>
        /// 送风口尺寸一个
        /// </summary>
        public string AirSupplyOutletOneSize { get; set; }
        /// <summary>
        /// 送风口尺寸两个
        /// </summary>
        public string AirSupplyOutletTwoSize { get; set; }
        /// <summary>
        /// 描述 - 备注
        /// </summary>
        public string Remarks { get; set; }
        /// <summary>
        /// 风机 - 数量
        /// </summary>
        public string FanCount { get; set; }
    }
}
