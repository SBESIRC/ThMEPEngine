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
using ThMEPHVAC.LoadCalculation.Extension;
using ThMEPHVAC.LoadCalculation.Model;

namespace TianHua.Hvac.UI.LoadCalculation.UI
{
    /// <summary>
    /// ColdNormConfig.xaml 的交互逻辑
    /// </summary>
    public partial class AccidentAirConfig : ThCustomWindow
    {
        public LampblackClass _data { get; set; }
        public AccidentAirConfig(LampblackClass data)
        {
            InitializeComponent();
            this._data = data;

            if(data.ByNorm)
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
            this.ProportionTxt.Focus();
            this.ProportionTxt.SelectionStart = this.ProportionTxt.Text.Length;
        }

        private void CancleButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            this._data.ByNorm = RadioBtnTrue.IsChecked.Value;
            this._data.Proportion = 0;
            if (double.TryParse(ProportionTxt.Text, out double num))
            {
                this._data.Proportion = num;
            }
            this._data.AirNum = AirNumTxt.Text.ToNullDouble();
            this._data.TotalValue = TotalTxt.Text.ToNullDouble();
            this.Close();
        }

        private void RadioBtnTrue_Checked(object sender, RoutedEventArgs e)
        {
            if (!ProportionTxt.IsNull() && !AirNumTxt.IsNull() && !TotalTxt.IsNull())
            {
                ProportionTxt.IsEnabled = true;
                AirNumTxt.IsEnabled = true;
                TotalTxt.IsEnabled = false;
            }
        }

        private void RadioBtnFalse_Checked(object sender, RoutedEventArgs e)
        {
            if (!ProportionTxt.IsNull() && !AirNumTxt.IsNull() && !TotalTxt.IsNull())
            {
                ProportionTxt.IsEnabled = false;
                AirNumTxt.IsEnabled = false;
                TotalTxt.IsEnabled = true;
            }
        }
    }
}
