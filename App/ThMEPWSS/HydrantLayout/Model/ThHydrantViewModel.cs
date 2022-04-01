using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPWSS.HydrantLayout.Model
{
    public class ThHydrantViewModel : NotifyPropertyChangedBase
    {
        private bool _CheckHydrant { get; set; }
        public bool CheckHydrant
        {
            get
            {
                return _CheckHydrant;
            }
            set
            {
                _CheckHydrant = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _CheckExtinguisher { get; set; }
        public bool ChechExtinguisher
        {
            get
            {
                return _CheckExtinguisher;
            }
            set
            {
                _CheckExtinguisher = value;
                this.RaisePropertyChanged();
            }
        }

        private int _SearchRadius { get; set; }
        public int SearchRadius
        {
            get
            {
                return _SearchRadius;
            }
            set
            {
                _SearchRadius = value;
                this.RaisePropertyChanged();
            }
        }
        private LayoutModeType _LayoutMode { get; set; }
        public LayoutModeType LayoutMode
        {
            get
            {
                return _LayoutMode;
            }
            set
            {
                _LayoutMode = value;
                this.RaisePropertyChanged();
            }
        }
        private bool _AvoidParking { get; set; }
        public bool AvoidParking
        {
            //开门是否避让车位 T:避让 F:不用避让
            get
            {
                return _AvoidParking;
            }
            set
            {
                _AvoidParking = value;
                this.RaisePropertyChanged();
            }
        }

        public ThHydrantViewModel()
        {
            CheckHydrant = true;
            ChechExtinguisher = true;
            SearchRadius = 3000;
            LayoutMode = LayoutModeType.Both;
            AvoidParking = true;
        }
    }
    public enum LayoutModeType
    {
        //一字（0） L字（1） 两者都考虑（2）
        IType = 0,
        LType = 1,
        Both = 2,
    }
}
