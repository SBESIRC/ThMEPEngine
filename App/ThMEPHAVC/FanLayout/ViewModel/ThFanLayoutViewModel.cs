using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPHVAC.FanLayout.ViewModel
{
    public class ThFanLayoutViewModel : NotifyPropertyChangedBase, ICloneable
    {
        public ThFanLayoutConfigInfo thFanLayoutConfigInfo { set; get; }
        public ThFanLayoutViewModel()
        {
            thFanLayoutConfigInfo = new ThFanLayoutConfigInfo();
        }
        public object Clone()
        {
            throw new NotImplementedException();
        }
        public int FanType 
        { 
            get { return thFanLayoutConfigInfo.FanType; }
            set 
            { 
                thFanLayoutConfigInfo.FanType = value;
                this.RaisePropertyChanged();
            }
        }
        public bool IsInsertHole
        {
            get { return thFanLayoutConfigInfo.IsInsertHole; }
            set
            {
                thFanLayoutConfigInfo.IsInsertHole = value;
                this.RaisePropertyChanged();
            }
        }
        public string MapScale
        {
            get
            {
                return thFanLayoutConfigInfo.MapScale;
            }
            set
            {
                thFanLayoutConfigInfo.MapScale = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
