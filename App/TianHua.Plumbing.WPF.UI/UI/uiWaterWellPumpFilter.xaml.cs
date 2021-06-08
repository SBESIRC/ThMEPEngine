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
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.Pipe.Model;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiWaterWellPumpFilter.xaml 的交互逻辑
    /// </summary>
    public partial class uiWaterWellPumpFilter : ThCustomWindow
    {
        WaterWellIdentifyConfigInfo identifyConfigInfo = new WaterWellIdentifyConfigInfo();
        public uiWaterWellPumpFilter()
        {
            InitializeComponent();
        }
        public WaterWellIdentifyConfigInfo GetIdentfyConfigInfo()
        {
            return identifyConfigInfo;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            identifyConfigInfo.WhiteList.Clear();
            if (!WhiteTextBox0.Text.IsNullOrEmpty())
            {
                identifyConfigInfo.WhiteList.Add(WhiteTextBox0.Text);
            }
            if (!WhiteTextBox1.Text.IsNullOrEmpty())
            {
                identifyConfigInfo.WhiteList.Add(WhiteTextBox1.Text);
            }
            if (!WhiteTextBox2.Text.IsNullOrEmpty())
            {
                identifyConfigInfo.WhiteList.Add(WhiteTextBox2.Text);
            }

            if (!BlackTextBox0.Text.IsNullOrEmpty())
            {
                identifyConfigInfo.BlackList.Add(WhiteTextBox0.Text);
            }
            if (!BlackTextBox1.Text.IsNullOrEmpty())
            {
                identifyConfigInfo.BlackList.Add(WhiteTextBox1.Text);
            }
            if (!BlackTextBox2.Text.IsNullOrEmpty())
            {
                identifyConfigInfo.BlackList.Add(WhiteTextBox2.Text);
            }

            this.DialogResult = true;
            this.Close();
        }
    }
}
