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
            AlternativePolesNums = ATSEComponents.Select(o => o.Poles).Distinct().ToList();
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
            && o.Poles == PolesNum
            && o.FrameSize == FrameSpecification
            && o.Amps.ToString() == RatedCurrent))
            {
                this.Model = model;
            }
            else
            {
                var components = ATSEComponents.First(o => o.Model == model);
                Model = components.Model;
                PolesNum = components.Poles;
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
            && o.Poles == PolesNum
            && o.FrameSize == frameSize
            && o.Amps.ToString() == RatedCurrent))
            {
                this.FrameSpecification = frameSize;
            }
            else
            {
                var components = ATSEComponents.First(o => o.FrameSize == frameSize);
                Model = components.Model;
                PolesNum = components.Poles;
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
            if (ATSEComponents.Any(o => o.Poles == polesNum
            && o.Model == Model
            && o.FrameSize == FrameSpecification
            && o.Amps.ToString() == RatedCurrent))
            {
                this.PolesNum = polesNum;
            }
            else
            {
                var components = ATSEComponents.First(o => o.Poles == polesNum);
                Model = components.Model;
                PolesNum = components.Poles;
                FrameSpecification = components.FrameSize;
                RatedCurrent = components.Amps.ToString();
            }
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
            if (ATSEComponents.Any(o => o.Poles == PolesNum
            && o.Model == Model
            && o.FrameSize == FrameSpecification
            && o.Amps.ToString() == ratedCurrent))
            {
                this.RatedCurrent = ratedCurrent;
            }
            else
            {
                var components = ATSEComponents.First(o => o.Amps.ToString() == ratedCurrent);
                Model = components.Model;
                PolesNum = components.Poles;
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
