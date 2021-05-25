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
using ThMEPWSS.Diagram.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiRainSystem.xaml 的交互逻辑
    /// </summary>
    public partial class uiRainSystem : ThCustomWindow
    {
        private RainSystemDiagramViewModel ViewModel = new RainSystemDiagramViewModel();

        public uiRainSystem()
        {
            InitializeComponent();
            this.DataContext = ViewModel;
        }

        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            var uiParams = new uiRainSystemParams(ViewModel.Params);
            uiParams.ShowDialog();

        }

        //run command
        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            using (var cmd = new ThRainSystemDiagramCmd())
            {
                cmd.Execute();
            }
        }

        //Init storeys
        private void ImageButton_Click_1(object sender, RoutedEventArgs e)
        {
            ViewModel.InitFloorListDatas();
        }
    }
}
