using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using AcHelper;
using AcHelper.Commands;
using Linq2Acad;

using ThMEPElectrical.AFAS.ViewModel;

namespace TianHua.Electrical.UI.FireAlarm
{
    /// <summary>
    /// UIThFireAlarmSetting.xaml 的交互逻辑
    /// </summary>
    public partial class UIThFireAlarmSetting : Window
    {
        private FireAlarmViewModel vm = null;
        public UIThFireAlarmSetting(FireAlarmViewModel viewModel)
        {
            InitializeComponent();
            this.vm = viewModel;
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

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            UIThFireAlarm.Save(vm);
            MessageBox.Show("保存成功！");
            this.Close();
        }


    }

    public class BroadcastConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BroadcastLayoutType s = (BroadcastLayoutType)value;
            return s == (BroadcastLayoutType)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (BroadcastLayoutType)int.Parse(parameter.ToString());
        }
    }

    public class DisplayBuildingTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DisplayBuildingType s = (DisplayBuildingType)value;
            return s == (DisplayBuildingType)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (DisplayBuildingType)int.Parse(parameter.ToString());
        }
    }

    public class DisplayBlkTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DisplayBlkType s = (DisplayBlkType)value;
            return s == (DisplayBlkType)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (DisplayBlkType)int.Parse(parameter.ToString());
        }
    }
}
