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
using ThMEPWSS.UndergroundWaterSystem.ViewModel;
using ThControlLibraryWPF.CustomControl;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiUndergroundWaterSystemSet.xaml 的交互逻辑
    /// </summary>
    public partial class uiUndergroundWaterSystemSet : ThCustomWindow
    {
        private static ThWaterSystemParamViewModel ViewModel = null;
        public uiUndergroundWaterSystemSet()
        {
            InitializeComponent();
            if (ViewModel == null)
            {
                ViewModel = new ThWaterSystemParamViewModel();
            }
            DataContext = ViewModel;
        }
        public ThWaterSystemParamViewModel GetViewModel()
        {
            return ViewModel;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
