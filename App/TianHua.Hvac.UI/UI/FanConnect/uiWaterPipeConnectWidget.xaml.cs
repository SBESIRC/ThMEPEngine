using AcHelper;
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
        public static void FocusMainWindow()
        {
#if ACAD_ABOVE_2014
            Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Focus();
#else
            FocusToCAD();
#endif
        }
        public static void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
        private void btnGeneraSPM_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            FocusMainWindow();
            var createSpm = new ThCreateSPMExtractCmd();
            createSpm.ConfigInfo = ViewModel.WaterPipeConfigInfo;
            createSpm.Execute();
        }

        private void btnUpdateSPM_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            FocusMainWindow();
            var updateSpmCmd = new ThUpdateSPMExtractCmd();
            updateSpmCmd.ConfigInfo = ViewModel.WaterPipeConfigInfo;
            updateSpmCmd.Execute();
        }

        private void btnConnectPipe_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            FocusMainWindow();
            var waterPipeConnectExtractCmd = new ThWaterPipeConnectExtractCmd();
            waterPipeConnectExtractCmd.ConfigInfo = ViewModel.WaterPipeConfigInfo;
            waterPipeConnectExtractCmd.Execute();
        }
        private void ThCustomWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
