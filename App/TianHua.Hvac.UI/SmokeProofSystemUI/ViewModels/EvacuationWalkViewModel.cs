using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace TianHua.Hvac.UI.SmokeProofSystemUI.ViewModels
{
    class EvacuationWalkViewModel : NotifyPropertyChangedBase
    {
        /// <summary>
        /// 风量
        /// </summary>
        private double _windVolume;
        public double WindVolume
        {
            get
            {
                return Math.Round(AreaNet * AirVolSpec);
            }
            set
            {
                _windVolume = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 净面积
        /// </summary>
        private double _areaNet;
        public double AreaNet
        {
            get
            {
                return _areaNet;
            }
            set
            {
                _areaNet = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 单位风量
        /// </summary>
        private double _airVolSpec;
        public double AirVolSpec
        {
            get
            {
                return _airVolSpec;
            }
            set
            {
                _airVolSpec = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
