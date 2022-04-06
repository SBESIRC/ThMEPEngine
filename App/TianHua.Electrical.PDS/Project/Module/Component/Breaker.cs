using System;
using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.Module.Component.Extension;
using System.Text.RegularExpressions;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 断路器
    /// </summary>
    [CascadeComponent]
    public class Breaker : BreakerBaseComponent
    {
        /// <summary>
        /// 标签
        /// </summary>
        public override string Content
        {
            get
            {
                return $"{Model}{FrameSpecification}-{TripUnitType}{RatedCurrent}/{PolesNum}";
            }
        }

        /// <summary>
        /// 断路器
        /// </summary>
        /// <param name="calculateCurrent">计算电流</param>
        /// <param name="tripDevice">脱扣器类型</param>
        /// <param name="polesNum">极数</param>
        /// <param name="characteristics">瞬时脱扣器类型</param>
        public Breaker(double calculateCurrent, List<string> tripDevice, string polesNum, string characteristics)
        {
            ComponentType = ComponentType.CB;

            if (ProjectSystemConfiguration.SinglePhasePolesNum.Contains(polesNum))
            {
                AlternativePolesNum = ProjectSystemConfiguration.SinglePhasePolesNum;
            }
            else if (ProjectSystemConfiguration.ThreePhasePolesNum.Contains(polesNum))
            {
                AlternativePolesNum = ProjectSystemConfiguration.ThreePhasePolesNum;
            }
            var breakers = BreakerConfiguration.breakerComponentInfos.
                Where(o => o.Amps > calculateCurrent
                && tripDevice.Contains(o.TripDevice)
                && AlternativePolesNum.Contains(o.Poles)
                && o.ResidualCurrent.IsNullOrWhiteSpace()
                && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(characteristics))).ToList();
            if (breakers.Count == 0)
            {
                throw new NotSupportedException();
            }
            var breaker = breakers.First(o => o.DefaultPick &&  o.Poles == polesNum);
            Model = breaker.Model;
            FrameSpecification = breaker.FrameSize;
            PolesNum =breaker.Poles;
            RatedCurrent =breaker.Amps.ToString();
            TripUnitType =breaker.TripDevice;

            Characteristics = characteristics;
            Breakers = breakers;
            AlternativeModel = breakers.Select(o => o.Model).Distinct().ToList();
            AlternativeFrameSpecifications = breakers.Select(o => o.FrameSize).Distinct().ToList();
            AlternativeRatedCurrent = breakers.Select(o => o.Amps).Distinct().OrderBy(o => o).Select(o => o.ToString()).ToList();
            AlternativeTripDevice = tripDevice;
            AlternativePolesNum = breakers.Select(o => o.Poles).Distinct().ToList();
        }

        /// <summary>
        /// 断路器
        /// </summary>
        /// <param name="breakerConfig">指定断路器配置</param>
        public Breaker(string breakerConfig)
        {
            ComponentType = ComponentType.CB;

            //例：MCB63-MA2.5/3P
            string[] configs = breakerConfig.Split('-');
            string[] detaileds = configs[1].Split('/');
            var model = (BreakerModel)Enum.Parse(typeof(BreakerModel), Regex.Replace(configs[0], @"\d", ""));
            var frameSpecification = Regex.Replace(configs[0], @"\D", "");
            var polesNum =detaileds[1];
            int numIndex = detaileds[0].IndexOfAny(ProjectSystemConfiguration.NumberArray);
            var ratedCurrent = detaileds[0].Substring(numIndex);
            var tripUnitType = detaileds[0].Substring(0,numIndex);

            var breakers = BreakerConfiguration.breakerComponentInfos.
                Where(o => o.Model == model
                && o.FrameSize == frameSpecification
                && o.Poles == polesNum
                && o.TripDevice == tripUnitType
                && o.Amps.ToString() == ratedCurrent
                && o.ResidualCurrent.IsNullOrWhiteSpace()).Take(1).ToList();
            if (breakers.Count == 0)
            {
                throw new NotSupportedException();
            }
            var breaker = breakers.First();
            Model = breaker.Model;
            FrameSpecification = breaker.FrameSize;
            PolesNum =breaker.Poles;
            RatedCurrent =breaker.Amps.ToString();
            TripUnitType =breaker.TripDevice;

            Characteristics = "";
            Breakers = breakers;
            AlternativeModel = breakers.Select(o => o.Model).Distinct().ToList();
            AlternativeFrameSpecifications = breakers.Select(o => o.FrameSize).Distinct().ToList();
            AlternativeRatedCurrent = breakers.Select(o => o.Amps).Distinct().OrderBy(o => o).Select(o => o.ToString()).ToList();
            AlternativeTripDevice = new List<string>() { TripUnitType };
            AlternativePolesNum = new List<string>() { PolesNum };
            //Characteristics = "C";
            //AlternativePolesNum = new List<string>() { PolesNum };
            //AlternativeModel = new List<BreakerModel>() { Model };
            //AlternativeFrameSpecifications = new List<string>() { FrameSpecification };
            //AlternativeRatedCurrent = new List<string>() { RatedCurrent };
            //AlternativeTripDevice = new List<string>() { TripUnitType };
        }

        /// <summary>
        /// 修改级数
        /// </summary>
        /// <param name="polesNum"></param>
        public override void SetPolesNum(string polesNum)
        {
            if (Breakers.Any(o => o.Poles == polesNum 
            && o.Model == Model
            && o.FrameSize == FrameSpecification 
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType 
            && o.Amps == double.Parse(RatedCurrent)))
            {
                this.PolesNum = polesNum;
            }
            else
            {
                var breaker = Breakers.First(o => o.Poles == polesNum);
                Model = breaker.Model;
                FrameSpecification = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
            }
        }
        public override List<string> GetPolesNums()
        {
            return AlternativePolesNum;
        }

        /// <summary>
        /// 修改脱扣器类型
        /// </summary>
        /// <param name="tripDevice"></param>
        public override void SetTripDevice(string tripDevice)
        {
            if (Breakers.Any(o => o.Poles ==  PolesNum
            && o.Model == Model
            && o.FrameSize == FrameSpecification 
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == tripDevice
            && o.Amps == double.Parse(RatedCurrent)))
            {
                this.TripUnitType = tripDevice;
            }
            else
            {
                var breaker = Breakers.First(o => o.TripDevice == tripDevice);
                Model = breaker.Model;
                FrameSpecification = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
            }
        }
        public override List<string> GetTripDevices()
        {
            return AlternativeTripDevice;
        }

        /// <summary>
        /// 修改额定电流
        /// </summary>
        /// <param name="ratedCurrentStr"></param>
        public override void SetRatedCurrent(string ratedCurrentStr)
        {
            var ratedCurrent = double.Parse(ratedCurrentStr);
            if (Breakers.Any(o => o.Poles ==  PolesNum
            && o.Model == Model
            && o.FrameSize == FrameSpecification 
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType
            && o.Amps == ratedCurrent))
            {
                this.RatedCurrent = ratedCurrentStr;
            }
            else
            {
                var breaker = Breakers.First(o => o.Amps == ratedCurrent);
                Model = breaker.Model;
                FrameSpecification = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
            }
        }
        public override List<string> GetRatedCurrents()
        {
            return AlternativeRatedCurrent;
        }

        /// <summary>
        /// 修改壳架规格
        /// </summary>
        /// <param name="ratedCurrentStr"></param>
        public override void SetFrameSpecification(string frameSpecifications)
        {
            if (Breakers.Any(o => o.Poles ==  PolesNum
            && o.Model == Model
            && o.FrameSize == frameSpecifications
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType
            && o.Amps == double.Parse(RatedCurrent)))
            {
                this.FrameSpecification = frameSpecifications;
            }
            else
            {
                var breaker = Breakers.First(o => o.FrameSize == frameSpecifications);
                Model = breaker.Model;
                FrameSpecification = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
            }
        }
        public override List<string> GetFrameSpecifications()
        {
            return AlternativeFrameSpecifications;
        }

        /// <summary>
        /// 修改型号
        /// </summary>
        /// <param name="ratedCurrentStr"></param>
        public override void SetModel(BreakerModel model)
        {
            if (Breakers.Any(o => o.Poles ==  PolesNum
            && o.Model == model
            && o.FrameSize == FrameSpecification
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType
            && o.Amps == double.Parse(RatedCurrent)))
            {
                this.Model= model;
            }
            else
            {
                var breaker = Breakers.First(o => o.Model == model);
                Model = breaker.Model;
                FrameSpecification = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
            }
        }
        public override List<BreakerModel> GetModels()
        {
            return AlternativeModel;
        }

        /// <summary>
        /// 级联
        /// </summary>
        /// <returns></returns>
        public override double GetCascadeRatedCurrent()
        {
            if (double.TryParse(RatedCurrent, out double result))
            {
                return result;
            }
            return 0;
        }

        /// <summary>
        /// 瞬时脱扣器类型
        /// </summary>
        private string Characteristics { get; set; }

        /// <summary>
        /// 断路器信息
        /// </summary>
        private List<BreakerComponentInfo> Breakers { get; set; }
    }
}
