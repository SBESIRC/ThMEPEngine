using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using ThMEPHVAC.FanLayout.Service;

namespace ThMEPHVAC.FanLayout.ViewModel
{
    public class ThFanCEXHViewModel : NotifyPropertyChangedBase, ICloneable
    {
        public ThFanCEXHConfigInfo FanCEXHConfigInfo { set; get; }
        public ThFanCEXHViewModel()
        {
            FanCEXHConfigInfo = new ThFanCEXHConfigInfo();
        }
        public object Clone()
        {
            throw new NotImplementedException();
        }
        public int ExAirPortMarkType
        {
            get { return FanCEXHConfigInfo.AirPipeConfigInfo.AirPortMarkType; }
            set
            {
                FanCEXHConfigInfo.AirPipeConfigInfo.AirPortMarkType = value;
                this.RaisePropertyChanged();
            }
        }
        public bool IsInsertAirPipe 
        {
            get { return FanCEXHConfigInfo.AirPipeConfigInfo.IsInsertPipe; }
            set
            {
                FanCEXHConfigInfo.AirPipeConfigInfo.IsInsertPipe = value;
                this.RaisePropertyChanged();
            }
        }
        public double AirPipeLenght
        {
            get { return FanCEXHConfigInfo.AirPipeConfigInfo.AirPipeLength; }
            set
            {
                FanCEXHConfigInfo.AirPipeConfigInfo.AirPipeLength = value;
                AirPipeWindSpeed = ThFanLayoutDealService.GetAirPipeSpeed(SelectFanConfig.FanVolume, AirPipeLenght, AirPipeHeight);
                this.RaisePropertyChanged();
            }
        }

        public double AirPipeHeight
        {
            get { return FanCEXHConfigInfo.AirPipeConfigInfo.AirPipeHeight; }
            set
            {
                FanCEXHConfigInfo.AirPipeConfigInfo.AirPipeHeight = value;
                AirPipeWindSpeed = ThFanLayoutDealService.GetAirPipeSpeed(SelectFanConfig.FanVolume, AirPipeLenght, AirPipeHeight);
                this.RaisePropertyChanged();
            }
        }
        public double AirPipeMarkHeight
        {
            get { return FanCEXHConfigInfo.AirPipeConfigInfo.AirPipeMarkHeight; }
            set
            {
                FanCEXHConfigInfo.AirPipeConfigInfo.AirPipeMarkHeight = value;
                this.RaisePropertyChanged();
            }
        }
        public double AirPipeWindSpeed
        {
            get { return FanCEXHConfigInfo.AirPipeConfigInfo.AirPipeWindSpeed; }
            set
            {
                FanCEXHConfigInfo.AirPipeConfigInfo.AirPipeWindSpeed = value;
                this.RaisePropertyChanged();
            }
        }
        public double ExAirPortDeepth
        {
            get { return FanCEXHConfigInfo.AirPipeConfigInfo.AirPortDeepth; }
            set
            {
                FanCEXHConfigInfo.AirPipeConfigInfo.AirPortDeepth = value;
                this.RaisePropertyChanged();
            }
        }
        public double ExAirPortHeight
        {
            get { return FanCEXHConfigInfo.AirPipeConfigInfo.AirPortHeight; }
            set
            {
                FanCEXHConfigInfo.AirPipeConfigInfo.AirPortHeight = value;
                ExAirPortWindSpeed = ThFanLayoutDealService.GetAirPipeSpeed(SelectFanConfig.FanVolume, ExAirPortLength, ExAirPortHeight);
                this.RaisePropertyChanged();
            }
        }
        public double ExAirPortLength
        {
            get { return FanCEXHConfigInfo.AirPipeConfigInfo.AirPortLength; }
            set
            {
                FanCEXHConfigInfo.AirPipeConfigInfo.AirPortLength = value;
                ExAirPortWindSpeed = ThFanLayoutDealService.GetAirPipeSpeed(SelectFanConfig.FanVolume, ExAirPortLength, ExAirPortHeight);
                this.RaisePropertyChanged();
            }
        }
        public double ExAirPortWindSpeed
        {
            get { return FanCEXHConfigInfo.AirPipeConfigInfo.AirPortWindSpeed; }
            set
            {
                FanCEXHConfigInfo.AirPipeConfigInfo.AirPortWindSpeed = value;
                this.RaisePropertyChanged();
            }
        }
        public double ExAirPortMarkHeight
        {
            get { return FanCEXHConfigInfo.AirPipeConfigInfo.AirPortMarkHeight; }
            set
            {
                FanCEXHConfigInfo.AirPipeConfigInfo.AirPortMarkHeight = value;
                this.RaisePropertyChanged();
            }
        }
        public bool IsInsertAirPort
        {
            get { return FanCEXHConfigInfo.AirPortSideConfigInfo.IsInsertAirPort; }
            set
            {
                FanCEXHConfigInfo.AirPortSideConfigInfo.IsInsertAirPort = value;
                this.RaisePropertyChanged();
            }
        }
        public bool IsInsertValve
        {
            get { return FanCEXHConfigInfo.AirPortSideConfigInfo.IsInsertValve; }
            set
            {
                FanCEXHConfigInfo.AirPortSideConfigInfo.IsInsertValve = value;
                this.RaisePropertyChanged();
            }
        }
        public int EnMarkHeigthType
        {
            get { return FanCEXHConfigInfo.AirPortSideConfigInfo.MarkHeigthType; }
            set
            {
                FanCEXHConfigInfo.AirPortSideConfigInfo.MarkHeigthType = value;
                this.RaisePropertyChanged();
            }
        }
        public double EnAirPortDeepth
        {
            get { return FanCEXHConfigInfo.AirPortSideConfigInfo.AirPortDeepth; }
            set
            {
                FanCEXHConfigInfo.AirPortSideConfigInfo.AirPortDeepth = value;
                this.RaisePropertyChanged();
            }
        }
        public double EnAirPortHeight
        {
            get { return FanCEXHConfigInfo.AirPortSideConfigInfo.AirPortHeight; }
            set
            {
                FanCEXHConfigInfo.AirPortSideConfigInfo.AirPortHeight = value;
                EnAirPortWindSpeed = ThFanLayoutDealService.GetAirPortSpeed(SelectFanConfig.FanVolume, EnAirPortLength, EnAirPortHeight);
                this.RaisePropertyChanged();
            }
        }
        public double EnAirPortLength
        {
            get { return FanCEXHConfigInfo.AirPortSideConfigInfo.AirPortLength; }
            set
            {
                FanCEXHConfigInfo.AirPortSideConfigInfo.AirPortLength = value;
                EnAirPortWindSpeed = ThFanLayoutDealService.GetAirPortSpeed(SelectFanConfig.FanVolume, EnAirPortLength, EnAirPortHeight);
                this.RaisePropertyChanged();
            }
        }
        public double EnAirPortMarkHeight
        {
            get { return FanCEXHConfigInfo.AirPortSideConfigInfo.AirPortMarkHeight; }
            set
            {
                FanCEXHConfigInfo.AirPortSideConfigInfo.AirPortMarkHeight = value;
                
                this.RaisePropertyChanged();
            }
        }
        public double EnAirPortWindSpeed
        {
            get { return FanCEXHConfigInfo.AirPortSideConfigInfo.AirPortWindSpeed; }
            set
            {
                FanCEXHConfigInfo.AirPortSideConfigInfo.AirPortWindSpeed = value;
                this.RaisePropertyChanged();
            }
        }
        public ThFanConfigInfo SelectFanConfig
        {
            get { return FanCEXHConfigInfo.FanSideConfigInfo.FanConfigInfo; }
            set
            {
                FanCEXHConfigInfo.FanSideConfigInfo.FanConfigInfo = value;
                AirPipeWindSpeed = ThFanLayoutDealService.GetAirPortSpeed(SelectFanConfig.FanVolume, AirPipeLenght, AirPipeHeight);
                ExAirPortWindSpeed = ThFanLayoutDealService.GetAirPipeSpeed(SelectFanConfig.FanVolume, ExAirPortLength, ExAirPortHeight);
                EnAirPortWindSpeed = ThFanLayoutDealService.GetAirPortSpeed(SelectFanConfig.FanVolume, EnAirPortLength, EnAirPortHeight);
                this.RaisePropertyChanged();
            }
        }
        public ObservableCollection<ThFanConfigInfo> FanInfoConfigs
        {
            get
            {
                return FanCEXHConfigInfo.FanSideConfigInfo.FanInfoList;
            }
            set
            {
                FanCEXHConfigInfo.FanSideConfigInfo.FanInfoList = value;
                this.RaisePropertyChanged();
            }
        }

    }
}
