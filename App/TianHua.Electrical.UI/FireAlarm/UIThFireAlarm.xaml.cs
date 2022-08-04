using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;


using AcHelper;
using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.AFAS.Command;

namespace TianHua.Electrical.UI.FireAlarm
{
    /// <summary>
    /// UIThFireAlarmNew.xaml 的交互逻辑
    /// </summary>
    public partial class UIThFireAlarm : Window
    {
        private static FireAlarmViewModel vm = null;
        public UIThFireAlarm()
        {
            InitializeComponent();
            if (vm == null)
            {
                vm = new FireAlarmViewModel();
            }
            var value = FireAlarmSetting.Instance.Scale;
            this.DataContext = vm;

        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Save(vm);
            this.Close();
        }
        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            var UISetting = new UIThFireAlarmSetting(vm);
            UISetting.ShowDialog();
        }

        private void btnLayout_Click(object sender, RoutedEventArgs e)
        {
            if (Active.Document == null)
                return;

            Save(vm);
            FocusToCAD();
            using (var cmd = new ThAFASCommand())
            {
                cmd.Execute();
            }
        }

        private void cbSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (cbSelectAll.IsChecked == true)
            {
                cbSmoke.IsChecked = cbSmoke.IsEnabled == true ? true : false;
                cbGas.IsChecked = cbGas.IsEnabled == true ? true : false;
                cbDisplay.IsChecked = cbDisplay.IsEnabled == true ? true : false;
                cbMonitor.IsChecked = cbMonitor.IsEnabled == true ? true : false;
                cbTel.IsChecked = cbTel.IsEnabled == true ? true : false;

            }
            else
            {
                cbSmoke.IsChecked = false;
                cbGas.IsChecked = false;
                cbDisplay.IsChecked = false;
                cbMonitor.IsChecked = false;
                cbTel.IsChecked = false;
            }
        }

        private void cbSelectOther_Click(object sender, RoutedEventArgs e)
        {
            if (cbSmoke.IsEnabled == true)
                cbSmoke.IsChecked  = cbSmoke.IsChecked == true ? false : true;
            if (cbGas.IsEnabled == true)
                cbGas.IsChecked = cbGas.IsChecked == true ? false : true;
            if (cbDisplay.IsEnabled == true)
                cbDisplay.IsChecked  = cbDisplay.IsChecked == true ? false : true;
            if (cbMonitor.IsEnabled == true)
                cbMonitor.IsChecked = cbMonitor.IsChecked == true ? false : true;
            if (cbTel.IsEnabled == true)
                cbTel.IsChecked = cbTel.IsChecked == true ? false : true;
        }

        private void cbSelectFloorRoom_Click(object sender, RoutedEventArgs e)
        {
            if (vm.SelectFloorRoom == 1)
            {
                cbDisplay.IsChecked  = false;
                cbMonitor.IsChecked = false;
                cbTel.IsChecked = false;

                cbDisplay.IsEnabled = false;
                cbMonitor.IsEnabled = false;
                cbTel.IsEnabled = false;
            }
            else
            {
                cbDisplay.IsEnabled = true;
                cbMonitor.IsEnabled = true;
                cbTel.IsEnabled = true;
            }
        }
        public static void Save(FireAlarmViewModel localVM)
        {
            FireAlarmSetting.Instance.Scale = (double)localVM.ScaleItem.Tag;
            FireAlarmSetting.Instance.SelectFloorRoom = localVM.SelectFloorRoom;
            FireAlarmSetting.Instance.FloorUpDown = localVM.FloorUpDown;

            FireAlarmSetting.Instance.Beam = (int)localVM.Beam;
            FireAlarmSetting.Instance.RoofThickness = localVM.RoofThickness;
            FireAlarmSetting.Instance.BufferDist = localVM.BufferDist;

            FireAlarmSetting.Instance.RoofHight = (int)localVM.RoofHight.Tag;
            FireAlarmSetting.Instance.RoofGrade = (int)localVM.RoofGrade.Tag;
            FireAlarmSetting.Instance.FixRef = (double)localVM.FixRef.Tag;

            FireAlarmSetting.Instance.BroadcastLayout = (int)localVM.BroadcastLayoutType;
            FireAlarmSetting.Instance.StepLengthBC = localVM.StepLengthBC * 1000;

            FireAlarmSetting.Instance.StepLengthMA = (double)localVM.StepLengthMA * 1000;
            FireAlarmSetting.Instance.GasProtectRadius = (double)localVM.GasProtectRadius;
            FireAlarmSetting.Instance.DisplayBuilding = (int)localVM.DisplayBuilding;
            FireAlarmSetting.Instance.DisplayBlk = (int)localVM.DisplayBlk;

            SaveLayout(localVM);
        }

        void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
                    Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }

        private static void SaveLayout(FireAlarmViewModel localVM)
        {
            FireAlarmSetting.Instance.LayoutItemList.Clear();

            if (localVM.LayoutSmoke == true)
            {
                FireAlarmSetting.Instance.LayoutItemList.Add((int)ThFaCommon.LayoutItemType.Smoke);
            }
            if (localVM.LayoutBroadcast == true)
            {
                FireAlarmSetting.Instance.LayoutItemList.Add((int)ThFaCommon.LayoutItemType.Broadcast);
            }
            if (localVM.LayoutDisplay == true)
            {
                FireAlarmSetting.Instance.LayoutItemList.Add((int)ThFaCommon.LayoutItemType.Display);
            }
            if (localVM.LayoutTel == true)
            {
                FireAlarmSetting.Instance.LayoutItemList.Add((int)ThFaCommon.LayoutItemType.Tel);
            }
            if (localVM.LayoutGas == true)
            {
                FireAlarmSetting.Instance.LayoutItemList.Add((int)ThFaCommon.LayoutItemType.Gas);
            }
            if (localVM.LayoutManualAlart == true)
            {
                FireAlarmSetting.Instance.LayoutItemList.Add((int)ThFaCommon.LayoutItemType.ManualAlarm);
            }
            if (localVM.LayoutMonitor == true)
            {
                FireAlarmSetting.Instance.LayoutItemList.Add((int)ThFaCommon.LayoutItemType.Monitor);
            }

            FireAlarmSetting.Instance.LayoutItemList = FireAlarmSetting.Instance.LayoutItemList.OrderBy(x => x).ToList();

        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(@"http://thlearning.thape.com.cn/kng/view/video/6273a9f6e8174f619c6adcd1fa91c440.html");
        }

    }

    public class BeamConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BeamType s = (BeamType)value;
            return s == (BeamType)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (BeamType)int.Parse(parameter.ToString());
        }
    }

    public class TrueFalseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = (int)value;
            return s == int.Parse(parameter.ToString());
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return int.Parse(parameter.ToString());
        }
    }
}
