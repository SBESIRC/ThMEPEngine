using System;
using ThControlLibraryWPF.ControlUtils;
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ThMEPWSS.ViewModel
{
    public class ThTianHuaUserConfigVM : NotifyPropertyChangedBase
    {
        public ThTianHuaUserConfigVM()
        {
            Load();
        }

        BeamRecognizeSource beamSourceSwitch = BeamRecognizeSource.DB; 
        public BeamRecognizeSource BeamSourceSwitch
        {
            get => beamSourceSwitch;
            set
            {
                beamSourceSwitch = value;
                OnPropertyChanged(nameof(BeamSourceSwitch));
            }
        }

        private void Load()
        {
            if (Convert.ToInt16(acadApp.GetSystemVariable("USERR1")) == 0)
            {
                beamSourceSwitch = BeamRecognizeSource.DB;
            }
            else if (Convert.ToInt16(acadApp.GetSystemVariable("USERR1")) == 1)
            {
                beamSourceSwitch = BeamRecognizeSource.Layer;
            }
        }
    }
    public enum BeamRecognizeSource
    {
        DB=0,
        Layer=1,
    }
}
