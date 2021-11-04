using System;
using System.Windows.Media;
using System.Collections.Generic;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPHVAC.LoadCalculation.Model
{
    [Serializable]
    public class ConfigDataModel
    {
        public List<DynamicLoadCalculationModelData> Configs;
        public ConfigDataModel()
        {
            Configs = new List<DynamicLoadCalculationModelData>();
        }
    }
    [Serializable]
    public class DynamicLoadCalculationModelData : IComparable<DynamicLoadCalculationModelData>
    {
        /// <summary>
        /// 房间功能-column1-string
        /// </summary>
        public string RoomFunction { get; set; }
        /// <summary>
        /// 冷指标-column2-class
        /// </summary>
        public NormClass ColdNorm { get; set; }

        /// <summary>
        /// 冷水温差-column3-double
        /// </summary>
        public double? CWaterTemperature { get; set; }

        /// <summary>
        /// 热指标-column4-class
        /// </summary>
        public NormClass HotNorm { get; set; }

        /// <summary>
        /// 热水温差-column5-double
        /// </summary>
        public double? HWaterTemperature { get; set; }

        /// <summary>
        /// 新风量-column6&7-class
        /// </summary>
        public ReshAirVolume ReshAir { get; set; }

        /// <summary>
        /// 排油烟-column8-class
        /// </summary>
        public LampblackClass Lampblack { get; set; }

        /// <summary>
        /// 油烟补风-column9-class
        /// </summary>
        public NormClass LampblackAir { get; set; }

        /// <summary>
        /// 事故排风-column10-class
        /// </summary>
        public LampblackClass AccidentAir { get; set; }

        /// <summary>
        /// 平时排风-column11-class
        /// </summary>
        public UsuallyExhaust Exhaust { get; set; }

        /// <summary>
        /// 平时补风-column12-class
        /// </summary>
        public UsuallyAirCompensation AirCompensation { get; set; }

        public int CompareTo(DynamicLoadCalculationModelData other)
        {
            bool IsCompare =
                this.RoomFunction.Equals(other.RoomFunction)

                && this.ColdNorm.ByNorm.Equals(other.ColdNorm.ByNorm)
                && this.ColdNorm.NormValue.Equals(other.ColdNorm.NormValue)
                && this.ColdNorm.TotalValue.Equals(other.ColdNorm.TotalValue)

                && this.CWaterTemperature.Equals(other.CWaterTemperature)

                && this.HotNorm.ByNorm.Equals(other.HotNorm.ByNorm)
                && this.HotNorm.NormValue.Equals(other.HotNorm.NormValue)
                && this.HotNorm.TotalValue.Equals(other.HotNorm.TotalValue)

                && this.HWaterTemperature.Equals(other.HWaterTemperature)

                && this.ReshAir.ByNorm.Equals(other.ReshAir.ByNorm)
                && this.ReshAir.PersonnelDensity.Equals(other.ReshAir.PersonnelDensity)
                && this.ReshAir.ReshAirNormValue.Equals(other.ReshAir.ReshAirNormValue)
                && this.ReshAir.TotalValue.Equals(other.ReshAir.TotalValue)

                && this.Lampblack.ByNorm.Equals(other.Lampblack.ByNorm)
                && this.Lampblack.Proportion.Equals(other.Lampblack.Proportion)
                && this.Lampblack.AirNum.Equals(other.Lampblack.AirNum)
                && this.Lampblack.TotalValue.Equals(other.Lampblack.TotalValue)

                && this.LampblackAir.ByNorm.Equals(other.LampblackAir.ByNorm)
                && this.LampblackAir.NormValue.Equals(other.LampblackAir.NormValue)
                && this.LampblackAir.TotalValue.Equals(other.LampblackAir.TotalValue)

                && this.AccidentAir.ByNorm.Equals(other.AccidentAir.ByNorm)
                && this.AccidentAir.Proportion.Equals(other.AccidentAir.Proportion)
                && this.AccidentAir.AirNum.Equals(other.AccidentAir.AirNum)
                && this.AccidentAir.TotalValue.Equals(other.AccidentAir.TotalValue)

                && this.Exhaust.ByNorm.Equals(other.Exhaust.ByNorm)
                && this.Exhaust.NormValue.Equals(other.Exhaust.NormValue)
                && this.Exhaust.TotalValue.Equals(other.Exhaust.TotalValue)
                && this.Exhaust.BreatheNum.Equals(other.Exhaust.BreatheNum)
                && this.Exhaust.CapacityType.Equals(other.Exhaust.CapacityType)
                && this.Exhaust.TransformerCapacity.Equals(other.Exhaust.TransformerCapacity)
                && this.Exhaust.BoilerCapacity.Equals(other.Exhaust.BoilerCapacity)
                && this.Exhaust.FirewoodCapacity.Equals(other.Exhaust.FirewoodCapacity)
                && this.Exhaust.HeatDissipation.Equals(other.Exhaust.HeatDissipation)
                && this.Exhaust.RoomTemperature.Equals(other.Exhaust.RoomTemperature)

                && this.AirCompensation.ByNorm.Equals(other.AirCompensation.ByNorm)
                && this.AirCompensation.NormValue.Equals(other.AirCompensation.NormValue)
                && this.AirCompensation.TotalValue.Equals(other.AirCompensation.TotalValue)
                && this.AirCompensation.CapacityType.Equals(other.AirCompensation.CapacityType)
                && this.AirCompensation.BoilerCapacity.Equals(other.AirCompensation.BoilerCapacity)
                && this.AirCompensation.FirewoodCapacity.Equals(other.AirCompensation.FirewoodCapacity)
                && this.AirCompensation.CombustionAirVolume.Equals(other.AirCompensation.CombustionAirVolume)
                ;
            if (IsCompare)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
    }

    /// <summary>
    /// 指标类
    /// </summary>
    [Serializable]
    public class NormClass : NotifyPropertyChangedBase
    {
        private bool byNorm;
        /// <summary>
        /// 是否按指标计算（Y-按指标 / N-按总量）
        /// </summary>
        public bool ByNorm
        {
            get
            {
                return byNorm;
            }
            set
            {
                byNorm = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged("GetTrueValue");
                this.RaisePropertyChanged("GetTrueColor");
            }
        }

        private double? normValue;
        /// <summary>
        /// 指标量
        /// </summary>
        public double? NormValue
        {
            get
            {
                return normValue;
            }
            set
            {
                normValue = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged("GetTrueValue");
                this.RaisePropertyChanged("GetTrueColor");
            }
        }

        private double totalValue;
        /// <summary>
        /// 总量
        /// </summary>
        public double TotalValue
        {
            get
            {
                return totalValue;
            }
            set
            {
                totalValue = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged("GetTrueValue");
                this.RaisePropertyChanged("GetTrueColor");
            }
        }

        public double? GetTrueValue
        {
            get
            {
                if (ByNorm)
                    return NormValue;
                else
                    return TotalValue;
            }
            set
            {
                ByNorm = true;
                NormValue = value;
            }
        }

        public SolidColorBrush GetTrueColor
        {
            get
            {
                if (ByNorm)
                    return Brushes.White;
                else
                    return Brushes.Pink;
            }
        }
    }

    /// <summary>
    /// 新风量
    /// </summary>
    [Serializable]
    public class ReshAirVolume : NotifyPropertyChangedBase
    {
        private bool byNorm;
        /// <summary>
        /// 是否按指标计算（Y-按指标 / N-按总量）
        /// </summary>
        public bool ByNorm
        {
            get
            {
                return byNorm;
            }
            set
            {
                byNorm = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged("GetPersonnelDensity");
                this.RaisePropertyChanged("GetTrueValue");
                this.RaisePropertyChanged("GetTrueColor");
            }
        }

        private double? personnelDensity;
        /// <summary>
        /// 人员密度
        /// </summary>
        public double? PersonnelDensity
        {
            get
            {
                return personnelDensity;
            }
            set
            {
                personnelDensity = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged("GetPersonnelDensity");
                this.RaisePropertyChanged("GetTrueValue");
                this.RaisePropertyChanged("GetTrueColor");
            }
        }

        private double? reshAirNormValue;
        /// <summary>
        /// 新风指标
        /// </summary>
        public double? ReshAirNormValue
        {
            get
            {
                return reshAirNormValue;
            }
            set
            {
                reshAirNormValue = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged("GetPersonnelDensity");
                this.RaisePropertyChanged("GetTrueValue");
                this.RaisePropertyChanged("GetTrueColor");
            }
        }

        private double totalValue;
        /// <summary>
        /// 总量
        /// </summary>
        public double TotalValue
        {
            get
            {
                return totalValue;
            }
            set
            {
                totalValue = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged("GetPersonnelDensity");
                this.RaisePropertyChanged("GetTrueValue");
                this.RaisePropertyChanged("GetTrueColor");
            }
        }

        public string GetPersonnelDensity
        {
            get
            {
                if (ByNorm)
                    return PersonnelDensity.ToString();
                else
                    return "";
            }
            set
            {
                ByNorm = true;
                PersonnelDensity = double.Parse(value);
            }
        }

        public double? GetTrueValue
        {
            get
            {
                if (ByNorm)
                    return ReshAirNormValue;
                else
                    return TotalValue;
            }
            set
            {
                ByNorm = true;
                ReshAirNormValue = value;
            }
        }
        public SolidColorBrush GetTrueColor
        {
            get
            {
                if (ByNorm)
                    return Brushes.White;
                else
                    return Brushes.Pink;
            }
        }
    }

    /// <summary>
    /// 油烟类
    /// </summary>
    [Serializable]
    public class LampblackClass : NotifyPropertyChangedBase
    {
        private bool byNorm;
        /// <summary>
        /// 是否按指标计算（Y-按指标 / N-按总量）
        /// </summary>
        public bool ByNorm
        {
            get
            {
                return byNorm;
            }
            set
            {
                byNorm = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged("GetTrueValue");
                this.RaisePropertyChanged("GetTrueColor");
            }
        }

        private double proportion;
        /// <summary>
        /// 占比
        /// </summary>
        public double Proportion
        {
            get
            {
                return proportion;
            }
            set
            {
                proportion = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged("GetTrueValue");
                this.RaisePropertyChanged("GetTrueColor");
            }
        }

        private double? airNum;
        /// <summary>
        /// 换气次数
        /// </summary>
        public double? AirNum
        {
            get
            {
                return airNum;
            }
            set
            {
                airNum = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged("GetTrueValue");
                this.RaisePropertyChanged("GetTrueColor");
            }
        }

        private double? totalValue;
        /// <summary>
        /// 总量
        /// </summary>
        public double? TotalValue
        {
            get
            {
                return totalValue;
            }
            set
            {
                totalValue = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged("GetTrueValue");
                this.RaisePropertyChanged("GetTrueColor");
            }
        }

        public double? GetTrueValue
        {
            get
            {
                if (ByNorm)
                    return AirNum;
                else
                    return TotalValue;
            }
            set
            {
                ByNorm = true;
                AirNum = value;
            }
        }

        public SolidColorBrush GetTrueColor
        {
            get
            {
                if (ByNorm)
                    return Brushes.White;
                else
                    return Brushes.Pink;
            }
        }
    }

    /// <summary>
    /// 平时排风类
    /// </summary>
    [Serializable]
    public class UsuallyExhaust : NotifyPropertyChangedBase
    {
        private double byNorm;
        /// <summary>
        /// 是否按指标计算（1-按指标 / 2-按总量 / 3-按热平衡计算）
        /// </summary>
        public double ByNorm
        {
            get
            {
                return byNorm;
            }
            set
            {
                byNorm = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged("GetTrueValue");
                this.RaisePropertyChanged("GetTrueColor");
            }
        }

        private double? normValue;
        /// <summary>
        /// 换气次数
        /// </summary>
        public double? NormValue
        {
            get
            {
                return normValue;
            }
            set
            {
                normValue = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged("GetTrueValue");
                this.RaisePropertyChanged("GetTrueColor");
            }
        }

        private double? totalValue;
        /// <summary>
        /// 总量
        /// </summary>
        public double? TotalValue
        {
            get
            {
                return totalValue;
            }
            set
            {
                totalValue = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged("GetTrueValue");
                this.RaisePropertyChanged("GetTrueColor");
            }
        }

        public string GetTrueValue
        {
            get
            {
                if (ByNorm == 1)
                    return NormValue.ToString();
                else if (ByNorm == 2)
                    return TotalValue.ToString();
                else
                    return "热平衡";
            }
            set
            {
                ByNorm = 1;
                if (string.IsNullOrEmpty(value))
                    NormValue = null;
                else
                    NormValue = double.Parse(value);
            }
        }

        public SolidColorBrush GetTrueColor
        {
            get
            {
                if (ByNorm == 1)
                    return Brushes.White;
                else if (ByNorm == 2)
                    return Brushes.Pink;
                else
                    return Brushes.Moccasin;
            }
        }

        /// <summary>
        /// 换气次数要求
        /// </summary>
        public double BreatheNum { get; set; }

        public double CapacityType { get; set; } = 1;

        /// <summary>
        /// 变压器容量
        /// </summary>
        public double TransformerCapacity { get; set; }

        /// <summary>
        /// 锅炉容量
        /// </summary>
        public double BoilerCapacity { get; set; }

        /// <summary>
        /// 柴发容量
        /// </summary>
        public double FirewoodCapacity { get; set; }

        /// <summary>
        /// 散热系数
        /// </summary>
        public double HeatDissipation { get; set; }

        /// <summary>
        /// 室内温度
        /// </summary>
        public double RoomTemperature { get; set; }
    }

    /// <summary>
    /// 平时补风类
    /// </summary>
    [Serializable]
    public class UsuallyAirCompensation : NotifyPropertyChangedBase
    {
        private double byNorm;
        /// <summary>
        /// 是否按指标计算（1-按指标 / 2-按总量 / 3-按排风+燃料所需空气量计算）
        /// </summary>
        public double ByNorm
        {
            get
            {
                return byNorm;
            }
            set
            {
                byNorm = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged("GetTrueValue");
                this.RaisePropertyChanged("GetTrueColor");
            }
        }

        private double? normValue;
        /// <summary>
        /// 平时补风系数
        /// </summary>
        public double? NormValue
        {
            get
            {
                return normValue;
            }
            set
            {
                normValue = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged("GetTrueValue");
                this.RaisePropertyChanged("GetTrueColor");
            }
        }

        private double? totalValue;
        /// <summary>
        /// 总量
        /// </summary>
        public double? TotalValue
        {
            get
            {
                return totalValue;
            }
            set
            {
                totalValue = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged("GetTrueValue");
                this.RaisePropertyChanged("GetTrueColor");
            }
        }

        public string GetTrueValue
        {
            get
            {
                if (ByNorm == 1)
                    return NormValue.ToString();
                else if (ByNorm == 2)
                    return TotalValue.ToString();
                else
                    return "计算值";
            }
            set
            {
                ByNorm = 1;
                if (string.IsNullOrEmpty(value))
                    NormValue = null;
                else
                    NormValue = double.Parse(value);
            }
        }
        public SolidColorBrush GetTrueColor
        {
            get
            {
                if (ByNorm == 1)
                    return Brushes.White;
                else if (ByNorm == 2)
                    return Brushes.Pink;
                else
                    return Brushes.Moccasin;
            }
        }
        public double CapacityType { get; set; } = 1;

        /// <summary>
        /// 锅炉容量
        /// </summary>
        public double BoilerCapacity { get; set; }

        /// <summary>
        /// 柴发容量
        /// </summary>
        public double FirewoodCapacity { get; set; }

        /// <summary>
        /// 燃烧空气量
        /// </summary>
        public double CombustionAirVolume { get; set; }
    }
}
