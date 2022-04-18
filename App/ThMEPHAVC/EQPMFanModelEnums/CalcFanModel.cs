namespace ThMEPHVAC.EQPMFanModelEnums
{
    public class CalcFanModel
    {
        /// <summary>
        /// 数据来源
        /// </summary>
        public EnumValueSource ValueSource { get; set; }
        /// <summary>
        /// 是否有只风机
        /// </summary>
        public bool HaveChildFan { get; set; }
        /// <summary>
        /// 风机型号表的ID
        /// </summary>
        public string FanModelID { get; set; }
        /// <summary>
        /// 风机型号表的名称
        /// </summary>
        public string FanModelName { get; set; }
        /// <summary>
        /// 型号
        /// </summary>
        public string FanModelNum { get; set; }
        /// <summary>
        /// CCCF规格
        /// </summary>
        public string FanModelCCCF { get; set; }
        /// <summary>
        /// 风机风量
        /// </summary>
        public string FanAirVolume { get; set; }
        /// <summary>
        /// 电机的转速
        /// </summary>
        public int MotorTempo { get; set; }
        /// <summary>
        /// 全压
        /// </summary>
        public string FanModelPa { get; set; }
        /// <summary>
        /// 电机功率
        /// </summary>
        public string FanModelMotorPower { get; set; }
        /// <summary>
        /// 电机功率 输入
        /// </summary>
        public string FanModelInputMotorPower { get; set; }
        /// <summary>
        /// 风机内效率
        /// </summary>
        public string FanInternalEfficiency { get; set; }
        /// <summary>
        /// 噪音
        /// </summary>
        public string FanModelNoise { get; set; }
        /// <summary>
        /// 风机转速
        /// </summary>
        public string FanModelFanSpeed { get; set; }

        /// <summary>
        /// 单位功耗
        /// </summary>
        public string FanModelPower { get; set; }
        /// <summary>
        /// 长
        /// </summary>
        public string FanModelLength { get; set; }
        /// <summary>
        /// 宽
        /// </summary>
        public string FanModelWidth { get; set; }
        /// <summary>
        /// 高
        /// </summary>
        public string FanModelHeight { get; set; }
        /// <summary>
        /// 重量
        /// </summary>
        public string FanModelWeight { get; set; }
        /// <summary>
        /// 直径
        /// </summary>
        public string FanModelDIA { get; set; }
    }
}
