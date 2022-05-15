using System;
using System.Collections.Generic;
using System.Linq;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.PDSProjectException;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// MTSE 手动转换开关
    /// </summary>
    [Serializable]
    public class ManualTransferSwitch : TransferSwitch
    {
        public ManualTransferSwitch(double calculateCurrent, string polesNum)
        {
            this.ComponentType = ComponentType.MTSE;

            if (polesNum == "2P")
            {
                AlternativePolesNums = new List<string> { "2P" };
            }
            else
            {
                AlternativePolesNums = new List<string> { "3P", "4P" };
            }
            var mTSEComponent = MTSEConfiguration.MTSEComponentInfos.Where(o =>
                o.Amps > calculateCurrent
                && o.Poles.Contains(polesNum)).ToList();
            if (mTSEComponent.Count == 0)
            {
                throw new NotFoundComponentException("设备库内找不到对应规格的MTSE");
            }
            MTSEComponents  = mTSEComponent;
            var MTSEComponent = mTSEComponent.First();
            Model = MTSEComponent.Model;
            PolesNum = polesNum;
            FrameSpecification = MTSEComponent.FrameSize;
            RatedCurrent = MTSEComponent.Amps.ToString();

            AlternativeModels = MTSEComponents.Select(o => o.Model).Distinct().ToList();
            AlternativeRatedCurrents = MTSEComponents.Select(o => o.Amps.ToString()).Distinct().ToList();
            AlternativeFrameSpecifications = MTSEComponents.Select(o => o.FrameSize).Distinct().ToList();
        }

        /// <summary>
        /// 修改型号
        /// </summary>
        /// <param name="polesNum"></param>
        public override void SetModel(string model)
        {
            if (MTSEComponents.Any(o => o.Model == model
            && o.FrameSize == FrameSpecification
            && o.Amps.ToString() == RatedCurrent))
            {
                this.Model = model;
            }
            else
            {
                var components = MTSEComponents.First(o => o.Model == model);
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
            if (MTSEComponents.Any(o => o.Model == Model
            && o.FrameSize == frameSize
            && o.Amps.ToString() == RatedCurrent))
            {
                this.FrameSpecification = frameSize;
            }
            else
            {
                var components = MTSEComponents.First(o => o.FrameSize == frameSize);
                Model = components.Model;
                RatedCurrent = components.Amps.ToString();
                FrameSpecification= components.FrameSize;
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
            if (MTSEComponents.Any(o => o.Model == Model
            && o.FrameSize == FrameSpecification
            && o.Amps.ToString() == ratedCurrent))
            {
                this.RatedCurrent = ratedCurrent;
            }
            else
            {
                var components = MTSEComponents.First(o => o.Amps.ToString() == ratedCurrent);
                Model = components.Model;
                RatedCurrent = components.Amps.ToString();
                FrameSpecification= components.FrameSize;
            }
        }
        public override List<string> GetRatedCurrents()
        {
            return AlternativeRatedCurrents;
        }

        private List<MTSEComponentInfo> MTSEComponents { get; set; }
    }
}
