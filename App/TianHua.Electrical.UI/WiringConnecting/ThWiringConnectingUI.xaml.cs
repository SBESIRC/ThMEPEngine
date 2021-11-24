using AcHelper;
using AcHelper.Commands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ThControlLibraryWPF.CustomControl;
using ThMEPElectrical.Model;
using ThMEPElectrical.Service;
using ThMEPElectrical.ViewModel;
using ThMEPLighting;
using ThMEPLighting.ServiceModels;

namespace TianHua.Electrical.UI.WiringConnecting
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
            this.Close();
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            ThWiringSettingUI thWiringSettingUI = new ThWiringSettingUI();
            thWiringSettingUI.ShowDialog();
        }

        private void btnConnectWiring_Click(object sender, RoutedEventArgs e)
        {
            if (this.fireAlarmSys.IsChecked == true)
            {
                ThElectricalUIService.Instance.fireAlarmParameter = new List<ThFireAlarmModel>();
                ThElectricalUIService.Instance.fireAlarmParameter = ThElectricalUIService.ConvertToModel(ThWiringSettingUI.settingVM.configLst);

                //聚焦到CAD
                SetFocusToDwgView();

                CommandHandlerBase.ExecuteFromCommandLine(false, "THHZLX");
            }
            else if (this.lightingSys.IsChecked == true)
            {
                ThMEPLightingService.Instance.Parameter = new List<ThLigitingWiringModel>();
                ThMEPLightingService.Instance.Parameter = ThMEPLightingService.ConvertToModel(ThWiringSettingUI.settingVM.configLst);

                //聚焦到CAD
                SetFocusToDwgView();

                CommandHandlerBase.ExecuteFromCommandLine(false, "THZMLX");
            }
            this.Hide();
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
    public class BlkTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
