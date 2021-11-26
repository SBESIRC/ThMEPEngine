using System.Windows;
using System.Windows.Input;
using ThMEPElectrical.ViewModel;

namespace ThMEPLighting.UI.WiringConnecting
{
    /// <summary>
    /// UIEmgLightLayout.xaml 的交互逻辑
    /// </summary>
    public partial class ThWiringSettingUI : Window
    {
        public static WiringConnectingViewModel settingVM = null;
        public ThWiringSettingUI()
        {
            InitializeComponent();
            if (settingVM == null)
            {
                settingVM = new WiringConnectingViewModel();
            }
            this.DataContext = settingVM;
        }
    
        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            settingVM.UpdateDataSource();
            MessageBox.Show("保存成功！");
            this.Close();
        }
    }
}
