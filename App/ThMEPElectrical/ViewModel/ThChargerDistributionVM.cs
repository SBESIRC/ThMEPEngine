using System.Collections.ObjectModel;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPElectrical.ViewModel
{
    public class ThChargerDistributionVM : NotifyPropertyChangedBase
    {
        private ObservableCollection<MultiCheckItem> _pickLayerNames = new ObservableCollection<MultiCheckItem>();
        public ObservableCollection<MultiCheckItem> PickLayerNames
        {
            get { return _pickLayerNames; }
            set
            {
                _pickLayerNames = value;
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<MultiCheckItem> _pickBlockNames = new ObservableCollection<MultiCheckItem>();
        public ObservableCollection<MultiCheckItem> PickBlockNames
        {
            get { return _pickBlockNames; }
            set
            {
                _pickBlockNames = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
