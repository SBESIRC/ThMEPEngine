using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.Project.Module
{
    public class ThPDSMiniBusbarModel : NotifyPropertyChangedBase
    {
        private readonly MiniBusbar _miniBusbar;

        public ThPDSMiniBusbarModel(MiniBusbar busbar)
        {
            _miniBusbar = busbar;
        }

        [DisplayName("功率")]
        [Category("小母排参数")]
        public double Power
        {
            get => _miniBusbar.Power;
            set
            {
                _miniBusbar.Power = value;
                OnPropertyChanged(nameof(Power));
            }
        }
    }
}
