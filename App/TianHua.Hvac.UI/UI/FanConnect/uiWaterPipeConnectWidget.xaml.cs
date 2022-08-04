using AcHelper;
using System.Windows.Controls;
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
        private void btnSelectRoom_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            FocusMainWindow();
            var pickRoomExtractCmd = new ThPickRoomExtractCmd();
            pickRoomExtractCmd.ConfigInfo = ViewModel.WaterPipeConfigInfo;
            pickRoomExtractCmd.Execute();
            ViewModel.RoomCount = "1";//仅触发页面刷新，不能实际赋值
        }

        private void SuppAddBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if(SuppLeftListBox.SelectedItem != null)
            {
                
                var selectedItem = (ListBoxItem)SuppLeftListBox.SelectedItem;
                string selectedValve = (string)selectedItem.Content;
                SuppRightListBox.Items.Add(selectedValve);
                ViewModel.WaterPipeConfigInfo.WaterValveConfigInfo.FeedPipeValves.Add(selectedValve);
            }
        }

        private void SuppCanBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SuppRightListBox.SelectedItem != null)
            {
                var selectedItem = (ListBoxItem)SuppRightListBox.SelectedItem;
                string selectedValve = (string)selectedItem.Content;
                SuppRightListBox.Items.Remove(SuppRightListBox.SelectedItem);
                ViewModel.WaterPipeConfigInfo.WaterValveConfigInfo.FeedPipeValves.Remove(selectedValve);
            }
        }

        private void BackAddBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (BackLeftListBox.SelectedItem != null)
            {
                var selectedItem = (ListBoxItem)BackLeftListBox.SelectedItem;
                string selectedValve = (string)selectedItem.Content;
                BackRightListBox.Items.Add(selectedValve);
                ViewModel.WaterPipeConfigInfo.WaterValveConfigInfo.ReturnPipeValeves.Add(selectedValve);
            }
        }

        private void BackCanBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (BackRightListBox.SelectedItem != null)
            {
                var selectedItem = (ListBoxItem)BackRightListBox.SelectedItem;
                string selectedValve = (string)selectedItem.Content;
                BackRightListBox.Items.Remove(BackRightListBox.SelectedItem);
                ViewModel.WaterPipeConfigInfo.WaterValveConfigInfo.ReturnPipeValeves.Remove(selectedValve);
            }
        }

        private void btnHelpSPM_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(@"http://thlearning.thape.com.cn/kng/course/package/video/3dc53d1443b04cda822db7046da629ac_6ae06ebd6c8f42178b0023d6095de1f6.html");
        }
    }
}
