namespace TianHua.Hvac.UI.EQPMFanSelect
{
    class ExportFanParaModel
    {
        public string ID { get; set; }
        /// <summary>
        /// 场景
        /// </summary>
        public string Scenario { get; set; }
        /// <summary>
        /// 场景编号
        /// </summary>
        public string ScenarioCode { get; set; }
        /// <summary>
        /// 设备编号
        /// </summary>
        public string No { get; set; }
        /// <summary>
        /// 服务区域
        /// </summary>
        public string Coverage { get; set; }
        /// <summary>
        /// 风机形式
        /// </summary>
        public string FanForm { get; set; }
        /// <summary>
        /// 计算风量
        /// </summary>
        public string CalcAirVolume { get; set; }
        /// <summary>
        /// 风机风量
        /// </summary>
        public string FanDelivery { get; set; }
        /// <summary>
        /// 余压
        /// </summary>
        public string Pa { get; set; }
        /// <summary>
        /// 静压
        /// </summary>
        public string StaticPa { get; set; }
        /// <summary>
        /// 风机能效等级
        /// </summary>
        public string FanEnergyLevel { get; set; }
        /// <summary>
        /// 风机内效率
        /// </summary>
        public string FanEfficiency { get; set; }
        /// <summary>
        /// 风机转速
        /// </summary>
        public string FanRpm { get; set; }
        /// <summary>
        /// 驱动方式
        /// </summary>
        public string DriveMode { get; set; }
        /// <summary>
        /// 机电能效等级
        /// </summary>
        public string ElectricalEnergyLevel { get; set; }
        /// <summary>
        /// 电机功率
        /// </summary>
        public string MotorPower { get; set; }
        /// <summary>
        /// 电源
        /// </summary>
        public string PowerSource { get; set; }
        /// <summary>
        /// 电机转速
        /// </summary>
        public string ElectricalRpm { get; set; }
        /// <summary>
        /// 是否双速
        /// </summary>
        public string IsDoubleSpeed { get; set; }
        /// <summary>
        /// 是否变频
        /// </summary>
        public string IsFrequency { get; set; }
        /// <summary>
        /// 单位风量耗功率
        /// </summary>
        public string WS { get; set; }
        /// <summary>
        /// 消防
        /// </summary>
        public string IsFirefighting { get; set; }
        /// <summary>
        /// 噪声
        /// </summary>
        public string dB { get; set; }
        /// <summary>
        /// 重量
        /// </summary>
        public string Weight { get; set; }
        /// <summary>
        /// 长
        /// </summary>
        public string Length { get; set; }
        /// <summary>
        /// 宽(直径)
        /// </summary>
        public string Width { get; set; }
        /// <summary>
        /// 高
        /// </summary>
        public string Height { get; set; }
        /// <summary>
        /// 减振方式 
        /// </summary>
        public string VibrationMode { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public string Amount { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 场景排序
        /// </summary>
        public int SortScenario { get; set; }
        /// <summary>
        /// 排序ID
        /// </summary>
        public int SortID { get; set; }
        
    }
}
