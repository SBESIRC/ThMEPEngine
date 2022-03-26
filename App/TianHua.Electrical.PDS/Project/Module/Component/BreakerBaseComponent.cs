using System.Collections.Generic;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 断路器（抽象基类）
    /// </summary>
    public abstract class BreakerBaseComponent : PDSBaseComponent
    {
        /// <summary>
        /// 型号
        /// </summary>
        public BreakerModel BreakerType { get; set; }

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

        /// <summary>
        /// 额定电流
        /// </summary>
        protected List<string> AlternativeRatedCurrent { get; set; }

        /// <summary>
        /// 级数
        /// </summary>
        protected List<string> AlternativePolesNum { get; set; }

        /// <summary>
        /// 脱扣器类型
        /// </summary>
        protected List<string> AlternativeTripDevice { get; set; }

        /// <summary>
        /// 壳架规格
        /// </summary>
        protected List<string> AlternativeFrameSpecifications { get; set; }

        /// <summary>
        /// 模型
        /// </summary>
        protected List<BreakerModel> AlternativeModel { get; set; }

        /// <summary>
        /// 标签
        /// </summary>
        public abstract string Content { get; }
    }
}
