using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPWSS.DrainageADPrivate.Model
{
    public class ThDrainageADPViewModel : NotifyPropertyChangedBase
    {
        private double _qL { get; set; }
        public double qL
        {
            get
            {
                return _qL;
            }
            set
            {
                _qL = value;
                this.RaisePropertyChanged();
            }
        }
        private double _m { get; set; }
        public double m
        {
            get
            {
                return _m;
            }
            set
            {
                _m = value;
                this.RaisePropertyChanged();
            }
        }

        private double _Kh { get; set; }
        public double Kh
        {
            get
            {
                return _Kh;
            }
            set
            {
                _Kh = value;
                this.RaisePropertyChanged();
            }
        }

        public ThDrainageADPViewModel()
        {
            qL = 230.0;
            m = 3.5;
            Kh = 1.5;
        }
    }
}
