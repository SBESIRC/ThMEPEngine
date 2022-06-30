using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public class THPDSLoadCalculationVM : NotifyPropertyChangedBase
    {
        public THPDSLoadCalculationVM()
        {
            var firstSubstation = Project.PDSProjectVM.Instance.LoadCalculationViewModel.Substations.FirstOrDefault();
            this.LoadCalculationRowItems = CreatLoadCalculationRowItems(firstSubstation);
        }
        private ObservableCollection<THPDSLoadCalculationRowVM> _loadCalculationRowItems;
        public ObservableCollection<THPDSLoadCalculationRowVM> LoadCalculationRowItems
        {
            get { return _loadCalculationRowItems; }
            set
            {
                _loadCalculationRowItems = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<THPDSLoadCalculationRowVM> CreatLoadCalculationRowItems(THPDSProjectSubstation firstSubstation)
        {
            ObservableCollection<THPDSLoadCalculationRowVM> results = new ObservableCollection<THPDSLoadCalculationRowVM>();
            if(!firstSubstation.IsNull())
            {
                var transformers = firstSubstation.Transformers;
                foreach(var transformer in transformers)
                {
                    var mapInfos = Project.PDSProjectVM.Instance.LoadCalculationViewModel.SubstationMap.GetMapInfos(firstSubstation, transformer);
                    foreach (var mapInfo in mapInfos)
                    {
                        if(mapInfo.Node.Details.LoadCalculationInfo.IsDualPower)
                        {
                            results.Add(new THPDSLoadCalculationRowVM(mapInfo, true, transformers));
                            results.Add(new THPDSLoadCalculationRowVM(mapInfo, false, transformers));
                        }
                    }
                }
            }
            return results;
        }
    }

    public class THPDSLoadCalculationRowVM : NotifyPropertyChangedBase
    {
        private SubstationMapInfo _mapInfo;
        private List<THPDSProjectTransformer> _transformers;
        private bool IsLow;
        public THPDSLoadCalculationRowVM(SubstationMapInfo mapInfo, bool isNomal, List<THPDSProjectTransformer> transformers)
        {
            this._mapInfo=mapInfo;
            this._transformers=transformers;
            IsLow = _mapInfo.Node.Details.LoadCalculationInfo.IsDualPower && isNomal;
        }

        [DisplayName("负载编号")]
        public string LoadID
        {
            get => _mapInfo.Node.Load.ID.LoadID;
        }

        [DisplayName("功能描述")]
        public string Description
        {
            get => _mapInfo.Node.Load.ID.Description;
            set
            {
                _mapInfo.Node.Load.ID.Description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        [DisplayName("Pn")]
        public double Power
        {
            get => IsLow ? _mapInfo.Node.Details.LoadCalculationInfo.LowPower : _mapInfo.Node.Details.LoadCalculationInfo.HighPower;
            set
            {
                if(IsLow)
                {
                    _mapInfo.Node.Details.LoadCalculationInfo.LowPower = value;
                }
                else
                {
                    _mapInfo.Node.Details.LoadCalculationInfo.HighPower = value;
                }
                OnPropertyChanged(nameof(Power));
            }
        }

        [DisplayName("Kx")]
        public double DemandFactor
        {
            get => IsLow ? _mapInfo.Node.Details.LoadCalculationInfo.LowDemandFactor : _mapInfo.Node.Details.LoadCalculationInfo.HighDemandFactor;
            set
            {
                if (IsLow)
                {
                    _mapInfo.Node.Details.LoadCalculationInfo.LowDemandFactor = value;
                }
                else
                {
                    _mapInfo.Node.Details.LoadCalculationInfo.HighDemandFactor = value;
                }
                OnPropertyChanged(nameof(DemandFactor));
            }
        }

        [DisplayName("cosφ")]
        public double PowerFactor
        {
            get => _mapInfo.Node.Details.LoadCalculationInfo.PowerFactor;
            set
            {
                _mapInfo.Node.Details.LoadCalculationInfo.PowerFactor = value;
                OnPropertyChanged(nameof(PowerFactor));
            }
        }

        [DisplayName("Pc")]
        public double ActivePower
        {
            get => IsLow ? _mapInfo.Node.Details.LoadCalculationInfo.LowActivePower : _mapInfo.Node.Details.LoadCalculationInfo.HighActivePower;
        }

        [DisplayName("Qc")]
        public double ReactivePower
        {
            get => IsLow ? _mapInfo.Node.Details.LoadCalculationInfo.LowReactivePower : _mapInfo.Node.Details.LoadCalculationInfo.HighReactivePower;
        }

        [DisplayName("Sc")]
        public double ApparentPower
        {
            get => IsLow ? _mapInfo.Node.Details.LoadCalculationInfo.LowApparentPower : _mapInfo.Node.Details.LoadCalculationInfo.HighApparentPower;
        }

        [DisplayName("Ic")]
        public double CalculateCurrent
        {
            get => IsLow ? _mapInfo.Node.Details.LoadCalculationInfo.LowCalculateCurrent : _mapInfo.Node.Details.LoadCalculationInfo.HighCalculateCurrent;
        }

        //工作场景
        //负荷等级
        [DisplayName("负荷等级")]
        public LoadCalculationGrade LoadCalculationGrade
        {
            get => _mapInfo.Node.Details.LoadCalculationInfo.LoadCalculationGrade;
            set
            {
                _mapInfo.Node.Details.LoadCalculationInfo.LoadCalculationGrade = value;
                OnPropertyChanged(nameof(LoadCalculationGrade));
            }
        }
        //主用备用

        //变压器
        //_mapInfo.Transformer
    }
}
