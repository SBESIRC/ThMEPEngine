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
using ThMEPLighting.Lighting.ViewModels;
using ThMEPLighting.Lighting.Commands;
using ThMEPEngineCore.Command;
using System.Threading;

namespace TianHua.Lighting.UI
{
    /// <summary>
    /// uiThLighting.xaml 的交互逻辑
    /// </summary>
    public partial class uiThLighting : ThCustomWindow
    {
        static LightingViewModel UIConfigs = null;
        public uiThLighting()
        {
            InitializeComponent();
             if(UIConfigs == null)
            {
                UIConfigs = new LightingViewModel();
            }
            DataContext = UIConfigs;

            //For single form instance
            MutexName = "Mutext_uiThLighting";
        }

        private void btnLayout_Click(object sender, RoutedEventArgs e)
        {
            using (var cmd = new LightingLayoutCommand(UIConfigs))
            {
                cmd.Execute();
            }
        }
        private void bthRouting_Click(object sender, RoutedEventArgs e)
        {
            using (var cmd = new LightingRouteCableCommand(UIConfigs))
            {
                cmd.Execute();
            }
        }
    }
}
