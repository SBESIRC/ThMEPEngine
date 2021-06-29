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

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiDrainageSystemAboveGround.xaml 的交互逻辑
    /// </summary>
    public partial class uiDrainageSysAboveGround : ThCustomWindow
    {
        public uiDrainageSysAboveGround()
        {
            InitializeComponent();
        }

        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            var ui = new uiDrainageSysAboveGroundSet();
            var result = ui.ShowDialog();
            if (result.Value) 
            {
                
            }
        }
    }
}
