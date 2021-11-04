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
using ThMEPHVAC.LoadCalculation.Model;

namespace TianHua.Hvac.UI.LoadCalculation.UI
{
    /// <summary>
    /// ColdNormConfig.xaml 的交互逻辑
    /// </summary>
    public partial class LampblackConfig : ThCustomWindow
    {
        public LampblackClass _data { get; set; }
        public LampblackConfig(LampblackClass data)
        {
            InitializeComponent();
            this._data = data;

            if (data.ByNorm)
            {
                RadioBtnTrue.IsChecked = true;
                RadioBtnFalse.IsChecked = false;
            }
            else
            {
                RadioBtnTrue.IsChecked = false;
                RadioBtnFalse.IsChecked = true;
            }
            ProportionTxt.Text = data.Proportion.ToString();
            AirNumTxt.Text = data.AirNum.ToString();
            TotalTxt.Text = data.TotalValue.ToString();
            this.AirNumTxt.Focus();
            this.AirNumTxt.SelectionStart = this.AirNumTxt.Text.Length;
        }

        private void CancleButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            this._data.ByNorm = RadioBtnTrue.IsChecked.Value;
            this._data.Proportion = double.Parse(ProportionTxt.Text);
            if (string.IsNullOrEmpty(AirNumTxt.Text))
            {
                this._data.AirNum = null;
            }
            else
            {
                this._data.AirNum = double.Parse(AirNumTxt.Text);
            }
            if (string.IsNullOrEmpty(TotalTxt.Text))
            {
                this._data.TotalValue = null;
            }
            else
            {
                this._data.TotalValue = double.Parse(TotalTxt.Text);
            }
            this.Close();
        }

        private void RadioBtnTrue_Checked(object sender, RoutedEventArgs e)
        {
            if (!panel1.IsNull() && !panel2.IsNull() && !TotalTxt.IsNull())
            {
                panel1.IsEnabled = true;
                panel2.IsEnabled = true;
                TotalTxt.IsEnabled = false;
            }
        }

        private void RadioBtnFalse_Checked(object sender, RoutedEventArgs e)
        {
            if (!panel1.IsNull() && !panel2.IsNull() && !TotalTxt.IsNull())
            {
                panel1.IsEnabled = false;
                panel2.IsEnabled = false;
                TotalTxt.IsEnabled = true;
            }
        }
    }
}
