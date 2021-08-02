using AcHelper;
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

namespace ThMEPWSS.UndergroundFireHydrantSystem.UI
{
    /// <summary>
    /// uiFireHydrantSystem.xaml 的交互逻辑
    /// </summary>
    public partial class uiFireHydrantSystem : ThCustomWindow
    {
        public uiFireHydrantSystem()
        {
            InitializeComponent();
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            using (var cmd = new ThFireHydrantCmd())
            {
                cmd.Execute();
            }
        }

        private void LoopMark_Click(object sender, RoutedEventArgs e)
        {
            ThFireHydrantSystemViewModel.InsertLoopMark();
        }

        private void SubLoopMark_Click(object sender, RoutedEventArgs e)
        {
            ThFireHydrantSystemViewModel.InsertSubLoopMark();
        }
    }
}
