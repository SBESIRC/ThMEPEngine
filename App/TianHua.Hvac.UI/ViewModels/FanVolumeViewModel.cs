using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using ThMEPHVAC.EQPMFanModelEnums;

namespace TianHua.Hvac.UI.ViewModels
{
    class FanVolumeViewModel : NotifyPropertyChangedBase
    {
        public FanVolumeCalcModel VolumeCalcModel { get; }
        public FanVolumeViewModel(FanVolumeCalcModel model) 
        {
            VolumeCalcModel = model;
            CalcAirVolume();
        }
        /// <summary>
        /// 风量计算值
        /// </summary>
        public int AirCalcValue
        {
            get { return VolumeCalcModel.AirCalcValue; }
            set
            {
                VolumeCalcModel.AirCalcValue = value;
                CalcAirVolume();
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 风量计算系数
        /// </summary>
        public double AirCalcFactor
        {
            get { return VolumeCalcModel.AirCalcFactor; }
            set
            {
                VolumeCalcModel.AirCalcFactor = value;
                CalcAirVolume();
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 风量
        /// </summary>
        public int AirVolume
        {
            get { return VolumeCalcModel.AirVolume; }
            set
            {
                VolumeCalcModel.AirVolume = value;
                this.RaisePropertyChanged();
            }
        }
        void CalcAirVolume() 
        {
            var temp = VolumeCalcModel.AirCalcValue * VolumeCalcModel.AirCalcFactor;
            var d = temp / 50.0;
            var intMax = (int)Math.Ceiling(d);
            AirVolume = intMax * 50;
        }
    }
}
