using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels
{
    public class EvacuationWalkViewModel : BaseSmokeProofViewModel
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
                WindVolume = WindVolume;
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
                WindVolume = WindVolume;
                this.RaisePropertyChanged();
            }
        }
    }
}
