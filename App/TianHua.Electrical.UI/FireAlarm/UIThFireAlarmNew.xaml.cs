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

using ThMEPElectrical.FireAlarm.ViewModels;
using ThMEPElectrical.FireAlarm;
using ThMEPElectrical.FireAlarm.Commands;

namespace TianHua.Electrical.UI.FireAlarm
{
    /// <summary>
    /// UIThFireAlarmNew.xaml 的交互逻辑
    /// </summary>
    public partial class UIThFireAlarmNew : Window
    {
        private static FireAlarmNewViewModel vm = null;
        public UIThFireAlarmNew()
        {
            InitializeComponent();
            if (vm == null)
            {
                vm = new FireAlarmNewViewModel();
            }
            var value = FireAlarmSetting.Instance.Scale;
            this.DataContext = vm;


        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
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
            using (var cmd = new FireAlarmLayoutCommand())
            {
                cmd.Execute();
            }
        }

        public static void Save(FireAlarmNewViewModel localVM)
        {
            FireAlarmSetting.Instance.Scale = (double)localVM.ScaleItem.Tag;
            FireAlarmSetting.Instance.Beam = (int)localVM.Beam;
            FireAlarmSetting.Instance.LayoutItem = (int)localVM.LayoutItem;

            FireAlarmSetting.Instance.RoofHight = (int)localVM.RoofHight.Tag;
            FireAlarmSetting.Instance.RoofGrade = (int)localVM.RoofGrade.Tag;
            FireAlarmSetting.Instance.RoofThickness = localVM.RoofThickness;
            FireAlarmSetting.Instance.FixRef = (double)localVM.FixRef.Tag;

            FireAlarmSetting.Instance.BroadcastLayout = (int)localVM.BroadcastLayout;
            FireAlarmSetting.Instance.StepLengthBC = localVM.StepLengthBC * 1000;

            FireAlarmSetting.Instance.StepLengthMA = (double)localVM.StepLengthMA * 1000;
            FireAlarmSetting.Instance.ProtectRadius = (double)localVM.ProtectRadius;
            FireAlarmSetting.Instance.DisplayBuilding = (int)localVM.DisplayBuilding;
            FireAlarmSetting.Instance.DisplayBlk = (int)localVM.DisplayBlk;
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

    public class LayoutConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            LayoutItemType s = (LayoutItemType)value;
            return s == (LayoutItemType)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (LayoutItemType)int.Parse(parameter.ToString());
        }
    }
}
