using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Diagram.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// WaterSupplyHalfPlatformSetting.xaml 的交互逻辑
    /// </summary>
    public partial class WaterSupplyHalfPlatformSetting : ThCustomWindow
    {
        public HalfPlatformSetVM setViewModel { get; set; }
        public WaterSupplyHalfPlatformSetting(HalfPlatformSetVM viewModel = null)
        {
            InitializeComponent();
            this.Title = "半平台参数设置";
            setViewModel = viewModel;
            //orgViewModel = viewModel;
            if (null == viewModel)
                setViewModel = new HalfPlatformSetVM();
            this.DataContext = setViewModel;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (!base.CheckInputData())
            {
                MessageBox.Show("输入的数据有错误，请检查输入后在进行后续操作", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            this.DialogResult = true;
            this.Close();
        }

        private void Cancle_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
