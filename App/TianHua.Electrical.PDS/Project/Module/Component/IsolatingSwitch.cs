using System;
using System.Collections.Generic;
using System.Linq;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.PDSProjectException;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 隔离开关
    /// </summary>
    [Serializable]
    public class IsolatingSwitch : PDSBaseComponent
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="calculateCurrent"></param>
        /// <param name="polesNum"></param>
        /// <exception cref="NotSupportedException"></exception>
        public IsolatingSwitch(double calculateCurrent, string polesNum)
        {
            ComponentType = ComponentType.QL;

            var isolators = IsolatorConfiguration.isolatorInfos.Where(o => o.Poles == polesNum && o.Amps >= calculateCurrent).ToList();
            if (isolators.Count == 0)
            {
                throw new NotFoundComponentException("设备库内找不到对应规格的隔离开关");
            }
            var isolator = isolators.First();
            MaxKV = isolator.MaxKV;
            PolesNum = isolator.Poles;
            Model = isolator.Model;
            RatedCurrent = isolator.Amps.ToString();
            this.Isolators = isolators;
            AlternativeModels = Isolators.Select(o => o.Model).Distinct().ToList();
            AlternativePolesNums = Isolators.Select(o => o.Poles).Distinct().ToList();
            AlternativeRatedCurrents = Isolators.Select(o => o.Amps.ToString()).Distinct().ToList();
        }

        /// <summary>
        /// 修改型号
        /// </summary>
        /// <param name="polesNum"></param>
        public void SetModel(string model)
        {
            if (Isolators.Any(o => o.Model == model
            && o.Poles == PolesNum
            && o.Amps.ToString() == RatedCurrent))
            {
                this.Model = model;
            }
            else
            {
                var contactor = Isolators.First(o => o.Model == model);
                Model = contactor.Model;
                PolesNum = contactor.Poles;
                RatedCurrent = contactor.Amps.ToString();
            }
        }
        public List<string> GetModels()
        {
            return AlternativeModels;
        }

        /// <summary>
        /// 修改级数
        /// </summary>
        /// <param name="polesNum"></param>
        public void SetPolesNum(string polesNum)
        {
            if (Isolators.Any(o => o.Poles == polesNum
            && o.Model == Model
            && o.Amps.ToString() == RatedCurrent))
            {
                this.PolesNum = polesNum;
            }
            else
            {
                var contactor = Isolators.First(o => o.Poles == polesNum);
                Model = contactor.Model;
                PolesNum = contactor.Poles;
                RatedCurrent = contactor.Amps.ToString();
            }
        }
        public List<string> GetPolesNums()
        {
            return AlternativePolesNums;
        }

        /// <summary>
        /// 修改额定电流
        /// </summary>
        /// <param name="polesNum"></param>
        public void SetRatedCurrent(string ratedCurrent)
        {
            if (Isolators.Any(o => o.Poles == PolesNum
            && o.Model == Model
            && o.Amps.ToString() == ratedCurrent))
            {
                this.RatedCurrent = ratedCurrent;
            }
            else
            {
                var contactor = Isolators.First(o => o.Amps.ToString() == ratedCurrent);
                Model = contactor.Model;
                PolesNum = contactor.Poles;
                RatedCurrent = contactor.Amps.ToString();
            }
        }
        public List<string> GetRatedCurrents()
        {
            return AlternativeRatedCurrents;
        }

        /// <summary>
        /// 型号
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

        private List<IsolatorConfigurationItem> Isolators { get;}
        private List<string> AlternativeModels { get; }
        private List<string> AlternativePolesNums { get; }
        private List<string> AlternativeRatedCurrents { get; }

        /// <summary>
        /// 额定电压
        /// </summary>
        public string MaxKV { get; set; }
    }
}
