using System;
using System.ComponentModel;

namespace ThMEPWSS.Hydrant.Model
{
    public class ThFireHydrantModel : INotifyPropertyChanged
    {
        public ThFireHydrantModel()
        {
            hoseLength = 25.0;
            selfLength = 15.0;
            fireType = "A类火灾";
            dangerLevel = "中危险级";
            isShowCheckResult = true;
            checkObjectOption = CheckObjectOps.FireHydrant;
            protectStrengthOption = ProtectStrengthOps.DoubleStrand;
            waterColumnLengthOption = WaterColumnLengthOps.TenMeters;
            maxProtectDisOption = MaxProtectDisOps.Calculation;
            reductionFactorOption = ReductionFactorOps.ZeroDotEight;
            waterColumnAngleOption = WaterColumnAngleOps.FortyFiveDegree;
        }
        private CheckObjectOps checkObjectOption;
        /// <summary>
        /// 校核对象->消火栓
        /// </summary>
        public CheckObjectOps CheckObjectOption
        {
            get
            {
                return checkObjectOption;
            }
            set
            {
                checkObjectOption = value;
                RaisePropertyChanged("CheckObjectOption");
            }
        }

        private ProtectStrengthOps protectStrengthOption;
        /// <summary>
        /// 保护强度->双股
        /// </summary>
        public ProtectStrengthOps ProtectStrengthOption
        {
            get
            {
                return protectStrengthOption;
            }
            set
            {
                protectStrengthOption = value;
                RaisePropertyChanged("ProtectStrengthOption");
            }
        }

        private double hoseLength;
        /// <summary>
        /// 水龙带长
        /// </summary>
        public double HoseLength
        {
            get
            {
                return hoseLength;
            }
            set
            {
                hoseLength = value;
                RaisePropertyChanged("HoseLength");
            }
        }

        private WaterColumnLengthOps waterColumnLengthOption;
        public WaterColumnLengthOps WaterColumnLengthOption
        {
            get
            {
                return waterColumnLengthOption;
            }
            set
            {
                waterColumnLengthOption = value;
                RaisePropertyChanged("WaterColumnLengthOption");
            }
        }

        private string dangerLevel;
        /// <summary>
        /// 危险等级
        /// </summary>
        public string DangerLevel
        {
            get
            {
                return dangerLevel;
            }
            set
            {
                dangerLevel = value;
                RaisePropertyChanged("DangerLevel");
            }
        }

        private string fireType;
        /// <summary>
        /// 火灾种类
        /// </summary>
        public string FireType
        {
            get
            {
                return fireType;
            }
            set
            {
                fireType = value;
                RaisePropertyChanged("FireType");
            }
        }

        private MaxProtectDisOps maxProtectDisOption;
        /// <summary>
        /// 最大距离保护-> 计算值选项
        /// </summary>
        public MaxProtectDisOps MaxProtectDisOption
        {
            get
            {
                return maxProtectDisOption;
            }
            set
            {
                maxProtectDisOption = value;
                RaisePropertyChanged("MaxProtectDisOption");
            }
        }

        private double selfLength;
        /// <summary>
        /// 最大保护距离-> 自定义长度
        /// </summary>
        public double SelfLength
        {
            get
            {
                return selfLength;
            }
            set
            {
                selfLength = value;
                RaisePropertyChanged("SelfLength");
            }
        }

        private bool isThinkIsolatedColumn;
        /// <summary>
        /// 考虑孤立柱
        /// </summary>
        public bool IsThinkIsolatedColumn
        {
            get
            {
                return isThinkIsolatedColumn;
            }
            set
            {
                isThinkIsolatedColumn = value;
                RaisePropertyChanged("IsThinkIsolatedColumn");
            }
        }

        private bool isShowCheckResult;
        /// <summary>
        /// 显示校验结果
        /// </summary>
        public bool IsShowCheckResult
        {
            get
            {
                return isShowCheckResult;
            }
            set
            {
                isShowCheckResult = value;
                RaisePropertyChanged("IsShowCheckResult");
            }

        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// 水柱长度
        /// </summary>
        public double WaterColumnLength
        {
            get
            {
                return WaterColumnLengthOption == WaterColumnLengthOps.TenMeters ? 10 : 13;
            }
        }

        private ReductionFactorOps reductionFactorOption;
        /// <summary>
        /// 折减系数
        /// </summary>
        public ReductionFactorOps ReductionFactorOption
        {
            get
            {
                return reductionFactorOption;
            }
            set
            {
                reductionFactorOption = value;
                RaisePropertyChanged("ReductionFactorOption");
            }

        }

        private WaterColumnAngleOps waterColumnAngleOption;
        /// <summary>
        /// 水柱角度(相对水平)
        /// </summary>
        public WaterColumnAngleOps WaterColumnAngleOption
        {
            get
            {
                return waterColumnAngleOption;
            }
            set
            {
                waterColumnAngleOption = value;
                RaisePropertyChanged("WaterColumnAngleOption");
            }

        }
        /// <summary>
        /// 水龙带能够走到的范围
        /// </summary>
        public double FireHoseWalkRange
        {
            get
            {
                //折减系数
                double k3 = reductionFactorOption == ReductionFactorOps.ZeroDotEight ? 0.8 : 0.9;
                return k3 * HoseLength;
            }
        }
        /// <summary>
        /// 范围上的点朝 360°喷射水柱的范围
        /// </summary>

        public double SprayWaterColumnRange
        {
            get
            {
                var ang = waterColumnAngleOption == WaterColumnAngleOps.FortyFiveDegree ? 45.0 : 60.0;
                var rad = ThMEPEngineCore.CAD.ThAuxiliaryUtils.AngToRad(ang);
                return Math.Sin(rad) * WaterColumnLength;
            }
        }
        /// <summary>
        /// 获取保护强度
        /// </summary>
        public bool GetProtectStrength
        {
            // true->单股，false->双股
            get
            {
                if (checkObjectOption == CheckObjectOps.FireHydrant)
                {
                    return protectStrengthOption == ProtectStrengthOps.SingleStrand ? true : false;
                }
                else
                {
                    return true;
                }
            }
        }
    }
    /// <summary>
    /// 检查对象
    /// </summary>
    public enum CheckObjectOps
    {
        FireHydrant = 0,
        FireExtinguisher = 1,
    }
    /// <summary>
    /// 最大保护距离
    /// </summary>
    public enum MaxProtectDisOps
    {
        Calculation = 0,
        Custom = 1,
    }
    /// <summary>
    /// 保护强度
    /// </summary>
    public enum ProtectStrengthOps
    {
        DoubleStrand = 0,
        SingleStrand = 1,
    }
    /// <summary>
    /// 水柱长度
    /// </summary>
    public enum WaterColumnLengthOps
    {
        TenMeters = 0,
        ThirteenMeters = 1,
    }
    /// <summary>
    /// 折减系数
    /// </summary>
    public enum ReductionFactorOps
    {
        /// <summary>
        /// 0.8
        /// </summary>
        ZeroDotEight,
        /// <summary>
        /// 0.9
        /// </summary>
        ZeroDotNine
    }

    /// <summary>
    /// 水柱角度(相对水平)
    /// </summary>
    public enum WaterColumnAngleOps
    {
        /// <summary>
        /// 45°
        /// </summary>
        FortyFiveDegree,
        /// <summary>
        /// 60°
        /// </summary>
        SixtyDegree
    }
}
