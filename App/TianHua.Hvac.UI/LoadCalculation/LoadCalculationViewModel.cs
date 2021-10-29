using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using ThMEPHVAC.LoadCalculation.Model;

namespace TianHua.Hvac.UI.LoadCalculation
{
    public class LoadCalculationViewModel : NotifyPropertyChangedBase
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

        public LoadCalculationViewModel()
        {
            DynamicModelData = new ObservableCollection<DynamicLoadCalculationModelData>();
        }
        public LoadCalculationViewModel(ConfigDataModel datamodel)
        {
            DynamicModelData = new ObservableCollection<DynamicLoadCalculationModelData>();
            
            foreach (var config in datamodel.Configs)
            {
                DynamicModelData.Add(config);
            }
        }
    }
    
}
