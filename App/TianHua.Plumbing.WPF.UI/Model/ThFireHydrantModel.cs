using System.ComponentModel;

namespace TianHua.Plumbing.WPF.UI.Model
{
    public class ThFireHydrantModel : INotifyPropertyChanged
    {
        public ThFireHydrantModel()
        {            
            checkFireHydrant = true;
            isDoubleStrands = true;
            hoseLength = 25.0;
            isTenMetres = true;
            isShowCheckResult = true;
            isCalculation = true;
            dangerLevel = "中危险级";
            fireType = "A类火灾";
            calculationLength = 25.0;
            selfLength = 15.0;
        }
        private bool checkFireHydrant;
        /// <summary>
        /// 校核对象->消火栓
        /// </summary>
        public bool CheckFireHydrant 
        {
            get
            {
                return checkFireHydrant;
            }
            set
            {
                checkFireHydrant = value;
                RaisePropertyChanged("CheckFireHydrant");
            }
        }
        private bool checkFireExtinguisher;
        /// <summary>
        /// 校核对象->灭火器
        /// </summary>
        public bool CheckFireExtinguisher
        {
            get
            {
                return checkFireExtinguisher;
            }
            set
            {
                checkFireExtinguisher = value;
                RaisePropertyChanged("CheckFireExtinguisher");
            }
        }

        private bool isDoubleStrands;
        /// <summary>
        /// 保护强度->双股
        /// </summary>
        public bool IsDoubleStrands
        {
            get
            {
                return isDoubleStrands;
            }
            set
            {
                isDoubleStrands = value;
                RaisePropertyChanged("IsDoubleStrands");
            }
        }

        private bool isSingleStrands;
        /// <summary>
        /// 保护强度->单股
        /// </summary>
        public bool IsSingleStrands
        {
            get
            {
                return isSingleStrands;
            }
            set
            {
                isSingleStrands = value;
                RaisePropertyChanged("IsSingleStrands");
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

        private bool isTenMetres;
        /// <summary>
        /// 10米
        /// </summary>
        public bool IsTenMetres
        {
            get
            {
                return isTenMetres;
            }
            set
            {
                isTenMetres = value;
                RaisePropertyChanged("IsTenMetres");
            }
        }

        private bool isThirteenMetres;
        /// <summary>
        /// 13米
        /// </summary>
        public bool IsThirteenMetres
        {
            get
            {
                return isThirteenMetres;
            }
            set
            {
                isThirteenMetres = value;
                RaisePropertyChanged("IsThirteenMetres");
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

        private bool isCalculation;
        /// <summary>
        /// 最大距离保护-> 计算值选项
        /// </summary>
        public bool IsCalculation
        {
            get
            {
                return isCalculation;
            }
            set
            {
                isCalculation = value;
                RaisePropertyChanged("IsCalculation");
            }
        }

        private bool isSelf;
        /// <summary>
        /// 最大距离保护-> 自定义选项
        /// </summary>
        public bool IsSelf
        {
            get
            {
                return isSelf;
            }
            set
            {
                isSelf = value;
                RaisePropertyChanged("IsSelf");
            }
        }

        private double calculationLength;
        /// <summary>
        /// 最大距离保护-> 计算值长度
        /// </summary>
        public double CalculationLength
        {
            get
            {
                return calculationLength;
            }
            set
            {
                calculationLength = value;
                RaisePropertyChanged("CalculationLength");
            }
        }

        private double selfLength;
        /// <summary>
        /// 最大距离保护-> 自定义长度
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
    }
}
