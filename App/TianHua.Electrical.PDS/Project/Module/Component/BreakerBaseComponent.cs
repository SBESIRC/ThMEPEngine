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
        public string FrameSpecification { get; set; }

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

        /// <summary>
        /// 设置级数
        /// </summary>
        /// <param name="polesNum"></param>
        public abstract void SetPolesNum(string polesNum);
        public abstract List<string> GetPolesNums();


        /// <summary>
        /// 设置脱扣器
        /// </summary>
        /// <param name="tripDevice"></param>
        public abstract void SetTripDevice(string tripDevice);
        public abstract List<string> GetTripDevices();


        /// <summary>
        /// 设置额定电流
        /// </summary>
        /// <param name="ratedCurrentStr"></param>
        public abstract void SetRatedCurrent(string ratedCurrentStr);
        public abstract List<string> GetRatedCurrents();

        /// <summary>
        /// 设置壳架规格
        /// </summary>
        /// <param name="frameSpecifications"></param>
        public abstract void SetFrameSpecification(string frameSpecifications);
        public abstract List<string> GetFrameSpecifications();

        /// <summary>
        /// 设置型号
        /// </summary>
        /// <param name="model"></param>
        public abstract void SetModel(BreakerModel model);
        public abstract List<BreakerModel> GetModels();
    }
}
