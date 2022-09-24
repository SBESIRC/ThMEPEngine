using System;
using System.Collections.Generic;
using System.Globalization;
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

using ThControlLibraryWPF;
using ThControlLibraryWPF.CustomControl;

using ThMEPWSS.SprinklerDim.Model;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiSprinklerDim.xaml 的交互逻辑
    /// </summary>
    public partial class uiSprinklerDim : ThCustomWindow
    {
        private ThSprinklerDimViewModel SprinklerDimViewModel;
        public static uiSprinklerDim Instance;

        static uiSprinklerDim()
        {
            if (Instance == null)
            {
                Instance = new uiSprinklerDim();
            }
        }

        public uiSprinklerDim()
        {
            InitializeComponent();
            this.MutexName = "THPLBZ";
            if (SprinklerDimViewModel == null)
            {
                SprinklerDimViewModel = new ThSprinklerDimViewModel();
            }
            DataContext = SprinklerDimViewModel;

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }

    public class TrueFalseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = (int)value;
            return s == int.Parse(parameter.ToString());
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return int.Parse(parameter.ToString());
        }
    }

}
