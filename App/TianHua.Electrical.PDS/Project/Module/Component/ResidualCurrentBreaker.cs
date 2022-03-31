using System;
using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.Module.Component.Extension;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 剩余电流断路器（带漏电保护功能的断路器）
    /// </summary>
    [CascadeComponent]
    public class ResidualCurrentBreaker : BreakerBaseComponent
    {
        /// <summary>
        /// 标签
        /// </summary>
        public override string Content
        {
            get
            {
                return $"{Model}{FrameSpecification}-{TripUnitType}{RatedCurrent}/{PolesNum}/{RCDType}{ResidualCurrent.GetDescription()}";
            }
        }

        /// <summary>
        /// 断路器
        /// </summary>
        /// <param name="calculateCurrent">计算电流</param>
        /// <param name="tripDevice">脱扣器类型</param>
        /// <param name="polesNum">极数</param>
        /// <param name="characteristics">瞬时脱扣器类型</param>
        public ResidualCurrentBreaker(double calculateCurrent, List<string> tripDevice, string polesNum, string characteristics, bool isMotor)
        {
            if (ProjectSystemConfiguration.SinglePhasePolesNum.Contains(polesNum))
            {
                AlternativePolesNum = ProjectSystemConfiguration.SinglePhasePolesNum;
            }
            else if (ProjectSystemConfiguration.ThreePhasePolesNum.Contains(polesNum))
            {
                AlternativePolesNum = ProjectSystemConfiguration.ThreePhasePolesNum;
            }
            ComponentType = ComponentType.RCD;
            var breakers = BreakerConfiguration.breakerComponentInfos.
                Where(o => o.Amps > calculateCurrent
                && tripDevice.Contains(o.TripDevice)
                && !o.ResidualCurrent.IsNullOrWhiteSpace()
                && AlternativePolesNum.Contains(o.Poles)
                && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(characteristics))).ToList();
            if (breakers.Count == 0)
            {
                throw new NotSupportedException();
            }
            IsMotor = isMotor;
            var breaker = breakers.First(o => o.DefaultPick && o.Poles == polesNum);
            Model = breaker.Model;
            FrameSpecification = breaker.FrameSize;
            PolesNum = breaker.Poles;
            RatedCurrent = breaker.Amps.ToString();
            TripUnitType = breaker.TripDevice;

            //剩余电流断路器 的RCD类型默认为A，负载为发动机，剩余电流选300，其余选择30
            RCDType = RCDType.A;
            if (IsMotor)
            {
                ResidualCurrent = ResidualCurrentSpecification.Specification300;
            }
            else
            {
                ResidualCurrent = ResidualCurrentSpecification.Specification30;
            }

            Characteristics = characteristics;
            Breakers = breakers;
            AlternativeModel = breakers.Select(o => o.Model).Distinct().ToList();
            AlternativeFrameSpecifications = breakers.Select(o => o.FrameSize).Distinct().ToList();
            AlternativeRatedCurrent = breakers.Select(o => o.Amps).Distinct().OrderBy(o => o).Select(o => o.ToString()).ToList();
            AlternativeTripDevice = tripDevice;
            AlternativeRCDTypes = breaker.RCDCharacteristics.Split(';').Select(o => o.GetEnumName<RCDType>()).ToList();
            AlternativeResidualCurrents = breaker.ResidualCurrent.Split(';').Select(o => (o + "mA").GetEnumName<ResidualCurrentSpecification>()).ToList();
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
                PolesNum = breaker.Poles;
                RatedCurrent = breaker.Amps.ToString();
                TripUnitType = breaker.TripDevice;
                AlternativeRCDTypes = breaker.RCDCharacteristics.Split(';').Select(o => o.GetEnumName<RCDType>()).ToList();
                AlternativeResidualCurrents = breaker.ResidualCurrent.Split(';').Select(o => (o + "mA").GetEnumName<ResidualCurrentSpecification>()).ToList();
                if (!AlternativeResidualCurrents.Contains(ResidualCurrent))
                {
                    if (IsMotor)
                    {
                        ResidualCurrent = ResidualCurrentSpecification.Specification300;
                    }
                    else
                    {
                        ResidualCurrent = ResidualCurrentSpecification.Specification30;
                    }
                }
            }
        }
        public override List<string> GetPolesNums()
        {
            return AlternativePolesNum;
        }

        /// <summary>
        /// 修改脱扣器类型给
        /// </summary>
        /// <param name="tripDevice"></param>
        public override void SetTripDevice(string tripDevice)
        {
            if (Breakers.Any(o => o.Poles == PolesNum
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
                PolesNum = breaker.Poles;
                RatedCurrent = breaker.Amps.ToString();
                TripUnitType = breaker.TripDevice;
                AlternativeRCDTypes = breaker.RCDCharacteristics.Split(';').Select(o => o.GetEnumName<RCDType>()).ToList();
                AlternativeResidualCurrents = breaker.ResidualCurrent.Split(';').Select(o => (o + "mA").GetEnumName<ResidualCurrentSpecification>()).ToList();
                if (!AlternativeResidualCurrents.Contains(ResidualCurrent))
                {
                    if (IsMotor)
                    {
                        ResidualCurrent = ResidualCurrentSpecification.Specification300;
                    }
                    else
                    {
                        ResidualCurrent = ResidualCurrentSpecification.Specification30;
                    }
                }
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
            if (Breakers.Any(o => o.Poles == PolesNum
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
                PolesNum = breaker.Poles;
                RatedCurrent = breaker.Amps.ToString();
                TripUnitType = breaker.TripDevice;
                AlternativeRCDTypes = breaker.RCDCharacteristics.Split(';').Select(o => o.GetEnumName<RCDType>()).ToList();
                AlternativeResidualCurrents = breaker.ResidualCurrent.Split(';').Select(o => (o + "mA").GetEnumName<ResidualCurrentSpecification>()).ToList();
                if (!AlternativeResidualCurrents.Contains(ResidualCurrent))
                {
                    if (IsMotor)
                    {
                        ResidualCurrent = ResidualCurrentSpecification.Specification300;
                    }
                    else
                    {
                        ResidualCurrent = ResidualCurrentSpecification.Specification30;
                    }
                }
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
            if (Breakers.Any(o => o.Poles == PolesNum
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
                PolesNum = breaker.Poles;
                RatedCurrent = breaker.Amps.ToString();
                TripUnitType = breaker.TripDevice;
                AlternativeRCDTypes = breaker.RCDCharacteristics.Split(';').Select(o => o.GetEnumName<RCDType>()).ToList();
                AlternativeResidualCurrents = breaker.ResidualCurrent.Split(';').Select(o => (o + "mA").GetEnumName<ResidualCurrentSpecification>()).ToList();
                if (!AlternativeResidualCurrents.Contains(ResidualCurrent))
                {
                    if (IsMotor)
                    {
                        ResidualCurrent = ResidualCurrentSpecification.Specification300;
                    }
                    else
                    {
                        ResidualCurrent = ResidualCurrentSpecification.Specification30;
                    }
                }
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
            if (Breakers.Any(o => o.Poles == PolesNum
            && o.Model == model
            && o.FrameSize == FrameSpecification
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType
            && o.Amps == double.Parse(RatedCurrent)))
            {
                this.Model = model;
            }
            else
            {
                var breaker = Breakers.First(o => o.Model == model);
                Model = breaker.Model;
                FrameSpecification = breaker.FrameSize;
                PolesNum = breaker.Poles;
                RatedCurrent = breaker.Amps.ToString();
                TripUnitType = breaker.TripDevice;
                AlternativeRCDTypes = breaker.RCDCharacteristics.Split(';').Select(o => o.GetEnumName<RCDType>()).ToList();
                AlternativeResidualCurrents = breaker.ResidualCurrent.Split(';').Select(o => (o + "mA").GetEnumName<ResidualCurrentSpecification>()).ToList();
                if (!AlternativeResidualCurrents.Contains(ResidualCurrent))
                {
                    if (IsMotor)
                    {
                        ResidualCurrent = ResidualCurrentSpecification.Specification300;
                    }
                    else
                    {
                        ResidualCurrent = ResidualCurrentSpecification.Specification30;
                    }
                }
            }
        }
        public override List<BreakerModel> GetModels()
        {
            return AlternativeModel;
        }

        /// <summary>
        /// 修改RCD类型
        /// </summary>
        /// <param name="type"></param>
        public void SetRCDType(RCDType type)
        {
            RCDType = type;
        }
        public List<RCDType> GetRCDTypes()
        {
            return AlternativeRCDTypes;
        }

        /// <summary>
        /// 修改剩余电流动作
        /// </summary>
        /// <param name="type"></param>
        public void SetResidualCurrent(ResidualCurrentSpecification type)
        {
            ResidualCurrent = type;
        }
        public List<ResidualCurrentSpecification> GetResidualCurrents()
        {
            return AlternativeResidualCurrents;
        }

        /// <summary>
        /// 瞬时脱扣器类型
        /// </summary>
        private string Characteristics { get; set; }
        private List<BreakerComponentInfo> Breakers { get; set; }

        /// <summary>
        /// 是否是发动机负载
        /// </summary>
        private bool IsMotor { get; }

        /// <summary>
        /// RCD类型
        /// </summary>
        public RCDType RCDType { get; set; }
        private List<RCDType> AlternativeRCDTypes { get; set; }

        /// <summary>
        /// 剩余电流动作
        /// </summary>
        public ResidualCurrentSpecification ResidualCurrent { get; set; }
        private List<ResidualCurrentSpecification> AlternativeResidualCurrents { get; set; }

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
