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
using ThMEPLighting;
using ThMEPLighting.ServiceModels;
using TianHua.Electrical.UI.WiringConnecting.ViewModel;

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
                var fireAlarmModel = ThWiringSettingUI.settingVM.configLst.Where(x => x.systemType == "火灾自动报警").FirstOrDefault();
                if (fireAlarmModel == null)
                {
                    return;
                }
                foreach (var model in fireAlarmModel.configModels)
                {
                    ThFireAlarmModel config = new ThFireAlarmModel();
                    config.loopType = model.loopType;
                    config.layerType = model.layerType;
                    config.pointNum = model.pointNum;
                    ThElectricalUIService.Instance.fireAlarmParameter.Add(config);
                }

                //聚焦到CAD
                SetFocusToDwgView();

                CommandHandlerBase.ExecuteFromCommandLine(false, "THHZLX");
            }
            else if (this.lightingSys.IsChecked == true)
            {
                ThMEPLightingService.Instance.Parameter = new List<ThLigitingWiringModel>();
                var lightingModel = ThWiringSettingUI.settingVM.configLst.Where(x => x.systemType == "照明").FirstOrDefault();
                if (lightingModel == null)
                {
                    return;
                }
                foreach (var model in lightingModel.configModels)
                {
                    ThLigitingWiringModel config = new ThLigitingWiringModel();
                    config.loopType = model.loopType;
                    config.layerType = model.layerType;
                    config.pointNum = model.pointNum;
                    ThMEPLightingService.Instance.Parameter.Add(config);
                }

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
