using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.ViewModel
{
    public class FirstFloorPlaneViewModel : NotifyPropertyChangedBase
    {
        /// <summary>
        /// 套管标高
        /// </summary>
        private double _drivepipeLevel = -0.55;
        public double DrivepipeLevel
        {
            get
            {
                return _drivepipeLevel;
            }
            set
            {
                _drivepipeLevel = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 污废水(合流\分流)
        /// </summary>
        DirvepipeDimensionTypeEnum _dirvepipeDimensionType;
        public DirvepipeDimensionTypeEnum DirvepipeDimensionType
        {
            get
            {
                return _dirvepipeDimensionType;
            }
            set
            {
                _dirvepipeDimensionType = value;
                OnPropertyChanged(nameof(DirvepipeDimensionType));
            }
        }
    }

    public enum DirvepipeDimensionTypeEnum
    {
        /// <summary>
        /// 普通钢套管
        /// </summary>
        OrdinarySteel = 0,

        /// <summary>
        /// B型钢防水套管
        /// </summary>
        BSteelWaterproof = 1,

        /// <summary>
        /// A型柔性防水套管
        /// </summary>
        AFlexibleWaterproof = 2,

        /// <summary>
        /// A型防护密闭套管
        /// </summary>
        AProtectiveSealing = 3,

        // <summary>
        /// C型防护密闭套管
        /// </summary>
        CProtectiveSealing = 4,

        // <summary>
        /// E型防护密闭套管
        /// </summary>
        EProtectiveSealing = 5,
    }
}
