using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.WaterSupplyPipeSystem.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// WaterSupplyRoofTank.xaml 的交互逻辑
    /// </summary>
    public partial class WaterSupplyRoofTank : ThCustomWindow
    {
        public RoofTankVM setViewModel { get; set; }

        public WaterSupplyRoofTank(RoofTankVM viewModel = null, int maxFloor=1)
        {
            InitializeComponent();
            this.Title = "屋顶水箱参数设置";
            setViewModel = viewModel;
            if (null == viewModel)
                setViewModel = new RoofTankVM(maxFloor);
            this.DataContext = setViewModel;
        }

        private void btnOk(object sender, RoutedEventArgs e)
        {
            if (!base.CheckInputData())
            {
                MessageBox.Show("输入的数据有错误，请检查输入后在进行后续操作", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            double l, w, h;
            try
            {
                l = Convert.ToDouble(length.Text);
            }
            catch
            {
                l = 0;
            }
            try
            {
                w = Convert.ToDouble(width.Text);
            }
            catch
            {
                w = 0;
            }
            try
            {
                h = Convert.ToDouble(height.Text);
            }
            catch
            {
                h = 0;
            }
          
            volum.Text = Convert.ToString( l* w * (h - 0.6));
        }

        private void KeyPress_TankLength(object sender, KeyEventArgs e)
        {
            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || (e.Key >= Key.D0 && e.Key <= Key.D9) || e.Key == Key.OemPeriod)
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void TextChanged_TankLength(object sender, TextChangedEventArgs e)
        {
            var str = ((TextBox)e.Source).Text.ToString();
            if (string.IsNullOrEmpty(str))
                return;
            var charArrs = str.ToCharArray();
            var newStr = "";
            foreach (var item in charArrs)
            {
                if (newStr.Contains('.'))
                {
                    if (item == '5' || item == '0')
                    {
                        newStr += item;
                    }
                    break;
                }
                if (item >= '0' && item <= '9')
                {
                    newStr += item;
                }
                if (item == '.' && !newStr.Contains('.'))
                {
                    newStr += item;
                }

            }
            ((TextBox)e.Source).Text = newStr;
        }

        private void KeyPress_Floor(object sender, KeyEventArgs e)
        {
            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || (e.Key >= Key.D0 && e.Key <= Key.D9))
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void TextChanged_Floor(object sender, TextChangedEventArgs e)
        {
            var str = ((TextBox)e.Source).Text.ToString();
            if (string.IsNullOrEmpty(str))
                return;
            var charArrs = str.ToCharArray();
            var newStr = "";
            var flag = false;
            foreach (var item in charArrs)
            {
                if(flag && item == '0')
                {
                    newStr += item;
                }
                if (item >= '1' && item <= '9')
                {
                    flag = true;
                    newStr += item;
                }
            }
            ((TextBox)e.Source).Text = newStr;
        }
    }
}
