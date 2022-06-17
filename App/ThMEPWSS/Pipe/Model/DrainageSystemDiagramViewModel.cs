using System;
using System.Collections.Generic;
using System.Windows;
using ThControlLibraryWPF.ControlUtils;
using ThMEPWSS.JsonExtensionsNs;


namespace ThMEPWSS.Pipe.Model
{


    [Serializable]
    public class DrainageSystemDiagramViewModel : NotifyPropertyChangedBase
    {
        public static readonly DrainageSystemDiagramViewModel Singleton = new DrainageSystemDiagramViewModel();
        private List<string> _FloorListDatas = new List<string>();
        public List<string> FloorListDatas
        {
            get
            {
                return _FloorListDatas;
            }
            set
            {
                _FloorListDatas = value;
                this.RaisePropertyChanged();
            }
        }

        private DrainageSystemDiagramParamsViewModel _Params = new DrainageSystemDiagramParamsViewModel();
        public DrainageSystemDiagramParamsViewModel Params
        {
            get
            {
                return _Params;
            }
            set
            {
                _Params = value;
                this.RaisePropertyChanged();
            }
        }

        public void CollectFloorListDatas(bool focus)
        {
            try
            {
                if (DateTime.Now == DateTime.MinValue)
                {
                    //框选
                    Pipe.Service.ThDrainageService.CollectFloorListDatas();
                }
                else
                {
                    //点选
                    ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.CollectFloorListDatasEx(focus);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
    [Serializable]
    public class DrainageSystemDiagramParamsViewModel : NotifyPropertyChangedBase
    {
        public void CopyTo(DrainageSystemDiagramParamsViewModel other)
        {
            foreach (var pi in GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                pi.SetValue(other, pi.GetValue(this));
            }
        }
        public DrainageSystemDiagramParamsViewModel Clone()
        {
            return this.ToJson().FromJson<DrainageSystemDiagramParamsViewModel>();
        }
        private double _StoreySpan = 1800; //mm
        public double StoreySpan
        {
            get
            {
                if (_StoreySpan <= 0)
                {
                    _StoreySpan = 2000;
                    this.RaisePropertyChanged();
                }
                return _StoreySpan;
            }

            set
            {
                _StoreySpan = value;
                this.RaisePropertyChanged();
            }
        }
        private bool _YS = true;
        public bool YS
        {
            get
            {
                return _YS;
            }
            set
            {
                _YS = value;
                this.RaisePropertyChanged();
            }
        }
        private bool _WFS = true;
        public bool WFS
        {
            get
            {
                return _WFS;
            }
            set
            {
                _WFS = value;
                this.RaisePropertyChanged();
            }
        }
        private string _WashingMachineFloorDrainDN = "DN50";
        public string WashingMachineFloorDrainDN
        {
            get
            {
                return _WashingMachineFloorDrainDN;
            }
            set
            {
                _WashingMachineFloorDrainDN = value;
                this.RaisePropertyChanged();
            }
        }
        private string _OtherFloorDrainDN = "DN50";
        public string OtherFloorDrainDN
        {
            get
            {
                return _OtherFloorDrainDN;
            }
            set
            {
                _OtherFloorDrainDN = value;
                this.RaisePropertyChanged();
            }
        }
        private string _DirtyWaterWellDN = "DN50";
        //这个指的是FL0的
        public string DirtyWaterWellDN
        {
            get
            {
                return _DirtyWaterWellDN;
            }
            set
            {
                _DirtyWaterWellDN = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _ShouldRaiseWashingMachine = false;
        public bool ShouldRaiseWashingMachine
        {
            get
            {
                return _ShouldRaiseWashingMachine;
            }
            set
            {
                _ShouldRaiseWashingMachine = value;
                this.RaisePropertyChanged();
            }
        }
        private bool _CouldHavePeopleOnRoof = true;
        public bool CouldHavePeopleOnRoof
        {
            get
            {
                return _CouldHavePeopleOnRoof;
            }
            set
            {
                _CouldHavePeopleOnRoof = value;
                this.RaisePropertyChanged();
            }
        }
        private bool _CanHaveDownboard = true;//能否有降板线
        public bool CanHaveDownboard
        {
            get
            {
                return _CanHaveDownboard;
            }
            set
            {
                _CanHaveDownboard = value;
                this.RaisePropertyChanged();
            }
        }
        private bool _H;
        public bool H
        {
            get
            {
                return _H;
            }
            set
            {
                _H = value;
                this.RaisePropertyChanged();
            }
        }
        private string _Basin = "双池S弯";
        public string Basin
        {
            get
            {
                return _Basin;
            }
            set
            {
                _Basin = value;
                this.RaisePropertyChanged();
            }
        }
        private string _BasinDN = "DN50";
        public string BasinDN
        {
            get
            {
                return _BasinDN;
            }
            set
            {
                _BasinDN = value;
                this.RaisePropertyChanged();
            }
        }

        private string _BalconyFloorDrainDN = "DN50";
        public string BalconyFloorDrainDN
        {
            get
            {
                return _BalconyFloorDrainDN;
            }
            set
            {
                _BalconyFloorDrainDN = value;
                this.RaisePropertyChanged();
            }
        }

        private string _WaterWellPipeVerticalDN = "DN75";
        public string WaterWellPipeVerticalDN
        {
            get
            {
                return _WaterWellPipeVerticalDN;
            }
            set
            {
                _WaterWellPipeVerticalDN = value;
                RaisePropertyChanged();
            }
        }
        private string _CondensePipeDN = "DN50";
        public string CondensePipeVerticalDN
        {
            get
            {
                return _CondensePipeDN;
            }
            set
            {
                _CondensePipeDN = value;
                this.RaisePropertyChanged();
            }
        }
        private string _CondenseFloorDrainDN = "DN50";
        public string CondenseFloorDrainDN
        {
            get
            {
                return _CondenseFloorDrainDN;
            }
            set
            {
                _CondenseFloorDrainDN = value;
                this.RaisePropertyChanged();
            }
        }
        private string _CondensePipeHorizontalDN = "DN25";
        public string CondensePipeHorizontalDN
        {
            get
            {
                return _CondensePipeHorizontalDN;
            }
            set
            {
                _CondensePipeHorizontalDN = value;
                this.RaisePropertyChanged();
            }
        }

        private string _BalconyRainPipeDN = "DN100";
        public string BalconyRainPipeDN
        {
            get
            {
                return _BalconyRainPipeDN;
            }
            set
            {
                _BalconyRainPipeDN = value;
                this.RaisePropertyChanged();
            }
        }
        private string _WaterWellFloorDrainDN = "DN50";
        public string WaterWellFloorDrainDN
        {
            get
            {
                return _WaterWellFloorDrainDN;
            }
            set
            {
                _WaterWellFloorDrainDN = value;
                this.RaisePropertyChanged();
            }
        }
        private bool _HasAirConditionerFloorDrain = false;
        public bool HasAirConditionerFloorDrain
        {
            get
            {
                return _HasAirConditionerFloorDrain;
            }
            set
            {
                _HasAirConditionerFloorDrain = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _HasAiringForCondensePipe = false;
        public bool HasAiringForCondensePipe
        {
            get
            {
                return _HasAiringForCondensePipe;
            }

            set
            {
                _HasAiringForCondensePipe = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
