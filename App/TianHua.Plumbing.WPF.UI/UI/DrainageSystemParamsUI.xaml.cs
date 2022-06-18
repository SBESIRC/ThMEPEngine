using Newtonsoft.Json;
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
using ThMEPWSS.Assistant;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.JsonExtensionsNs;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiRainSystemParams.xaml 的交互逻辑
    /// </summary>
    public partial class DrainageSystemParamsUI : ThCustomWindow
    {
        public bool Ok;
        dynamic vm;
        dynamic _vm;
        public DrainageSystemParamsUI(dynamic vm)
        {
            InitializeComponent();
            this._vm = ObjFac.CloneObjByJson(vm);
            this.vm = vm;
            this.DataContext = _vm;
            cbxBasinDn.ItemsSource = cbxFloorDrain.ItemsSource = new string[] { "DN50", "DN75", };
            this.Basin.ItemsSource = new string[] { "双池S弯", "双池P弯" };
            cbxWellFD.ItemsSource = cbxBalFD.ItemsSource = cbxCndFD.ItemsSource = new string[] { "DN50", "DN75", };
            cbxLNLG.ItemsSource = cbxYTYS.ItemsSource = new string[] { "DN50", "DN75", "DN100" };
            cbxLNHG.ItemsSource = new string[] { "DN25", "DN32", "DN50" };
            cbxSJLG.ItemsSource = new string[] { "DN50", "DN75", "DN100" };
        }



        private void OK_Click(object sender, RoutedEventArgs e)
        {
            ObjFac.CopyProperties(_vm, vm);
            Ok = true;
            this.Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void btnSetHeights(object sender, RoutedEventArgs e)
        {
            FloorHeightSettingWindow.ShowModelSingletonWindow();
        }
    }
}
