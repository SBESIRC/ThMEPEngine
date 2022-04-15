namespace ThMEPHVAC.EQPMFanModelEnums
{
    public class FanVolumeCalcModel
    {
        /// <summary>
        /// 风量计算值(输入框输入)
        /// </summary>
        public int AirCalcValue { get; set; }
        /// <summary>
        /// 风量计算系数
        /// </summary>
        public double AirCalcFactor { get; set; }
        /// <summary>
        /// 风量
        /// </summary>
        public int AirVolume { get; set; }
    }
}
