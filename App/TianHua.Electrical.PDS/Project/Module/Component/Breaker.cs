namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 断路器
    /// </summary>
    public class Breaker : BreakerBaseComponent
    {
        public Breaker()
        {
            ComponentType = ComponentType.断路器;
        }

        public string Content { get { return $"{BreakerType}{FrameSpecifications}-{TripUnitType}{RatedCurrent}/{PolesNum}"; } }

        /// <summary>
        /// 模型
        /// </summary>
        public string BreakerType { get; set; }

        /// <summary>
        /// 壳架规格
        /// </summary>
        public string FrameSpecifications { get; set; }

        /// <summary>
        /// 极数
        /// </summary>
        public string PolesNum { get; set; }

        /// <summary>
        /// 额定电流
        /// </summary>
        public string RatedCurrent { get; set; }

        /// <summary>
        /// 脱扣器类型
        /// </summary>
        public string TripUnitType { get; set; }

        /// <summary>
        /// 附件
        /// </summary>
        public string Appendix { get; set; }
    }
}
