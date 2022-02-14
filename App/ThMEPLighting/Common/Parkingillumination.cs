namespace ThMEPLighting.Common
{
    public class Parkingillumination
    {
        /// <summary>
        /// 车位照度要求（Eav）
        /// </summary>
        public double MastIllumination { get; set; }
        /// <summary>
        /// 灯具额定光通（Φs）
        /// </summary>
        public double LightRatedIllumination { get; set; }
        /// <summary>
        /// 灯具额定功率（W）
        /// </summary>
        public double LightRatedPower { get; set; }
        /// <summary>
        /// 利用系数
        /// </summary>
        public double UtilizationCoefficient { get; set; }
        /// <summary>
        /// 维护系数
        /// </summary>
        public double MaintenanceFactor { get; set; }
        /// <summary>
        /// 显示计算结果
        /// </summary>
        public bool ShowResult { get; set; }
    }
}
