using ThControlLibraryWPF.ControlUtils;

namespace ThMEPWSS.ViewModel
{
    public class ThTianHuaUserConfigVM : NotifyPropertyChangedBase
    {
        public ThTianHuaUserConfigVM()
        {
            beamSourceSwitch = "协同";
        }

        string beamSourceSwitch = "";
        public string BeamSourceSwitch
        {
            get => beamSourceSwitch;
            set
            {
                beamSourceSwitch = value;
                OnPropertyChanged(nameof(BeamSourceSwitch));
            }
        }
    }
}
