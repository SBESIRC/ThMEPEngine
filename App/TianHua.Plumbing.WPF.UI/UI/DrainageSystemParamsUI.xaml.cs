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
        //DrainageSystemDiagramParamsViewModel vm;
        //DrainageSystemDiagramParamsViewModel _vm;
        dynamic vm;
        dynamic _vm;
        //public DrainageSystemParamsUI(DrainageSystemDiagramParamsViewModel vm)
        public DrainageSystemParamsUI(dynamic vm)
        {
            InitializeComponent();
            //this._vm = vm.Clone();
            //this.vm = vm;
            this._vm = Clone(vm);
            this.vm = vm;
            this.DataContext = _vm;
            this.洗衣地漏.ItemsSource = new string[] { "DN50", "DN75", };
            this.厨房洗涤盆.ItemsSource = new string[] { "双池S弯", "双池P弯" };
        }

        public static object Clone(object o)
        {
            return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(o), o.GetType());
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            //_vm.CopyTo(vm);
            CopyPropertiesTo(_vm, vm);
            Ok = true;
            this.Close();
        }
        public static void CopyPropertiesTo(object src, object dst)
        {
            src.GetType().GetProperties().Join(dst.GetType().GetProperties(), x => new KeyValuePair<string, Type>(x.Name, x.PropertyType), x => new KeyValuePair<string, Type>(x.Name, x.PropertyType), (x, y) =>
            {
                y.SetValue(dst, x.GetValue(src));
                return 666;
            }).Count();
        }
        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            //todo: clear
            this.Close();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
