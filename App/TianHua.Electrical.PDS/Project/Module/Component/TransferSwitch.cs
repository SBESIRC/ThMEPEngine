using System;
using System.Collections.Generic;
using System.Linq;
using TianHua.Electrical.PDS.Project.Module.Configure;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// TSE 转换开关
    /// </summary>
    [Serializable]
    public abstract class TransferSwitch : PDSBaseComponent
    {
        /// <summary>
        /// 隔离开关类型
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// 极数
        /// </summary>
        public string PolesNum { get; set; }

        /// <summary>
        /// 额定电流
        /// </summary>
        public string RatedCurrent { get; set; }

        /// <summary>
        /// 壳架规格
        /// </summary>
        public string FrameSpecification { get; set; }

        /// <summary>
        /// 修改型号
        /// </summary>
        /// <param name="polesNum"></param>
        public abstract void SetModel(string model);
        public abstract List<string> GetModels();

        /// <summary>
        /// 修改壳架等级
        /// </summary>
        /// <param name="polesNum"></param>
        public abstract void SetFrameSize(string frameSize);
        public abstract List<string> GetFrameSizes();

        /// <summary>
        /// 修改级数
        /// </summary>
        /// <param name="polesNum"></param>
        public abstract void SetPolesNum(string polesNum);
        public abstract List<string> GetPolesNums();

        /// <summary>
        /// 修改额定电流
        /// </summary>
        /// <param name="polesNum"></param>
        public abstract void SetRatedCurrent(string ratedCurrent);
        public abstract List<string> GetRatedCurrents();

        protected List<string> AlternativeModels { get; set; }
        protected List<string> AlternativePolesNums { get; set; }
        protected List<string> AlternativeRatedCurrents { get; set; }
        protected List<string> AlternativeFrameSpecifications { get; set; }
    }
}
