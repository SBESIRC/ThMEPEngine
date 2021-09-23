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
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Pipe.Model;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiRainSystemParams.xaml 的交互逻辑
    /// </summary>
    public partial class uiRainSystemParams : ThCustomWindow
    {
        RainSystemDiagramParamsViewModel vm;
        RainSystemDiagramParamsViewModel _vm;
        public uiRainSystemParams(RainSystemDiagramParamsViewModel vm)
        {
            InitializeComponent();
            this._vm = vm.Clone();
            this.vm = vm;
            this.DataContext = _vm;
            this.阳台地漏.ItemsSource = new string[] { "DN50", "DN75", };
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            _vm.CopyTo(vm);
            this.Close();
        }

        private void Cancle_Click(object sender, RoutedEventArgs e)
        {
            //todo: clear
            this.Close();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {

        }

        private void CheckBox_Checked_2(object sender, RoutedEventArgs e)
        {

        }

        private void btnSetHeights(object sender, RoutedEventArgs e)
        {
            FloorHeightSettingWindow.ShowModelSingletonWindow();
        }
    }
}
