using System.Collections.ObjectModel;
using ThControlLibraryWPF.ControlUtils;
using ThMEPHVAC.IndoorFanModels;

namespace TianHua.Hvac.UI.ViewModels
{
    public class FanDataShowViewModel : NotifyPropertyChangedBase
    {
        public FanDataShowViewModel()
        {
            this.FanInfos = new ObservableCollection<IndoorFanBase>();
        }
        private ObservableCollection<IndoorFanBase> _fanInfos;
        public ObservableCollection<IndoorFanBase> FanInfos
        {
            get { return _fanInfos; }
            set
            {
                _fanInfos = value;
                this.RaisePropertyChanged();
            }
        }
        private IndoorFanBase _selectFan { get; set; }
        public IndoorFanBase SelectFanData
        {
            get { return _selectFan; }
            set
            {
                _selectFan = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
