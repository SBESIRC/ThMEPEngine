using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using ThMEPElectrical.ElectricalLoadCalculation;

namespace TianHua.Electrical.UI.ElectricalLoadCalculation
{
    public class ElectricalLoadCalculationViewModel : NotifyPropertyChangedBase
    {
        public ObservableCollection<DynamicLoadCalculationModelData> DynamicModelData
        {
            get { return dynamicModelData; }
            set
            {
                dynamicModelData = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<DynamicLoadCalculationModelData> dynamicModelData;

        public ElectricalLoadCalculationViewModel()
        {
            DynamicModelData = new ObservableCollection<DynamicLoadCalculationModelData>();
        }
        public ElectricalLoadCalculationViewModel(ElectricalConfigDataModel datamodel)
        {
            DynamicModelData = new ObservableCollection<DynamicLoadCalculationModelData>();

            foreach (var config in datamodel.Configs)
            {
                DynamicModelData.Add(config);
            }
        }
    }
}
