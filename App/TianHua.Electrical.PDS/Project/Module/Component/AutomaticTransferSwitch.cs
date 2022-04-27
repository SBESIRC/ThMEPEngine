using System;
using System.Collections.Generic;
using System.Linq;
using TianHua.Electrical.PDS.Project.Module.Configure;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// ATSE 自动转换开关
    /// </summary>
    [Serializable]
    public class AutomaticTransferSwitch : TransferSwitch
    {
        public AutomaticTransferSwitch(double calculateCurrent, string polesNum)
        {
            this.ComponentType = ComponentType.ATSE;
            if(polesNum == "2P")
            {
                AlternativePolesNums = new List<string> { "2P" };
            }
            else
            {
                AlternativePolesNums = new List<string> { "3P", "4P" };
            }
            var aTSEComponents = ATSEConfiguration.ATSEComponentInfos.Where(o =>
                o.Amps > calculateCurrent
                && o.Poles.Contains(polesNum)).ToList();
            if (aTSEComponents.Count == 0)
            {
                throw new NotSupportedException();
            }
            ATSEComponents = aTSEComponents;
            var ATSEComponent = aTSEComponents.First();
            Model = ATSEComponent.Model;
            PolesNum = polesNum;
            RatedCurrent = ATSEComponent.Amps.ToString();
            FrameSpecification = ATSEComponent.FrameSize;

            AlternativeModels = ATSEComponents.Select(o => o.Model).Distinct().ToList();
            AlternativeRatedCurrents = ATSEComponents.Select(o => o.Amps.ToString()).Distinct().ToList();
            AlternativeFrameSpecifications = ATSEComponents.Select(o => o.FrameSize).Distinct().ToList();
        }

        /// <summary>
        /// 修改型号
        /// </summary>
        /// <param name="polesNum"></param>
        public override void SetModel(string model)
        {
            if (ATSEComponents.Any(o => o.Model == model
            && o.FrameSize == FrameSpecification
            && o.Amps.ToString() == RatedCurrent))
            {
                this.Model = model;
            }
            else
            {
                var components = ATSEComponents.First(o => o.Model == model);
                Model = components.Model;
                RatedCurrent = components.Amps.ToString();
                FrameSpecification= components.FrameSize;
            }
        }
        public override List<string> GetModels()
        {
            return AlternativeModels;
        }

        /// <summary>
        /// 修改壳架等级
        /// </summary>
        /// <param name="polesNum"></param>
        public override void SetFrameSize(string frameSize)
        {
            if (ATSEComponents.Any(o => o.Model == Model
            && o.FrameSize == frameSize
            && o.Amps.ToString() == RatedCurrent))
            {
                this.FrameSpecification = frameSize;
            }
            else
            {
                var components = ATSEComponents.First(o => o.FrameSize == frameSize);
                Model = components.Model;
                FrameSpecification = components.FrameSize;
                RatedCurrent = components.Amps.ToString();
            }
        }
        public override List<string> GetFrameSizes()
        {
            return AlternativeFrameSpecifications;
        }

        /// <summary>
        /// 修改级数
        /// </summary>
        /// <param name="polesNum"></param>
        public override void SetPolesNum(string polesNum)
        {
            this.PolesNum = polesNum;
        }
        public override List<string> GetPolesNums()
        {
            return AlternativePolesNums;
        }

        /// <summary>
        /// 修改额定电流
        /// </summary>
        /// <param name="polesNum"></param>
        public override void SetRatedCurrent(string ratedCurrent)
        {
            if (ATSEComponents.Any(o => o.Model == Model
            && o.FrameSize == FrameSpecification
            && o.Amps.ToString() == ratedCurrent))
            {
                this.RatedCurrent = ratedCurrent;
            }
            else
            {
                var components = ATSEComponents.First(o => o.Amps.ToString() == ratedCurrent);
                Model = components.Model;
                FrameSpecification = components.FrameSize;
                RatedCurrent = components.Amps.ToString();
            }
        }
        public override List<string> GetRatedCurrents()
        {
            return AlternativeRatedCurrents;
        }
        private List<ATSEComponentInfo> ATSEComponents { get; set; }
    }
}
