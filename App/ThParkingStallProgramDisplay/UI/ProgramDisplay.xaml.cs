using Serilog.Core;
using ThControlLibraryWPF.CustomControl;
using ThParkingStallProgramDisplay.ViewModel;

namespace ThParkingStallProgramDisplay.UI
{
    /// <summary>
    /// ProgramDisplay.xaml 的交互逻辑
    /// </summary>
    public partial class ProgramDisplay : ThCustomWindow
    {
        static ParkingStallDisplayViewModel _ViewModel = null;
        public ProgramDisplay(Logger logger)
        {
            logger?.Information("进入UI");
            if(_ViewModel == null)
            {
                _ViewModel = new ParkingStallDisplayViewModel();
            }
            DataContext = _ViewModel;
            InitializeComponent();

            logger?.Information("UI结束");
        }

    }
}
