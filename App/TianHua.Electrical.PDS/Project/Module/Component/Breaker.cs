using System;
using System.Linq;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.Module.Component.Extension;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 断路器（抽象基类）
    /// </summary>
    public abstract class BreakerBaseComponent : PDSBaseComponent
    {

    }

    /// <summary>
    /// 断路器
    /// </summary>
    [CascadeComponent]
    public class Breaker : BreakerBaseComponent
    {
        /// <summary>
        /// 断路器
        /// </summary>
        /// <param name="calculateCurrent">计算电流</param>
        /// <param name="tripDevice">脱扣器类型</param>
        /// <param name="polesNum">极数</param>
        /// <param name="characteristics">瞬时脱扣器类型</param>
        public Breaker(double calculateCurrent, List<string> tripDevice, string polesNum, string characteristics)
        {
            if(ProjectGlobalConfiguration.SinglePhasePolesNum.Contains(polesNum))
            {
                AlternativePolesNum = ProjectGlobalConfiguration.SinglePhasePolesNum;
            }
            else if (ProjectGlobalConfiguration.ThreePhasePolesNum.Contains(polesNum))
            {
                AlternativePolesNum = ProjectGlobalConfiguration.ThreePhasePolesNum;
            }
            ComponentType = ComponentType.断路器;
            var breakers = BreakerConfiguration.breakerComponentInfos.
                Where(o => o.Amps > calculateCurrent
                && tripDevice.Contains(o.TripDevice)
                && AlternativePolesNum.Contains(o.Poles)
                && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(characteristics))).ToList();
            if (breakers.Count == 0)
            {
                throw new NotSupportedException();
            }
            var breaker = breakers.First(o => o.DefaultPick &&  o.Poles == polesNum);
            BreakerType = breaker.Model;
            FrameSpecifications = breaker.FrameSize;
            PolesNum =breaker.Poles;
            RatedCurrent =breaker.Amps.ToString();
            TripUnitType =breaker.TripDevice;

            
            Characteristics = characteristics;
            Breakers = breakers;
            AlternativeModel = breakers.Select(o => o.Model).Distinct().ToList();
            AlternativeFrameSpecifications = breakers.Select(o => o.FrameSize).Distinct().ToList();
            AlternativeRatedCurrent = breakers.Select(o => o.Amps).Distinct().OrderBy(o => o).Select(o => o.ToString()).ToList();
            AlternativeTripDevice = tripDevice;
        }

        /// <summary>
        /// 修改级数
        /// </summary>
        /// <param name="polesNum"></param>
        public void SetPolesNum(string polesNum)
        {
            if (Breakers.Any(o => o.Poles == polesNum 
            && o.Model == BreakerType
            && o.FrameSize == FrameSpecifications 
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType 
            && o.Amps == double.Parse(RatedCurrent)))
            {
                this.PolesNum = polesNum;
            }
            else
            {
                var breaker = Breakers.First(o => o.Poles == polesNum);
                BreakerType = breaker.Model;
                FrameSpecifications = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
            }
        }

        /// <summary>
        /// 修改脱扣器类型
        /// </summary>
        /// <param name="tripDevice"></param>
        public void SetTripDevice(string tripDevice)
        {
            if (Breakers.Any(o => o.Poles ==  PolesNum
            && o.Model == BreakerType
            && o.FrameSize == FrameSpecifications 
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == tripDevice
            && o.Amps == double.Parse(RatedCurrent)))
            {
                this.TripUnitType = tripDevice;
            }
            else
            {
                var breaker = Breakers.First(o => o.TripDevice == tripDevice);
                BreakerType = breaker.Model;
                FrameSpecifications = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
            }
        }
        
        /// <summary>
        /// 修改额定电流
        /// </summary>
        /// <param name="ratedCurrentStr"></param>
        public void SetRatedCurrent(string ratedCurrentStr)
        {
            var ratedCurrent = double.Parse(ratedCurrentStr);
            if (Breakers.Any(o => o.Poles ==  PolesNum
            && o.Model == BreakerType
            && o.FrameSize == FrameSpecifications 
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType
            && o.Amps == ratedCurrent))
            {
                this.RatedCurrent = ratedCurrentStr;
            }
            else
            {
                var breaker = Breakers.First(o => o.Amps == ratedCurrent);
                BreakerType = breaker.Model;
                FrameSpecifications = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
            }
        }
        
        /// <summary>
        /// 修改壳架规格
        /// </summary>
        /// <param name="ratedCurrentStr"></param>
        public void SetFrameSpecifications(string frameSpecifications)
        {
            if (Breakers.Any(o => o.Poles ==  PolesNum
            && o.Model == BreakerType
            && o.FrameSize == frameSpecifications
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType
            && o.Amps == double.Parse(RatedCurrent)))
            {
                this.FrameSpecifications = frameSpecifications;
            }
            else
            {
                var breaker = Breakers.First(o => o.FrameSize == frameSpecifications);
                BreakerType = breaker.Model;
                FrameSpecifications = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
            }
        }

        /// <summary>
        /// 修改型号
        /// </summary>
        /// <param name="ratedCurrentStr"></param>
        public void SetModel(string model)
        {
            if (Breakers.Any(o => o.Poles ==  PolesNum
            && o.Model == model
            && o.FrameSize == FrameSpecifications
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType
            && o.Amps == double.Parse(RatedCurrent)))
            {
                this.BreakerType= model;
            }
            else
            {
                var breaker = Breakers.First(o => o.Model == model);
                BreakerType = breaker.Model;
                FrameSpecifications = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
            }
        }

        /// <summary>
        /// 瞬时脱扣器类型
        /// </summary>
        private string Characteristics { get; set;}

        private List<BreakerComponentInfo> Breakers { get; set; }

        public string Content { get { return $"{BreakerType}{FrameSpecifications}-{TripUnitType}{RatedCurrent}/{PolesNum}"; } }

        /// <summary>
        /// 型号
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

        /// <summary>
        /// 额定电流
        /// </summary>
        public List<string> AlternativeRatedCurrent { get; }
        /// <summary>
        /// 级数
        /// </summary>
        public List<string> AlternativePolesNum { get; }
        /// <summary>
        /// 脱扣器类型
        /// </summary>
        public List<string> AlternativeTripDevice { get; }
        /// <summary>
        /// 壳架规格
        /// </summary>
        public List<string> AlternativeFrameSpecifications { get; }
        /// <summary>
        /// 模型
        /// </summary>
        public List<string> AlternativeModel { get; }

        public override double GetCascadeRatedCurrent()
        {
            if (double.TryParse(RatedCurrent, out double result))
            {
                return result;
            }
            return 0;
        }
    }

    /// <summary>
    /// 剩余电流断路器（带漏电保护功能的断路器）
    /// </summary>
    [CascadeComponent]
    public class ResidualCurrentBreaker : BreakerBaseComponent
    {
        /// <summary>
        /// 断路器
        /// </summary>
        /// <param name="calculateCurrent">计算电流</param>
        /// <param name="tripDevice">脱扣器类型</param>
        /// <param name="polesNum">极数</param>
        /// <param name="characteristics">瞬时脱扣器类型</param>
        public ResidualCurrentBreaker(double calculateCurrent, List<string> tripDevice, string polesNum, string characteristics)
        {
            if (ProjectGlobalConfiguration.SinglePhasePolesNum.Contains(polesNum))
            {
                AlternativePolesNum = ProjectGlobalConfiguration.SinglePhasePolesNum;
            }
            else if (ProjectGlobalConfiguration.ThreePhasePolesNum.Contains(polesNum))
            {
                AlternativePolesNum = ProjectGlobalConfiguration.ThreePhasePolesNum;
            }
            ComponentType = ComponentType.剩余电流断路器;
            var breakers = BreakerConfiguration.breakerComponentInfos.
                Where(o => o.Amps > calculateCurrent
                && tripDevice.Contains(o.TripDevice)
                && AlternativePolesNum.Contains(o.Poles)
                && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(characteristics))).ToList();
            if (breakers.Count == 0)
            {
                throw new NotSupportedException();
            }
            var breaker = breakers.First(o => o.DefaultPick &&  o.Poles == polesNum);
            BreakerType = breaker.Model;
            FrameSpecifications = breaker.FrameSize;
            PolesNum =breaker.Poles;
            RatedCurrent =breaker.Amps.ToString();
            TripUnitType =breaker.TripDevice;


            Characteristics = characteristics;
            Breakers = breakers;
            AlternativeModel = breakers.Select(o => o.Model).Distinct().ToList();
            AlternativeFrameSpecifications = breakers.Select(o => o.FrameSize).Distinct().ToList();
            AlternativeRatedCurrent = breakers.Select(o => o.Amps).Distinct().OrderBy(o => o).Select(o => o.ToString()).ToList();
            AlternativeTripDevice = tripDevice;
        }

        /// <summary>
        /// 修改级数
        /// </summary>
        /// <param name="polesNum"></param>
        public void SetPolesNum(string polesNum)
        {
            if (Breakers.Any(o => o.Poles == polesNum
            && o.Model == BreakerType
            && o.FrameSize == FrameSpecifications
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType
            && o.Amps == double.Parse(RatedCurrent)))
            {
                this.PolesNum = polesNum;
            }
            else
            {
                var breaker = Breakers.First(o => o.Poles == polesNum);
                BreakerType = breaker.Model;
                FrameSpecifications = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
            }
        }

        /// <summary>
        /// 修改脱扣器类型给
        /// </summary>
        /// <param name="tripDevice"></param>
        public void SetTripDevice(string tripDevice)
        {
            if (Breakers.Any(o => o.Poles ==  PolesNum
            && o.Model == BreakerType
            && o.FrameSize == FrameSpecifications
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == tripDevice
            && o.Amps == double.Parse(RatedCurrent)))
            {
                this.TripUnitType = tripDevice;
            }
            else
            {
                var breaker = Breakers.First(o => o.TripDevice == tripDevice);
                BreakerType = breaker.Model;
                FrameSpecifications = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
            }
        }

        /// <summary>
        /// 修改额定电流
        /// </summary>
        /// <param name="ratedCurrentStr"></param>
        public void SetRatedCurrent(string ratedCurrentStr)
        {
            var ratedCurrent = double.Parse(ratedCurrentStr);
            if (Breakers.Any(o => o.Poles ==  PolesNum
            && o.Model == BreakerType
            && o.FrameSize == FrameSpecifications
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType
            && o.Amps == ratedCurrent))
            {
                this.RatedCurrent = ratedCurrentStr;
            }
            else
            {
                var breaker = Breakers.First(o => o.Amps == ratedCurrent);
                BreakerType = breaker.Model;
                FrameSpecifications = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
            }
        }

        /// <summary>
        /// 修改壳架规格
        /// </summary>
        /// <param name="ratedCurrentStr"></param>
        public void SetFrameSpecifications(string frameSpecifications)
        {
            if (Breakers.Any(o => o.Poles ==  PolesNum
            && o.Model == BreakerType
            && o.FrameSize == frameSpecifications
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType
            && o.Amps == double.Parse(RatedCurrent)))
            {
                this.FrameSpecifications = frameSpecifications;
            }
            else
            {
                var breaker = Breakers.First(o => o.FrameSize == frameSpecifications);
                BreakerType = breaker.Model;
                FrameSpecifications = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
            }
        }

        /// <summary>
        /// 修改型号
        /// </summary>
        /// <param name="ratedCurrentStr"></param>
        public void SetModel(string model)
        {
            if (Breakers.Any(o => o.Poles ==  PolesNum
            && o.Model == model
            && o.FrameSize == FrameSpecifications
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType
            && o.Amps == double.Parse(RatedCurrent)))
            {
                this.BreakerType= model;
            }
            else
            {
                var breaker = Breakers.First(o => o.Model == model);
                BreakerType = breaker.Model;
                FrameSpecifications = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
            }
        }

        /// <summary>
        /// 瞬时脱扣器类型
        /// </summary>
        private string Characteristics { get; set; }

        private List<BreakerComponentInfo> Breakers { get; set; }

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

        public List<string> AlternativeRatedCurrent { get; }

        public List<string> AlternativePolesNum { get; }

        public List<string> AlternativeTripDevice { get; }
        public List<string> AlternativeFrameSpecifications { get; }
        public List<string> AlternativeModel { get; }

        public override double GetCascadeRatedCurrent()
        {
            if (double.TryParse(RatedCurrent, out double result))
            {
                return result;
            }
            return 0;
        }
    }
}
