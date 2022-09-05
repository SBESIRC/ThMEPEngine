using System;
using System.Windows;
using System.Diagnostics;
using System.Windows.Data;
using System.Globalization;

using AcHelper.Commands;

using ThMEPWSS.ViewModel;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.HydrantConnectPipe.Command;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiHydrantConnectPipe.xaml 的交互逻辑
    /// </summary>
    public partial class UiHydrantConnectPipe : ThCustomWindow
    {
        private static HydrantConnectPipeViewModel ViewModel = null;
        private static bool _rbtChecked = false;
        public UiHydrantConnectPipe()
        {
            InitializeComponent();
            if (ViewModel == null)
            {
                ViewModel = new HydrantConnectPipeViewModel();
            }

            DataContext = ViewModel;
        }

        private void btnConnectPipe_Click(object sender, RoutedEventArgs e)
        {
            ThHydrantConnectPipeConnectCmd.ConfigInfo = ViewModel.GetConfigInfo();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THFHPC");
        }

        private void btnMarkLoop_Click(object sender, RoutedEventArgs e)
        {
            FireHydrantSystemViewModel.InsertLoopMark();
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://thlearning.thape.com.cn/kng/view/video/693b4adf25cc42e5b64d0a4c89507bf5.html");
        }

        private void rbtTCH_Checked(object sender, RoutedEventArgs e)
        {
            rbt_Checked();
        }

        private void rbtByMainRing_Checked(object sender, RoutedEventArgs e)
        {
            rbt_Checked();
        }

        private void rbt_Checked()
        {
            if (!_rbtChecked)
            {
                var showMsg = "本功能在未安装专版天正及参数化数据库补丁包的情况下会导致CAD崩溃，请确保已具备使用条件。";
                var result = MessageBox.Show(showMsg, "天华-警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (result != MessageBoxResult.OK)
                {
                    rbtCAD.IsChecked = true;
                }
                _rbtChecked = true;
            }
        }
    }

    public class OutputOpsBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = (OutputType)value;
            return s == (OutputType)int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return (OutputType)int.Parse(parameter.ToString());
        }
    }
}
