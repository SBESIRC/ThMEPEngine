using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPElectrical.ElectricalLoadCalculation
{
    [Serializable]
    public class ElectricalConfigDataModel
    {
        public List<DynamicLoadCalculationModelData> Configs;
        public ElectricalConfigDataModel()
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
        /// 用电指标-column2-class
        /// </summary>
        public PowerSpecifications PowerNorm { get; set; }

        public int CompareTo(DynamicLoadCalculationModelData other)
        {
            bool IsCompare =
                this.RoomFunction.Equals(other.RoomFunction)
                && this.PowerNorm.ByNorm.Equals(other.PowerNorm.ByNorm)
                && this.PowerNorm.NormValue.Equals(other.PowerNorm.NormValue)
                && this.PowerNorm.TotalValue.Equals(other.PowerNorm.TotalValue);
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
    public class PowerSpecifications : NotifyPropertyChangedBase
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

        private int? normValue;
        /// <summary>
        /// 指标量
        /// </summary>
        public int? NormValue
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

        private int? minTotalValue;
        /// <summary>
        /// 指标量
        /// </summary>
        public int? MinTotalValue
        {
            get
            {
                return minTotalValue;
            }
            set
            {
                minTotalValue = value;
                this.RaisePropertyChanged();
            }
        }

        private int totalValue;
        /// <summary>
        /// 总量
        /// </summary>
        public int TotalValue
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

        public int? GetTrueValue
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
}
