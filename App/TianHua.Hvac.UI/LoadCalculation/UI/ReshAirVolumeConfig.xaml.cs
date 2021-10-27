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
    public partial class ReshAirVolumeConfig : ThCustomWindow
    {
        public ReshAirVolume _data { get; set; }
        public ReshAirVolumeConfig(ReshAirVolume data)
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
            PersonnelDensityTxt.Text = data.PersonnelDensity.ToString();
            FreshAirNormTxt.Text = data.ReshAirNormValue.ToString();
            TotalTxt.Text = data.TotalValue.ToString();
        }

        private void CancleButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            this._data.ByNorm = RadioBtnTrue.IsChecked.Value;
            this._data.PersonnelDensity = double.Parse(PersonnelDensityTxt.Text);
            this._data.ReshAirNormValue = int.Parse(FreshAirNormTxt.Text);
            this._data.TotalValue = int.Parse(TotalTxt.Text);
            this.Close();
        }
    }
}
