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
using ThMEPWSS.Command;
using ThMEPWSS.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiFireControlSystem.xaml 的交互逻辑
    /// </summary>
    public partial class uiFireControlSystem : ThCustomWindow
    {
        FireControlSystemDiagramViewModel vm = new FireControlSystemDiagramViewModel();

        public uiFireControlSystem()
        {
            InitializeComponent();
            this.DataContext = vm;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            var cmd = new ThFireControlSystemDiagramCmd();
            cmd.Execute();
        }
    }
}
