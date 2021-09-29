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
using ThMEPHVAC.FanLayout.ViewModel;

namespace TianHua.Hvac.UI.UI
{
    /// <summary>
    /// uiFanInfoWidget.xaml 的交互逻辑
    /// </summary>
    public partial class uiFanInfoWidget : ThCustomWindow
    {
        public uiFanInfoWidget()
        {
            InitializeComponent();
        }

        public ThFanConfigInfo GetFanConfigInfo()
        {
            var fanInfo = new ThFanConfigInfo();
            fanInfo.FanNumber = textBoxNumber.Text;
            fanInfo.FanVolume = double.Parse(textBoxVolume.Text);
            fanInfo.FanPressure = double.Parse(textBoxPressure.Text);
            fanInfo.FanPower = double.Parse(textBoxPower.Text);
            fanInfo.FanWeight = double.Parse(textBoxWeight.Text);
            fanInfo.FanNoise = double.Parse(textBoxNoise.Text);
            fanInfo.FanDepth = double.Parse(textBoxDepth.Text);
            fanInfo.FanLength = double.Parse(textBoxLength.Text);
            fanInfo.FanWidth = double.Parse(textBoxWidth.Text);
            return fanInfo;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
