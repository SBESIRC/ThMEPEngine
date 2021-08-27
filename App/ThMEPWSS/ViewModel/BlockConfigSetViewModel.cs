using System.Collections.ObjectModel;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPWSS.ViewModel
{
    public class BlockConfigSetViewModel : NotifyPropertyChangedBase
    {
        public string BlockName { get; set; }
        private ObservableCollection<BlockNameConfigViewModel> _configList { get; set; }
        public ObservableCollection<BlockNameConfigViewModel> ConfigList 
        {
            get { return _configList; }
            set 
            {
                _configList = value;
                this.RaisePropertyChanged("ConfigList");
            }
        }
        public BlockConfigSetViewModel()
        {
            this.ConfigList = new ObservableCollection<BlockNameConfigViewModel>();
        }
        public BlockConfigSetViewModel Clone()
        {
            var cloned = new BlockConfigSetViewModel();
            cloned.BlockName = (string)(BlockName?.Clone());
            foreach(var config in _configList)
            {
                var configClone = new BlockNameConfigViewModel((string)(config.layerName?.Clone()));
                cloned.ConfigList.Add(configClone);
            }
            return cloned;
        }
    }
    public class BlockNameConfigViewModel : NotifyPropertyChangedBase
    {
        public int order { get; set; }
        private string _layerName { get; set; }
        public string layerName
        {
            get { return _layerName; }
            set 
            {
                _layerName = value;
                this.RaisePropertyChanged("layerName");
            }
        }
        public BlockNameConfigViewModel(string layerName) 
        {
            this.layerName = layerName;
        }
    }

}
