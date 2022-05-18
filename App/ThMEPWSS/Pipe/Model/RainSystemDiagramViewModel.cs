using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using ThControlLibraryWPF.ControlUtils;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Uitl;

namespace ThMEPWSS.Pipe.Model
{

    [Serializable]
    public class BaseClone<T>
    {
        public virtual T Clone()
        {
            var memoryStream = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(memoryStream, this);
            memoryStream.Position = 0;
            return (T)formatter.Deserialize(memoryStream);
        }
    }

    [Serializable]
    public class RainSystemDiagramViewModel : NotifyPropertyChangedBase
    {
        public static readonly RainSystemDiagramViewModel Singleton = new RainSystemDiagramViewModel();
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

        private RainSystemDiagramParamsViewModel _Params = new RainSystemDiagramParamsViewModel();
        public RainSystemDiagramParamsViewModel Params
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

        public void InitFloorListDatas(bool focus)
        {
            // 绑定控件
            if (DateTime.Now == DateTime.MinValue) FloorListDatas = SystemDiagramUtils.GetFloorListDatas();
            else
            {
                try
                {
                    ThMEPWSS.ReleaseNs.RainSystemNs.ThRainService.CollectFloorListDatasEx(focus);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
    [Serializable]
    public class RainSystemDiagramParamsViewModel : NotifyPropertyChangedBase
    {
        public void CopyTo(RainSystemDiagramParamsViewModel other)
        {
            foreach (var pi in GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                pi.SetValue(other, pi.GetValue(this));
            }
        }
        public RainSystemDiagramParamsViewModel Clone()
        {
            return this.ToJson().FromJson<RainSystemDiagramParamsViewModel>();
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
    }
}
