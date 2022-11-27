﻿using System;
using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.Module.Component.Extension;
using TianHua.Electrical.PDS.Project.PDSProjectException;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 断路器
    /// </summary>
    [CascadeComponent]
    [Serializable]
    public class Breaker : PDSBaseComponent
    {
        /// <summary>
        /// 断路器
        /// </summary>
        /// <param name="calculateCurrent">计算电流</param>
        /// <param name="tripDevice">脱扣器类型</param>
        /// <param name="polesNum">极数</param>
        /// <param name="characteristics">瞬时脱扣器类型</param>
        /// <param name="isDomesticWaterPump">是否是生活水泵</param>
        /// <param name="hasLeakageProtection">是否带漏电保护</param>
        public Breaker(double calculateCurrent, List<string> tripDevice, string polesNum, string characteristics, bool isDomesticWaterPump , bool hasLeakageProtection)
        {
            if (ProjectSystemConfiguration.SinglePhasePolesNum.Contains(polesNum))
            {
                AlternativePolesNum = ProjectSystemConfiguration.SinglePhasePolesNum;
            }
            else if (ProjectSystemConfiguration.ThreePhasePolesNum.Contains(polesNum))
            {
                AlternativePolesNum = ProjectSystemConfiguration.ThreePhasePolesNum;
            }
            CalculateCurrent = calculateCurrent;
            IsDomesticWaterPump = isDomesticWaterPump;
            IsSpecifiedSelection = false;
            this.TripDevices = tripDevice;
            List<BreakerComponentInfo> breakers;
            BreakerComponentInfo breaker;
            if (hasLeakageProtection)
            {
                ComponentType = ComponentType.一体式RCD;
                breakers = BreakerConfiguration.breakerComponentInfos.
                Where(o => o.Amps > calculateCurrent
                && o.Amps >= 16
                && tripDevice.Contains(o.TripDevice)
                && !o.ResidualCurrent.IsNullOrWhiteSpace()
                && AlternativePolesNum.Contains(o.Poles)
                && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(characteristics))).ToList();
                if (breakers.Count == 0)
                {
                    throw new NotFoundComponentException("设备库内找不到对应规格的Breaker");
                }
                breaker = breakers.FirstOrDefault(o => o.DefaultPick &&  o.Poles == polesNum);
                if(breaker.IsNull())
                {
                    breaker = breakers.FirstOrDefault(o => o.Poles == polesNum);
                    if (breaker.IsNull())
                    {
                        breaker = breakers.First();
                    }
                }
                //剩余电流断路器 的RCD类型默认为A，负载为发动机，剩余电流选300，其余选择30
                RCDType = RCDType.A;
                if (IsDomesticWaterPump)
                {
                    ResidualCurrent = ResidualCurrentSpecification.Specification300;
                }
                else
                {
                    ResidualCurrent = ResidualCurrentSpecification.Specification30;
                }
                AlternativeRCDTypes = breaker.RCDCharacteristics.Split(';').Select(o => o.GetEnumName<RCDType>()).ToList();
                AlternativeResidualCurrents = breaker.ResidualCurrent.Split(';').Select(o => (o + "mA").GetEnumName<ResidualCurrentSpecification>()).ToList();
            }
            else
            {
                ComponentType = ComponentType.CB;
                breakers = BreakerConfiguration.breakerComponentInfos.
                    Where(o => o.Amps > calculateCurrent
                    && o.Amps >= 16
                    && tripDevice.Contains(o.TripDevice)
                    && AlternativePolesNum.Contains(o.Poles)
                    && o.ResidualCurrent.IsNullOrWhiteSpace()
                    && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(characteristics))).ToList();
                if (breakers.Count == 0)
                {
                    throw new NotFoundComponentException("设备库内找不到对应规格的Breaker");
                }
                breaker = breakers.FirstOrDefault(o => o.DefaultPick &&  o.Poles == polesNum);
                if (breaker.IsNull())
                {
                    breaker = breakers.FirstOrDefault(o => o.Poles == polesNum);
                    if (breaker.IsNull())
                    {
                        breaker = breakers.First();
                    }
                }
            }
            Model = breaker.Model;
            FrameSpecification = breaker.FrameSize;
            PolesNum =breaker.Poles;
            RatedCurrent =breaker.Amps.ToString();
            TripUnitType =breaker.TripDevice;
            Icu = breaker.IcuConfig;

            Characteristics = characteristics;
            Breakers = breakers;
            AlternativeModel = breakers.Select(o => o.Model).Distinct().ToList();
            var model_breakers = Breakers.Where(o => o.Model == breaker.Model);
            AlternativeFrameSpecifications = model_breakers.Select(o => o.FrameSize).Distinct().ToList();
            AlternativeRatedCurrent = model_breakers.Select(o => o.Amps).Distinct().OrderBy(o => o).Select(o => o.ToString()).ToList();
            AlternativeTripDevice = model_breakers.Select(o => o.TripDevice).Distinct().ToList();
            AlternativePolesNum = model_breakers.Select(o => o.Poles).Distinct().ToList();
            AlternativeIcus = model_breakers.Select(o => o.IcuConfig).Distinct().ToList();
        }

        /// <summary>
        /// 断路器
        /// </summary>
        /// <param name="breakerConfig">指定断路器配置</param>
        public Breaker(string breakerConfig)
        {
            ComponentType = ComponentType.CB;

            IsSpecifiedSelection = true;
            //例：MCB63-MA2.5/3P
            string[] configs = breakerConfig.Split('-');
            string[] detaileds = configs[1].Split('/');
            var model = (BreakerModel)Enum.Parse(typeof(BreakerModel), Regex.Replace(configs[0], @"\d", ""));
            var frameSpecification = Regex.Replace(configs[0], @"\D", "");
            var polesNum = detaileds[1];
            int numIndex = detaileds[0].IndexOfAny(ProjectSystemConfiguration.NumberArray);
            var ratedCurrent = detaileds[0].Substring(numIndex);
            var tripUnitType = detaileds[0].Substring(0, numIndex);

            var breakers = BreakerConfiguration.breakerComponentInfos.
                Where(o => o.Model == model
                && o.FrameSize == frameSpecification
                && o.Poles == polesNum
                && o.TripDevice == tripUnitType
                && o.Amps.ToString() == ratedCurrent
                && o.ResidualCurrent.IsNullOrWhiteSpace()).Take(1).ToList();
            if (breakers.Count == 0)
            {
                throw new NotFoundComponentException("设备库内找不到对应规格的Breaker");
            }
            var breaker = breakers.First();
            Model = breaker.Model;
            FrameSpecification = breaker.FrameSize;
            PolesNum =breaker.Poles;
            RatedCurrent =breaker.Amps.ToString();
            TripUnitType =breaker.TripDevice;
            Icu = breaker.IcuConfig;

            Characteristics = "";
            Breakers = breakers;
            AlternativeModel = breakers.Select(o => o.Model).Distinct().ToList();
            AlternativeFrameSpecifications = breakers.Select(o => o.FrameSize).Distinct().ToList();
            AlternativeRatedCurrent = breakers.Select(o => o.Amps).Distinct().OrderBy(o => o).Select(o => o.ToString()).ToList();
            AlternativeTripDevice = new List<string>() { TripUnitType };
            AlternativePolesNum = new List<string>() { PolesNum };
            AlternativeIcus = breakers.Select(o => o.IcuConfig).Distinct().ToList();
        }

        public Breaker(List<string> breakerConfig, List<string> tripDevice, string characteristics)
        {
            ComponentType = ComponentType.CB;

            IsSpecifiedSelection = true;
            List<Tuple<double, string>> configs = new List<Tuple<double, string>>();
            var appendix = AppendixType.无;
            this.TripDevices = tripDevice;
            breakerConfig.ForEach(o =>
            {
                //例：80A/2P/ST
                string[] detaileds = o.Split('/');
                configs.Add((double.Parse(detaileds[0].Replace("A", "")), detaileds[1]).ToTuple());
                if (detaileds.Length == 3)
                {
                    appendix = (AppendixType)Enum.Parse(typeof(AppendixType), detaileds[2]);
                }
            });
            BreakerComponentInfo breaker;
            var breakers = BreakerConfiguration.breakerComponentInfos.Where(o =>
                configs.Any(config => config.Item1 == o.Amps && config.Item2 == o.Poles)
                && tripDevice.Contains(o.TripDevice)
                && o.ResidualCurrent.IsNullOrWhiteSpace()
                && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(characteristics))).ToList();
            if (breakers.Count == 0)
            {
                throw new NotFoundComponentException("设备库内找不到对应规格的Breaker");
            }
            breaker = breakers.First();
            Model = breaker.Model;
            FrameSpecification = breaker.FrameSize;
            PolesNum =breaker.Poles;
            RatedCurrent =breaker.Amps.ToString();
            TripUnitType =breaker.TripDevice;
            Icu = breaker.IcuConfig;

            Characteristics = characteristics;
            Breakers = breakers;
            AlternativeModel = breakers.Select(o => o.Model).Distinct().ToList();
            var model_breakers = Breakers.Where(o => o.Model == breaker.Model);
            AlternativeFrameSpecifications = model_breakers.Select(o => o.FrameSize).Distinct().ToList();
            AlternativeRatedCurrent = model_breakers.Select(o => o.Amps).Distinct().OrderBy(o => o).Select(o => o.ToString()).ToList();
            AlternativeIcus = model_breakers.Select(o => o.IcuConfig).Distinct().ToList();
            AlternativeTripDevice = model_breakers.Select(o => o.TripDevice).Distinct().ToList();
            AlternativePolesNum = model_breakers.Select(o => o.Poles).Distinct().ToList();
            switch(appendix)
            {
                case AppendixType.ST:
                {
                        STAppendix = true;
                        break;
                }
                case AppendixType.AL:
                    {
                        ALAppendix = true;
                        break;
                    }
                case AppendixType.AX:
                    {
                        AXAppendix = true;
                        break;
                    }
                case AppendixType.RC:
                    {
                        RCDAppendix = true;
                        break;
                    }
                case AppendixType.UR:
                    {
                        URAppendix = true;
                        break;
                    }
                default:
                    break;
            }
        }

        private BreakerModel _model;

        /// <summary>
        /// 型号
        /// </summary>
        public BreakerModel Model
        {
            get
            {
                return _model;
            }
            set
            {
                _model = value;
                BreakerModelUpdate(value);
            }
        }

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
        /// 分断能力
        /// </summary>
        public string Icu { get; set; }

        /// <summary>
        /// RCD附件
        /// </summary>
        public bool? RCDAppendix { get; set; } = null;

        /// <summary>
        /// 分励脱扣附件
        /// </summary>
        public bool? STAppendix { get; set; } = null;

        /// <summary>
        /// 报警附件
        /// </summary>
        public bool? ALAppendix { get; set; } = null;

        /// <summary>
        /// 失压脱扣附件
        /// </summary>
        public bool? URAppendix { get; set; } = null;

        /// <summary>
        /// 辅助触电附件
        /// </summary>
        public bool? AXAppendix { get; set; } = null;

        /// <summary>
        /// RCD类型(仅RCD可见)
        /// </summary>
        public RCDType RCDType { get; set; }

        /// <summary>
        /// 剩余电流动作(仅RCD可见)
        /// </summary>
        public ResidualCurrentSpecification ResidualCurrent { get; set; }

        public void SetBreakerType(ComponentType componentType)
        {
            if (this.ComponentType == componentType)
            {
                return;
            }
            if (this.ComponentType == ComponentType.CB && componentType == ComponentType.组合式RCD)
            {
                this.ComponentType = ComponentType.组合式RCD;
                RCDAppendix = true;
                //剩余电流断路器 的RCD类型默认为A，负载为发动机，剩余电流选300，其余选择30
                RCDType = RCDType.A;
                if (IsDomesticWaterPump)
                {
                    ResidualCurrent = ResidualCurrentSpecification.Specification300;
                }
                else
                {
                    ResidualCurrent = ResidualCurrentSpecification.Specification30;
                }
                AlternativeRCDTypes = new List<RCDType>() { RCDType.A, RCDType.AC, RCDType.B, RCDType.F };
                AlternativeResidualCurrents = new List<ResidualCurrentSpecification>() { ResidualCurrentSpecification.Specification10, ResidualCurrentSpecification.Specification30, ResidualCurrentSpecification.Specification100, ResidualCurrentSpecification.Specification300, ResidualCurrentSpecification.Specification500 };
            }
            else if (this.ComponentType == ComponentType.组合式RCD && componentType == ComponentType.CB)
            {
                this.ComponentType = ComponentType.CB;
                RCDAppendix = false;
            }
            else
            {
                if (!IsSpecifiedSelection)
                {
                    List<BreakerComponentInfo> breakers;
                    BreakerComponentInfo breaker;
                    if (componentType == ComponentType.一体式RCD)
                    {
                        ComponentType = ComponentType.一体式RCD;
                        breakers = BreakerConfiguration.breakerComponentInfos.
                        Where(o => o.Amps > CalculateCurrent
                        && o.Amps >= 16
                        && TripDevices.Contains(o.TripDevice)
                        && !o.ResidualCurrent.IsNullOrWhiteSpace()
                        && AlternativePolesNum.Contains(o.Poles)
                        && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))).ToList();
                        if (breakers.Count == 0)
                        {
                            throw new NotFoundComponentException("设备库内找不到对应规格的Breaker");
                        }
                        breaker = breakers.FirstOrDefault(o => o.DefaultPick &&  o.Poles == PolesNum);
                        if (breaker.IsNull())
                        {
                            breaker = breakers.FirstOrDefault(o => o.Poles == PolesNum);
                            if (breaker.IsNull())
                            {
                                breaker = breakers.First();
                            }
                        }
                        //剩余电流断路器 的RCD类型默认为A，负载为发动机，剩余电流选300，其余选择30
                        RCDType = RCDType.A;
                        if (IsDomesticWaterPump)
                        {
                            ResidualCurrent = ResidualCurrentSpecification.Specification300;
                        }
                        else
                        {
                            ResidualCurrent = ResidualCurrentSpecification.Specification30;
                        }
                        AlternativeRCDTypes = breaker.RCDCharacteristics.Split(';').Select(o => o.GetEnumName<RCDType>()).ToList();
                        AlternativeResidualCurrents = breaker.ResidualCurrent.Split(';').Select(o => (o + "mA").GetEnumName<ResidualCurrentSpecification>()).ToList();
                    }
                    else
                    {
                        breakers = BreakerConfiguration.breakerComponentInfos.
                            Where(o => o.Amps > CalculateCurrent
                            && o.Amps >= 16
                            && TripDevices.Contains(o.TripDevice)
                            && AlternativePolesNum.Contains(o.Poles)
                            && o.ResidualCurrent.IsNullOrWhiteSpace()
                            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))).ToList();
                        if (breakers.Count == 0)
                        {
                            throw new NotFoundComponentException("设备库内找不到对应规格的Breaker");
                        }
                        breaker = breakers.FirstOrDefault(o => o.DefaultPick &&  o.Poles == PolesNum);
                        if (breaker.IsNull())
                        {
                            breaker = breakers.FirstOrDefault(o => o.Poles == PolesNum);
                            if (breaker.IsNull())
                            {
                                breaker = breakers.First();
                            }
                        }
                        if (componentType == ComponentType.CB)
                        {
                            RCDAppendix = false;
                            ComponentType = ComponentType.CB;
                        }
                        else
                        {
                            ComponentType = ComponentType.组合式RCD;
                            RCDAppendix = true;
                            //剩余电流断路器 的RCD类型默认为A，负载为发动机，剩余电流选300，其余选择30
                            RCDType = RCDType.A;
                            if (IsDomesticWaterPump)
                            {
                                ResidualCurrent = ResidualCurrentSpecification.Specification300;
                            }
                            else
                            {
                                ResidualCurrent = ResidualCurrentSpecification.Specification30;
                            }
                            AlternativeRCDTypes = new List<RCDType>() { RCDType.A,RCDType.AC,RCDType.B,RCDType.F};
                            AlternativeResidualCurrents = new List<ResidualCurrentSpecification>() { ResidualCurrentSpecification.Specification10,ResidualCurrentSpecification.Specification30,ResidualCurrentSpecification.Specification100,ResidualCurrentSpecification.Specification300,ResidualCurrentSpecification.Specification500};
                        }
                    }
                    Model = breaker.Model;
                    FrameSpecification = breaker.FrameSize;
                    PolesNum =breaker.Poles;
                    RatedCurrent =breaker.Amps.ToString();
                    TripUnitType =breaker.TripDevice;

                    Characteristics = Characteristics;
                    Breakers = breakers;
                    AlternativeModel = breakers.Select(o => o.Model).Distinct().ToList();
                    AlternativeFrameSpecifications = breakers.Select(o => o.FrameSize).Distinct().ToList();
                    AlternativeRatedCurrent = breakers.Select(o => o.Amps).Distinct().OrderBy(o => o).Select(o => o.ToString()).ToList();
                    AlternativeTripDevice = breakers.Select(o => o.TripDevice).Distinct().ToList();
                    AlternativePolesNum = breakers.Select(o => o.Poles).Distinct().ToList();
                }
            }
        }

        /// <summary>
        /// 修改级数
        /// </summary>
        /// <param name="polesNum"></param>
        public void SetPolesNum(string polesNum)
        {
            if (Breakers.Any(o => o.Poles == polesNum
            && o.Model == Model
            && o.FrameSize == FrameSpecification
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType
            && o.Amps == double.Parse(RatedCurrent)
            && o.IcuConfig == Icu))
            {
                this.PolesNum = polesNum;
            }
            else
            {
                var breaker = Breakers.Where(o => o.Model == Model).First(o => o.Poles == polesNum);
                FrameSpecification = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
                Icu = breaker.IcuConfig;
                if (this.ComponentType == ComponentType.一体式RCD)
                {
                    AlternativeRCDTypes = breaker.RCDCharacteristics.Split(';').Select(o => o.GetEnumName<RCDType>()).ToList();
                    AlternativeResidualCurrents = breaker.ResidualCurrent.Split(';').Select(o => (o + "mA").GetEnumName<ResidualCurrentSpecification>()).ToList();
                    if (!AlternativeResidualCurrents.Contains(ResidualCurrent))
                    {
                        if (IsDomesticWaterPump)
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
        }
        public List<string> GetPolesNums()
        {
            return AlternativePolesNum;
        }

        /// <summary>
        /// 修改脱扣器类型
        /// </summary>
        /// <param name="tripDevice"></param>
        public void SetTripDevice(string tripDevice)
        {
            if (Breakers.Any(o => o.Poles ==  PolesNum
            && o.Model == Model
            && o.FrameSize == FrameSpecification
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == tripDevice
            && o.Amps == double.Parse(RatedCurrent)
            && o.IcuConfig == Icu))
            {
                this.TripUnitType = tripDevice;
            }
            else
            {
                var breaker = Breakers.Where(o => o.Model == Model).First(o => o.TripDevice == tripDevice);
                FrameSpecification = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
                Icu = breaker.IcuConfig;
                if (this.ComponentType == ComponentType.一体式RCD)
                {
                    AlternativeRCDTypes = breaker.RCDCharacteristics.Split(';').Select(o => o.GetEnumName<RCDType>()).ToList();
                    AlternativeResidualCurrents = breaker.ResidualCurrent.Split(';').Select(o => (o + "mA").GetEnumName<ResidualCurrentSpecification>()).ToList();
                    if (!AlternativeResidualCurrents.Contains(ResidualCurrent))
                    {
                        if (IsDomesticWaterPump)
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
        }
        public List<string> GetTripDevices()
        {
            return AlternativeTripDevice;
        }

        /// <summary>
        /// 修改额定电流
        /// </summary>
        /// <param name="ratedCurrentStr"></param>
        public void SetRatedCurrent(string ratedCurrentStr)
        {
            var ratedCurrent = double.Parse(ratedCurrentStr);
            if (Breakers.Any(o => o.Poles ==  PolesNum
            && o.Model == Model
            && o.FrameSize == FrameSpecification
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType
            && o.Amps == ratedCurrent
            && o.IcuConfig == Icu))
            {
                this.RatedCurrent = ratedCurrentStr;
            }
            else
            {
                var breaker = Breakers.Where(o => o.Model == Model).First(o => o.Amps == ratedCurrent);
                FrameSpecification = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
                Icu = breaker.IcuConfig;
                if (this.ComponentType == ComponentType.一体式RCD)
                {
                    AlternativeRCDTypes = breaker.RCDCharacteristics.Split(';').Select(o => o.GetEnumName<RCDType>()).ToList();
                    AlternativeResidualCurrents = breaker.ResidualCurrent.Split(';').Select(o => (o + "mA").GetEnumName<ResidualCurrentSpecification>()).ToList();
                    if (!AlternativeResidualCurrents.Contains(ResidualCurrent))
                    {
                        if (IsDomesticWaterPump)
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
        }

        /// <summary>
        /// 修改放大倍数
        /// </summary>
        /// <param name="ratedCurrentStr"></param>
        public void SetMagnificationRatedCurrent(double calculateCurrentMagnification)
        {
            var breaker = Breakers.FirstOrDefault(o => o.Poles == PolesNum
            && o.FrameSize == FrameSpecification
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType
            && o.Amps >= calculateCurrentMagnification
            && o.IcuConfig == Icu);
            if(breaker != null)
            {
                SetModel(breaker.Model);
                SetRatedCurrent(breaker.Amps.ToString());
            }
            else
            {
                SetRatedCurrent(AlternativeRatedCurrent.Last());
            }
        }

        public List<string> GetRatedCurrents()
        {
            return AlternativeRatedCurrent;
        }

        /// <summary>
        /// 修改壳架规格
        /// </summary>
        /// <param name="ratedCurrentStr"></param>
        public void SetFrameSpecification(string frameSpecifications)
        {
            if (Breakers.Any(o => o.Poles ==  PolesNum
            && o.Model == Model
            && o.FrameSize == frameSpecifications
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType
            && o.Amps == double.Parse(RatedCurrent)
            && o.IcuConfig == Icu))
            {
                this.FrameSpecification = frameSpecifications;
            }
            else
            {
                var breaker = Breakers.Where(o => o.Model == Model).First(o => o.FrameSize == frameSpecifications);
                FrameSpecification = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
                Icu = breaker.IcuConfig;
                if (this.ComponentType == ComponentType.一体式RCD)
                {
                    AlternativeRCDTypes = breaker.RCDCharacteristics.Split(';').Select(o => o.GetEnumName<RCDType>()).ToList();
                    AlternativeResidualCurrents = breaker.ResidualCurrent.Split(';').Select(o => (o + "mA").GetEnumName<ResidualCurrentSpecification>()).ToList();
                    if (!AlternativeResidualCurrents.Contains(ResidualCurrent))
                    {
                        if (IsDomesticWaterPump)
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
        }
        public List<string> GetFrameSpecifications()
        {
            return AlternativeFrameSpecifications;
        }

        /// <summary>
        /// 修改型号
        /// </summary>
        /// <param name="ratedCurrentStr"></param>
        public void SetModel(BreakerModel model)
        {
            if (Breakers.Any(o => o.Poles ==  PolesNum
            && o.Model == model
            && o.FrameSize == FrameSpecification
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType
            && o.Amps == double.Parse(RatedCurrent)
            && o.IcuConfig == Icu))
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
                Icu = breaker.IcuConfig;
                var model_breakers = Breakers.Where(o => o.Model == breaker.Model);
                AlternativeFrameSpecifications = model_breakers.Select(o => o.FrameSize).Distinct().ToList();
                AlternativeRatedCurrent = model_breakers.Select(o => o.Amps).Distinct().OrderBy(o => o).Select(o => o.ToString()).ToList();
                AlternativeTripDevice = model_breakers.Select(o => o.TripDevice).Distinct().ToList();
                AlternativePolesNum = model_breakers.Select(o => o.Poles).Distinct().ToList();
                AlternativeIcus = model_breakers.Select(o => o.IcuConfig).Distinct().ToList();
                if (this.ComponentType == ComponentType.一体式RCD)
                {
                    AlternativeRCDTypes = breaker.RCDCharacteristics.Split(';').Select(o => o.GetEnumName<RCDType>()).ToList();
                    AlternativeResidualCurrents = breaker.ResidualCurrent.Split(';').Select(o => (o + "mA").GetEnumName<ResidualCurrentSpecification>()).ToList();
                    if (!AlternativeResidualCurrents.Contains(ResidualCurrent))
                    {
                        if (IsDomesticWaterPump)
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
        }
        public List<BreakerModel> GetModels()
        {
            return AlternativeModel;
        }

        private void BreakerModelUpdate(BreakerModel model)
        {
            switch (model)
            {
                case BreakerModel.MCB:
                    {
                        if (!RCDAppendix.HasValue)
                            RCDAppendix = false;
                        if (!STAppendix.HasValue)
                            STAppendix = false;
                        if (!AXAppendix.HasValue)
                            AXAppendix = false;
                        if (!ALAppendix.HasValue)
                            ALAppendix = false;
                        URAppendix = null;
                        break;
                    }
                case BreakerModel.MCCB:
                    {
                        if (!RCDAppendix.HasValue)
                            RCDAppendix = false;
                        if (!STAppendix.HasValue)
                            STAppendix = false;
                        if (!AXAppendix.HasValue)
                            AXAppendix = false;
                        if (!URAppendix.HasValue)
                            URAppendix = false;
                        if (!ALAppendix.HasValue)
                            ALAppendix = false;
                        break;
                    }
                case BreakerModel.RCBO:
                    {
                        if (!STAppendix.HasValue)
                            STAppendix = false;
                        if (!AXAppendix.HasValue)
                            AXAppendix = false;
                        if (!URAppendix.HasValue)
                            URAppendix = false;
                        if (!ALAppendix.HasValue)
                            ALAppendix = false;
                        RCDAppendix = null;
                        break;
                    }
                case BreakerModel.RCCB:
                    {
                        if (!STAppendix.HasValue)
                            STAppendix = false;
                        if (!AXAppendix.HasValue)
                            AXAppendix = false;
                        if (!URAppendix.HasValue)
                            URAppendix = false;
                        if (!ALAppendix.HasValue)
                            ALAppendix = false;
                        RCDAppendix = null;
                        break;
                    }
                case BreakerModel.ACB:
                    {
                        if (!STAppendix.HasValue)
                            STAppendix = false;
                        if (!AXAppendix.HasValue)
                            AXAppendix = false;
                        if (!URAppendix.HasValue)
                            URAppendix = false;
                        if (!ALAppendix.HasValue)
                            ALAppendix = false;
                        RCDAppendix = null;
                        break;
                    }
            }
        }

        public bool GetRCDAppendix()
        {
            return RCDAppendix ?? false;
        }

        public void SetRCDAppendix(bool value)
        {
            if (RCDAppendix != value)
            {
                RCDAppendix = value;
                SetBreakerType(value ? ComponentType.组合式RCD : ComponentType.CB);
            }
        }

        public void SetIcu(string icu)
        {
            if (Breakers.Any(o => o.Poles ==  PolesNum
            && o.Model == Model
            && o.FrameSize == FrameSpecification
            && (o.Characteristics.IsNullOrWhiteSpace() || o.Characteristics.Contains(Characteristics))
            && o.TripDevice == TripUnitType
            && o.Amps == double.Parse(RatedCurrent)
            && o.IcuConfig == icu))
            {
                this.Icu = icu;
            }
            else
            {
                var breaker = Breakers.Where(o => o.Model == Model).First(o => o.IcuConfig == icu);
                FrameSpecification = breaker.FrameSize;
                PolesNum =breaker.Poles;
                RatedCurrent =breaker.Amps.ToString();
                TripUnitType =breaker.TripDevice;
                Icu = breaker.IcuConfig;
                if (this.ComponentType == ComponentType.一体式RCD)
                {
                    AlternativeRCDTypes = breaker.RCDCharacteristics.Split(';').Select(o => o.GetEnumName<RCDType>()).ToList();
                    AlternativeResidualCurrents = breaker.ResidualCurrent.Split(';').Select(o => (o + "mA").GetEnumName<ResidualCurrentSpecification>()).ToList();
                    if (!AlternativeResidualCurrents.Contains(ResidualCurrent))
                    {
                        if (IsDomesticWaterPump)
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
        }

        /// <summary>
        /// 获取分段能力下拉框选值
        /// </summary>
        public List<string> GetIcus()
        {
            return AlternativeIcus;
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
        public string Characteristics { get; set; }

        /// <summary>
        /// 断路器信息
        /// </summary>
        private List<BreakerComponentInfo> Breakers { get; set; }

        /// <summary>
        /// 额定电流下拉框选择项
        /// </summary>
        private List<string> AlternativeRatedCurrent { get; set; }

        /// <summary>
        /// 级数下拉框选择项
        /// </summary>
        private List<string> AlternativePolesNum { get; set; }

        /// <summary>
        /// 脱扣器类型下拉框选择项
        /// </summary>
        private List<string> AlternativeTripDevice { get; set; }
        
        /// <summary>
        /// 脱扣器类型
        /// </summary>
        private List<string> TripDevices { get; set; }

        /// <summary>
        /// 壳架规格下拉框选择项
        /// </summary>
        private List<string> AlternativeFrameSpecifications { get; set; }

        /// <summary>
        /// 分段能力下拉框选择项
        /// </summary>
        private List<string> AlternativeIcus { get; set; }

        /// <summary>
        /// 模型下拉框选择项
        /// </summary>
        private List<BreakerModel> AlternativeModel { get; set; }

        /// <summary>
        /// 是否是生活水泵
        /// </summary>
        private bool IsDomesticWaterPump { get; set; }

        /// <summary>
        /// 是否是指定选型
        /// </summary>
        private bool IsSpecifiedSelection { get; set; }

        /// <summary>
        /// 计算电流
        /// </summary>
        private double CalculateCurrent { get; set; }

        private List<RCDType> AlternativeRCDTypes { get; set; }
        private List<ResidualCurrentSpecification> AlternativeResidualCurrents { get; set; }
    }
}
