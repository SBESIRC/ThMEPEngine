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
    public partial class AirCompensationConfig : ThCustomWindow
    {
        public UsuallyAirCompensation _data { get; set; }
        public AirCompensationConfig(UsuallyAirCompensation data)
        {
            InitializeComponent();
            this._data = data;

            if (data.ByNorm == 1)
            {
                RadioBtnType1.IsChecked = true;
                RadioBtnType2.IsChecked = false;
                RadioBtnType3.IsChecked = false;
            }
            else if (data.ByNorm == 2)
            {
                RadioBtnType1.IsChecked = false;
                RadioBtnType2.IsChecked = true;
                RadioBtnType3.IsChecked = false;
            }
            else
            {
                RadioBtnType1.IsChecked = false;
                RadioBtnType2.IsChecked = false;
                RadioBtnType3.IsChecked = true;
            }
            NormTxt.Text = data.NormValue.ToString();
            TotalTxt.Text = data.TotalValue.ToString();
            if (data.CapacityType == 1)
            {
                CapacityType1.IsChecked = true;
            }
            else
            {
                CapacityType2.IsChecked = true;
            }
            BoilerCapacityTxt.Text = data.BoilerCapacity.ToString();
            FirewoodCapacityTxt.Text = data.FirewoodCapacity.ToString();
            CombustionAirVolumeTxt.Text = data.CombustionAirVolume.ToString();
            this.NormTxt.Focus();
            this.NormTxt.SelectionStart = this.NormTxt.Text.Length;
        }

        private void CancleButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            this._data.ByNorm = RadioBtnType1.IsChecked.Value ? 1 : RadioBtnType2.IsChecked.Value ? 2 : 3;
            if (string.IsNullOrEmpty(NormTxt.Text))
            {
                this._data.NormValue = null;
            }
            else
            {
                this._data.NormValue = double.Parse(NormTxt.Text);
            }
            if (string.IsNullOrEmpty(TotalTxt.Text))
            {
                this._data.TotalValue = null;
            }
            else
            {
                this._data.TotalValue = int.Parse(TotalTxt.Text);
            }
            this._data.CapacityType = CapacityType1.IsChecked.Value ? 1 : 2;
            this._data.BoilerCapacity = int.Parse(BoilerCapacityTxt.Text);
            this._data.FirewoodCapacity = int.Parse(FirewoodCapacityTxt.Text);
            this._data.CombustionAirVolume = int.Parse(CombustionAirVolumeTxt.Text);
            this.Close();
        }

        private void RadioBtnType1_Checked(object sender, RoutedEventArgs e)
        {
            if (!NormTxt.IsNull() && !TotalTxt.IsNull() && !panel1.IsNull() && !panel2.IsNull())
            {
                NormTxt.IsEnabled = true;
                TotalTxt.IsEnabled = false;
                panel1.IsEnabled = false;
                panel2.IsEnabled = false;
            }
        }

        private void RadioBtnType2_Checked(object sender, RoutedEventArgs e)
        {
            if (!NormTxt.IsNull() && !TotalTxt.IsNull() && !panel1.IsNull() && !panel2.IsNull())
            {
                NormTxt.IsEnabled = false;
                TotalTxt.IsEnabled = true;
                panel1.IsEnabled = false;
                panel2.IsEnabled = false;
            }
        }

        private void RadioBtnType3_Checked(object sender, RoutedEventArgs e)
        {
            if (!NormTxt.IsNull() && !TotalTxt.IsNull() && !panel1.IsNull() && !panel2.IsNull())
            {
                NormTxt.IsEnabled = false;
                TotalTxt.IsEnabled = false;
                panel1.IsEnabled = true;
                panel2.IsEnabled = true;
            }
        }
    }
}
