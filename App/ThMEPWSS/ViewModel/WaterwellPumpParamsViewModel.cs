using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Diagram.ViewModel
{
    public class WaterwellPumpParamsViewModel : NotifyPropertyChangedBase,ICloneable
    {
        private WaterWellPumpConfigInfo Config = new WaterWellPumpConfigInfo();
        public WaterWellPumpConfigInfo GetConfigInfo()
        {
            return Config;
        }
        public void SetIdentfyConfigInfo(WaterWellIdentifyConfigInfo info)
        {
            Config.WaterWellInfo.identifyInfo = info;
        }
        public object Clone()
        {
            throw new NotImplementedException();
        }
        public bool FilterByWatewellSize 
        { 
            get
            {
                return Config.WaterWellInfo.isWaterWellSizeFilter;
            }
            set
            {
                Config.WaterWellInfo.isWaterWellSizeFilter = value;
                this.RaisePropertyChanged();
            }
        }
        public double MinimumArea
        {
            get
            {
                return Config.WaterWellInfo.fMinacreage;
            }
            set
            {
                Config.WaterWellInfo.fMinacreage = value;
                this.RaisePropertyChanged();
            }
        }
        public string FloorLocation
        {
            get
            {
                return Config.WaterWellInfo.strFloorlocation;
            }
            set
            {
                Config.WaterWellInfo.strFloorlocation = value;
                this.RaisePropertyChanged();
            }
        }
        public string NumberPrefix
        {
            get
            {
                return Config.PumpInfo.strNumberPrefix;
            }
            set
            {
                Config.PumpInfo.strNumberPrefix = value;
                this.RaisePropertyChanged();
            }
        }
        public string PumpsNumber
        {
            get
            {
                return Config.PumpInfo.PumpsNumber.ToString();
            }
            set
            {
                Config.PumpInfo.PumpsNumber = int.Parse(value) ;
                this.RaisePropertyChanged();
            }
        }
        public string PipeDN
        {
            get
            {
                return Config.PumpInfo.strPipeDiameter;
            }
            set
            {
                Config.PumpInfo.strPipeDiameter = value;
                this.RaisePropertyChanged();
            }
        }
        public bool CoveredWaterWell
        {
            get
            {
                return Config.PumpInfo.isCoveredWaterWell;
            }
            set
            {
                Config.PumpInfo.isCoveredWaterWell = value;
                this.RaisePropertyChanged();
            }
        }
        public string MapScale
        {
            get
            {
                return Config.PumpInfo.strMapScale;
            }
            set
            {
                Config.PumpInfo.strMapScale = value;
                this.RaisePropertyChanged();
            }
        }
        public LAYOUTTYPE PumpLyoutType
        {
            get
            {
                return Config.PumpInfo.PumpLyoutType;
            }
            set
            {
                Config.PumpInfo.PumpLyoutType = value;
                this.RaisePropertyChanged();
            }
        }
        public bool IsPickSelection 
        {
            get
            {
                return PumpLyoutType.Equals(LAYOUTTYPE.DOTCHOICE);
            }
            set
            {
                if(value.Equals(true))
                {
                    PumpLyoutType = LAYOUTTYPE.DOTCHOICE;
                }
                else
                {
                    //todo:
                    PumpLyoutType = LAYOUTTYPE.BOXCHOICE;
                }
                this.RaisePropertyChanged();
            }
        }
        public bool IsFrameSelection {
            get
            {
                return PumpLyoutType.Equals(LAYOUTTYPE.BOXCHOICE);
            }
            set
            {
                if (value.Equals(true))
                {
                    PumpLyoutType = LAYOUTTYPE.BOXCHOICE;
                }
                else
                {
                    //todo:
                    PumpLyoutType = LAYOUTTYPE.DOTCHOICE;
                }
                this.RaisePropertyChanged();
            }
        }
        public WaterwellPumpParamsViewModel()
        {
        }
    }
}
