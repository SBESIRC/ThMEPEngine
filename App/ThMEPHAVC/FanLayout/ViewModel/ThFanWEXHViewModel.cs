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
    public class ThFanWEXHViewModel : NotifyPropertyChangedBase, ICloneable
    {
        public ThFanWEXHConfigInfo fanWEXHConfigInfo { set; get; }
        public ThFanWEXHViewModel()
        {
            fanWEXHConfigInfo = new ThFanWEXHConfigInfo();
        }
        public object Clone()
        {
            throw new NotImplementedException();
        }
        public int FanMarkHeigthType
        {
            get { return fanWEXHConfigInfo.FanSideConfigInfo.MarkHeigthType; }
            set
            {
                fanWEXHConfigInfo.FanSideConfigInfo.MarkHeigthType = value;
                this.RaisePropertyChanged();
            }
        }

        public double FanMarkHeight
        {
            get { return fanWEXHConfigInfo.FanSideConfigInfo.FanMarkHeight; }
            set
            {
                fanWEXHConfigInfo.FanSideConfigInfo.FanMarkHeight = value;
                this.RaisePropertyChanged();
            }
        }
        public int AirMarkHeigthType
        {
            get { return fanWEXHConfigInfo.AirPortSideConfigInfo.MarkHeigthType; }
            set
            {
                fanWEXHConfigInfo.AirPortSideConfigInfo.MarkHeigthType = value;
                this.RaisePropertyChanged();
            }
        }
        public double AirMarkHeight
        {
            get { return fanWEXHConfigInfo.AirPortSideConfigInfo.AirPortMarkHeight; }
            set
            {
                fanWEXHConfigInfo.AirPortSideConfigInfo.AirPortMarkHeight = value;
                this.RaisePropertyChanged();
            }
        }
        public bool IsInsertValve
        {
            get { return fanWEXHConfigInfo.AirPortSideConfigInfo.IsInsertValve; }
            set
            {
                fanWEXHConfigInfo.AirPortSideConfigInfo.IsInsertValve = value;
                this.RaisePropertyChanged();
            }
        }
        public bool IsInsertAirPort
        {
            get { return fanWEXHConfigInfo.AirPortSideConfigInfo.IsInsertAirPort; }
            set
            {
                fanWEXHConfigInfo.AirPortSideConfigInfo.IsInsertAirPort = value;
                this.RaisePropertyChanged();
            }
        }
        public double AirPortLength
        {
            get { return fanWEXHConfigInfo.AirPortSideConfigInfo.AirPortLength; }
            set
            {
                fanWEXHConfigInfo.AirPortSideConfigInfo.AirPortLength = value;
                WindSpeed = ThFanLayoutDealService.GetAirPortSpeed(SelectFanConfig.FanVolume, AirPortLength, AirPortHeight);
                this.RaisePropertyChanged();
            }
        }
        public double AirPortHeight
        {
            get { return fanWEXHConfigInfo.AirPortSideConfigInfo.AirPortHeight; }
            set
            {
                fanWEXHConfigInfo.AirPortSideConfigInfo.AirPortHeight = value;
                WindSpeed = ThFanLayoutDealService.GetAirPortSpeed(SelectFanConfig.FanVolume, AirPortLength, AirPortHeight);
                this.RaisePropertyChanged();
            }
        }
        public double WindSpeed
        {
            get { return fanWEXHConfigInfo.AirPortSideConfigInfo.AirPortWindSpeed; }
            set
            {
                fanWEXHConfigInfo.AirPortSideConfigInfo.AirPortWindSpeed = value;
                this.RaisePropertyChanged();
            }
        }
        public ThFanConfigInfo SelectFanConfig
        {
            get { return fanWEXHConfigInfo.FanSideConfigInfo.FanConfigInfo; }
            set
            {
                fanWEXHConfigInfo.FanSideConfigInfo.FanConfigInfo = value;
                WindSpeed = ThFanLayoutDealService.GetAirPortSpeed(SelectFanConfig.FanVolume, AirPortLength, AirPortHeight);
                this.RaisePropertyChanged();
            }
        }
        public ObservableCollection<ThFanConfigInfo> FanInfoConfigs
        {
            get
            {
                return fanWEXHConfigInfo.FanSideConfigInfo.FanInfoList;
            }
            set
            {
                fanWEXHConfigInfo.FanSideConfigInfo.FanInfoList = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
