using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPHVAC.FanLayout.ViewModel
{
    public class ThFanWAFViewModel : NotifyPropertyChangedBase, ICloneable
    {
        public ThFanWAFConfigInfo fanWAFConfigInfo { set; get; }
        public ThFanWAFViewModel()
        {
            fanWAFConfigInfo = new ThFanWAFConfigInfo();
        }
        public object Clone()
        {
            throw new NotImplementedException();
        }

        public int FanMarkHeigthType
        {
            get { return fanWAFConfigInfo.FanSideConfigInfo.MarkHeigthType; }
            set
            {
                fanWAFConfigInfo.FanSideConfigInfo.MarkHeigthType = value;
                this.RaisePropertyChanged();
            }
        }
        
        public double FanMarkHeight
        {
            get { return fanWAFConfigInfo.FanSideConfigInfo.FanMarkHeight; }
            set
            {
                fanWAFConfigInfo.FanSideConfigInfo.FanMarkHeight = value;
                this.RaisePropertyChanged();
            }
        }
        public int AirMarkHeigthType
        {
            get { return fanWAFConfigInfo.AirPortSideConfigInfo.MarkHeigthType; }
            set
            {
                fanWAFConfigInfo.AirPortSideConfigInfo.MarkHeigthType = value;
                this.RaisePropertyChanged();
            }
        }
        public double AirMarkHeight
        {
            get { return fanWAFConfigInfo.AirPortSideConfigInfo.AirPortMarkHeight; }
            set
            {
                fanWAFConfigInfo.AirPortSideConfigInfo.AirPortMarkHeight = value;
                this.RaisePropertyChanged();
            }
        }
        public bool IsInsertValve
        {
            get { return fanWAFConfigInfo.AirPortSideConfigInfo.IsInsertValve; }
            set
            {
                fanWAFConfigInfo.AirPortSideConfigInfo.IsInsertValve = value;
                this.RaisePropertyChanged();
            }
        }
        public bool IsInsertAirPort
        {
            get { return fanWAFConfigInfo.AirPortSideConfigInfo.IsInsertAirPort; }
            set
            {
                fanWAFConfigInfo.AirPortSideConfigInfo.IsInsertAirPort = value;
                this.RaisePropertyChanged();
            }
        }
        public double AirPortLength
        {
            get { return fanWAFConfigInfo.AirPortSideConfigInfo.AirPortLength; }
            set
            {
                fanWAFConfigInfo.AirPortSideConfigInfo.AirPortLength = value;
                this.RaisePropertyChanged();
            }
        }
        public double AirPortHeight
        {
            get { return fanWAFConfigInfo.AirPortSideConfigInfo.AirPortHeight; }
            set
            {
                fanWAFConfigInfo.AirPortSideConfigInfo.AirPortHeight = value;
                this.RaisePropertyChanged();
            }
        }
        public double WindSpeed
        {
            get { return fanWAFConfigInfo.AirPortSideConfigInfo.AirPortWindSpeed; }
            set
            {
                this.RaisePropertyChanged();
            }
        }
        public ThFanConfigInfo SelectFanConfig 
        {
            get { return fanWAFConfigInfo.FanSideConfigInfo.FanConfigInfo; }
            set
            {
                fanWAFConfigInfo.FanSideConfigInfo.FanConfigInfo = value;
                this.RaisePropertyChanged();
            }
        }
        public ObservableCollection<ThFanConfigInfo> FanInfoConfigs
        {
            get
            {
                return fanWAFConfigInfo.FanSideConfigInfo.FanInfoList;
            }
            set
            {
                fanWAFConfigInfo.FanSideConfigInfo.FanInfoList = value;
                this.RaisePropertyChanged();
            }
        }

    }
}
