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
using ThControlLibraryWPF;
using ThMEPLighting.UI.ViewModels;

namespace ThMEPLighting.UI.UI
{
    /// <summary>
    /// ExampleUI.xaml 的交互逻辑
    /// </summary>
    public partial class ExampleUI : Window
    {
        public ExampleUI()
        {
            InitializeComponent();
            ExampleViewModel exampleViewModel = new ExampleViewModel();
            this.DataContext = exampleViewModel;
        }
    }
}
