using ThControlLibraryWPF.CustomControl;
using ThMEPHVAC.FanConnect.Command;
using ThMEPHVAC.FanConnect.ViewModel;

namespace TianHua.Hvac.UI.UI.FanConnect
{
    /// <summary>
    /// uiWaterPipeConnectWidget.xaml 的交互逻辑
    /// </summary>
    public partial class uiWaterPipeConnectWidget : ThCustomWindow
    {
        public uiWaterPipeConnectWidget()
        {
            InitializeComponent();
            this.DataContext = new ThWaterPipeViewModel();
        }
        public ThWaterPipeViewModel ViewModel
        {
            get
            {
                return this.DataContext as ThWaterPipeViewModel;
            }
        }

        private void btnGeneraSPM_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var createSpm = new ThCreateSPMExtractCmd();
            createSpm.ConfigInfo = ViewModel.WaterPipeConfigInfo;
            createSpm.Execute();
        }

        private void btnUpdateSPM_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void btnConnectPipe_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var waterPipeConnectExtractCmd = new ThWaterPipeConnectExtractCmd();
            waterPipeConnectExtractCmd.ConfigInfo = ViewModel.WaterPipeConfigInfo;
            waterPipeConnectExtractCmd.Execute();
        }
    }
}
