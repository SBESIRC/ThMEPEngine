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

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    /// <summary>
    /// ThPDSAssignCircuit2SmallBusbar.xaml 的交互逻辑
    /// </summary>
    public partial class ThPDSAssignCircuit2SmallBusbar : Window
    {
        public ThPDSAssignCircuit2SmallBusbar()
        {
            InitializeComponent();
        }

        private void btnOkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
