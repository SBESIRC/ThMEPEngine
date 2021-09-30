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
    }
}
