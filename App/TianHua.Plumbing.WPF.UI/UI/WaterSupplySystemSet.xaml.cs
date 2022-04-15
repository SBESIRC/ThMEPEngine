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
    /// uiDrainageSystemSet.xaml 的交互逻辑
    /// </summary>
    public partial class WaterSupplySystemSet : ThCustomWindow
    {
        public WaterSupplySetVM setViewModel;
        //private DrainageSetViewModel orgViewModel;
        public WaterSupplySystemSet( WaterSupplySetVM viewModel = null)
        {
            InitializeComponent();
            this.Title = "参数设置";
            setViewModel = viewModel;
            //orgViewModel = viewModel;
            if (null == viewModel)
                setViewModel = new WaterSupplySetVM();
            this.DataContext = setViewModel;
        }

        private void Cancle_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            //输入框数据校验
            if (!base.CheckInputData())
            {
                MessageBox.Show("输入的数据有错误，请检查输入后在进行后续操作", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            this.DialogResult = true;
            this.Close();
        }
      
        /// <summary>
        /// 楼层线间距
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_TextChanged_FloorGap(object sender, TextChangedEventArgs e)
        {
            var str = ((TextBox)e.Source).Text.ToString();
            if (string.IsNullOrEmpty(str))
                return;
            var charArrs = str.ToCharArray();
            var newStr = "";
            foreach (var item in charArrs)
            {
                if (item >= '0' && item <= '9')
                {
                    newStr += item;
                }
            }
            ((TextBox)e.Source).Text = newStr;
        }

        private void FloorLineGap_KeyPress(object sender, KeyEventArgs e)//输入键值判断，只能输入 0 到 9
        {
            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)  || (e.Key >= Key.D0 && e.Key <= Key.D9))
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)//光标丢失时，楼层线间距输入值判断
        {
            TextBox txtBox = sender as TextBox;
            string strText = txtBox.Text;
            if(strText.Length >= 5)
                (sender as TextBox).Text = Convert.ToString(9999);
            else
            {
                int max = 9999;
                int min = 1800;
                int number = int.Parse(strText);
                if (number < min)
                    (sender as TextBox).Text = Convert.ToString(min);
                else if (number > max)
                    (sender as TextBox).Text = Convert.ToString(max);
                else
                    e.Handled = false;
            }
        }

        private void FlushFaucet_KeyPress(object sender, KeyEventArgs e)
        {
            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || 
                (e.Key >= Key.D0 && e.Key <= Key.D9) || 
                 e.Key == Key.OemComma || e.Key == Key.OemMinus)
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void TextBox_TextChanged_Faucet(object sender, TextChangedEventArgs e)
        {
            var str = ((TextBox)e.Source).Text.ToString();
            if (string.IsNullOrEmpty(str))
                return;
            var charArrs = str.ToCharArray();
            var newStr = "";
            foreach (var item in charArrs)
            {
                if (item >= '0' && item <= '9')
                {
                    if(item == '0' && newStr.Length > 0)
                    {
                        if(newStr.Last() != '0' && newStr.Last() != '-' && newStr.Last() != ',')
                        {
                            newStr += item;
                        }
                    }
                    else
                    {
                        newStr += item;
                    }
                    
                }
                if(item == '-' || item == ',')
                {
                    newStr += item;
                }

            }

            ((TextBox)e.Source).Text = newStr;

        }

        private void NoCheckValve_KeyPress(object sender, KeyEventArgs e)
        {
            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || 
                (e.Key >= Key.D0 && e.Key <= Key.D9) || 
                 e.Key == Key.OemComma || e.Key == Key.OemMinus)
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void TextBox_TextChanged_NoCheckValve(object sender, TextChangedEventArgs e)
        {
            var str = ((TextBox)e.Source).Text.ToString();
            if (string.IsNullOrEmpty(str))
            {
                return;
            }
            var charArrs = str.ToCharArray();
            var newStr = "";
            foreach (var item in charArrs)
            {
                if (item >= '0' && item <= '9')
                {
                    if (item == '0' && newStr.Length > 0)
                    {
                        if (newStr.Last() != '0' && newStr.Last() != '-' && newStr.Last() != ',')
                        {
                            newStr += item;
                        }
                    }
                    else
                    {
                        newStr += item;
                    }

                }
                if (item == '-' || item == ',')
                {
                    newStr += item;
                }
            }

            ((TextBox)e.Source).Text = newStr;
        }

        private void LostFocus_MaxDayQuota(object sender, RoutedEventArgs e)
        {
            TextBox txtBox = sender as TextBox;

            string strText = txtBox.Text;
            if (strText.Length >= 5)
                (sender as TextBox).Text = Convert.ToString(320);
            else
            {
                int max = 320;
                int min = 130;

                int number = int.Parse(strText);
                if (number < min)
                    (sender as TextBox).Text = Convert.ToString(min);
                else if (number > max)
                    (sender as TextBox).Text = Convert.ToString(max);
                else
                    e.Handled = false;
            }
        }

        private void KeyPress_MaxDayQuota(object sender, KeyEventArgs e)
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

        private void TextChanged_MaxDayQuota(object sender, TextChangedEventArgs e)
        {
            var str = ((TextBox)e.Source).Text.ToString();
            if (string.IsNullOrEmpty(str))
                return;
            var charArrs = str.ToCharArray();
            var newStr = "";
            foreach (var item in charArrs)
            {
                if (item >= '0' && item <= '9')
                {
                    newStr += item;
                }
            }
            ((TextBox)e.Source).Text = newStr;
        }

        private void LostFocus_MaxDayHourCoefficient(object sender, RoutedEventArgs e)
        {
            TextBox txtBox = sender as TextBox;
            
            if ((sender as TextBox).Text.Contains('.'))
            {
                (sender as TextBox).Text = Convert.ToDouble((sender as TextBox).Text).ToString("#0.0");
            }
            string strText = txtBox.Text;
            if (strText.Length >= 5)
                (sender as TextBox).Text = Convert.ToString(2.8);
            else
            {
                double max = 2.8;
                double min = 2.0;

                double number = Convert.ToDouble(strText);
                if (number < min)
                    (sender as TextBox).Text = Convert.ToString(min);
                else if (number > max)
                    (sender as TextBox).Text = Convert.ToString(max);
                else
                    e.Handled = false;
            }
        }

        private void KeyPress_MaxDayHourCoefficient(object sender, KeyEventArgs e)
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

        private void TextChanged_MaxDayHourCoefficient(object sender, TextChangedEventArgs e)
        {
            var str = ((TextBox)e.Source).Text.ToString();
            if (string.IsNullOrEmpty(str))
                return;
            var charArrs = str.ToCharArray();
            var newStr = "";
            foreach (var item in charArrs)
            {
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

        private void LostFocus_NumberOfHouseholds(object sender, RoutedEventArgs e)
        {
            TextBox txtBox = sender as TextBox;

            if ((sender as TextBox).Text.Contains('.'))
            {
                (sender as TextBox).Text = Convert.ToDouble((sender as TextBox).Text).ToString("#0.0");
            }
            string strText = txtBox.Text;
            if (strText.Length >= 5)
                (sender as TextBox).Text = Convert.ToString(6);
            else
            {
                double max = 6;
                double min = 1;

                double number = Convert.ToDouble(strText);
                if (number < min)
                    (sender as TextBox).Text = Convert.ToString(min);
                else if (number > max)
                    (sender as TextBox).Text = Convert.ToString(max);
                else
                    e.Handled = false;
            }
        }

        private void KeyPress_NumberOfHouseholds(object sender, KeyEventArgs e)
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

        private void TextChanged_NumberOfHouseholds(object sender, TextChangedEventArgs e)
        {
            var str = ((TextBox)e.Source).Text.ToString();
            if (string.IsNullOrEmpty(str))
                return;
            var charArrs = str.ToCharArray();
            var newStr = "";
            foreach (var item in charArrs)
            {
                if (item >= '0' && item <= '9')
                {
                    newStr += item;
                }
                if(item == '.' && !newStr.Contains('.'))
                {
                    newStr += item;
                }
            }
            ((TextBox)e.Source).Text = newStr;
        }

        private void HalfPlatformSet(object sender, RoutedEventArgs e)
        {
            var oldViewModel = setViewModel.halfViewModel?.Clone();

            WaterSupplyHalfPlatformSetting systemSet = new WaterSupplyHalfPlatformSetting(setViewModel.halfViewModel);
            systemSet.Owner = this;
            var ret = systemSet.ShowDialog();
            if (ret == false)
            {
                //用户取消了操作
                setViewModel.halfViewModel = oldViewModel;
                return;
            }
        }

        private void btnHeights_Click(object sender, RoutedEventArgs e)
        {
            FloorHeightSettingWindow.ShowModelSingletonWindow();

        }
    }
}
