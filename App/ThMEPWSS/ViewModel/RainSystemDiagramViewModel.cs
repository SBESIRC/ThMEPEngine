using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ThControlLibraryWPF.ControlUtils;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Pipe;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.Uitl;

namespace ThMEPWSS.Diagram.ViewModel
{
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
                    ThRainSystemService.InitFloorListDatas();
                }
                catch(System.Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }

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

        private bool _HasAirConditionerFloorDrain = true;
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

        private bool _HasAiringForCondensePipe = true;
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
