using AcHelper;
using AcHelper.Commands;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using ThMEPLighting.ViewModel;
using ThMEPLighting.ServiceModels;
using System.Windows.Media;

namespace ThMEPLighting.UI.WiringConnecting
{
    /// <summary>
    /// UIEmgLightLayout.xaml 的交互逻辑
    /// </summary>
    public partial class ThWiringConnectingUI : Window
    {
        public ThWiringConnectingUI()
        {
            InitializeComponent();
            ThWiringSettingUI.settingVM = new WiringConnectingViewModel();
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
            base.Hide();
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            ThWiringSettingUI thWiringSettingUI = new ThWiringSettingUI();
            thWiringSettingUI.ShowDialog();
            this.Setting.Background = (Brush)(new SolidColorBrush(Color.FromRgb(85, 85, 85)));
        }

        private void btnConnectWiring_Click(object sender, RoutedEventArgs e)
        {
            if (this.fireAlarmSys.IsChecked == true)
            {
                ThMEPLightingService.Instance.AFASParameter = new List<ThFireAlarmModel>();
                ThMEPLightingService.Instance.AFASParameter = ThMEPLightingService.ConvertToFireAlarmModel(ThWiringSettingUI.settingVM.configLst);
                ThMEPLightingService.Instance.AvoidColumnChecked =ThWiringSettingUI.settingVM.AvoidColumnChecked.Value;

                //聚焦到CAD
                SetFocusToDwgView();

                CommandHandlerBase.ExecuteFromCommandLine(false, "THHZLX");
            }
            else if (this.lightingSys.IsChecked == true)
            {
                ThMEPLightingService.Instance.Parameter = new List<ThLigitingWiringModel>();
                ThMEPLightingService.Instance.Parameter = ThMEPLightingService.ConvertToLigitingWiringModel(ThWiringSettingUI.settingVM.configLst);

                //聚焦到CAD
                SetFocusToDwgView();

                CommandHandlerBase.ExecuteFromCommandLine(false, "THZMLX");
            }
        }

        private void btnUcsCompass_Click(object sender, RoutedEventArgs e)
        {
            // 发送命令
            SetFocusToDwgView();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THLXUCS");
        }

        /// <summary>
        /// 聚焦到CAD
        /// </summary>
        private void SetFocusToDwgView()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
    }
}
