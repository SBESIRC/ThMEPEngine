using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ThControlLibraryWPF.ControlUtils;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Pipe;
using ThMEPWSS.Uitl;

namespace ThMEPWSS.Diagram.ViewModel
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

        public void InitFloorListDatas()
        {
            // 绑定控件
            if (DateTime.Now == DateTime.MinValue) FloorListDatas = SystemDiagramUtils.GetFloorListDatas();
            else
            {
                try
                {
                    ThMEPWSS.ReleaseNs.RainSystemNs.ThRainService.CollectFloorListDatasEx();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
    [Serializable]
    public class DrainageSystemDiagramViewModel : NotifyPropertyChangedBase
    {
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

        public void CollectFloorListDatas()
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
                    ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.CollectFloorListDatasEx();
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

        private string _WashingMachineFloorDrainDN = "DN75";
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
        private bool _CouldHavePeopleOnRoof = false;
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

        private string _BalconyFloorDrainDN = "DN25";
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
        private string _CondensePipeDN = "DN25";
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

        private string _BalconyRainPipeDN = "DN25";
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
        private bool _CouldHavePeopleOnRoof = false;
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
