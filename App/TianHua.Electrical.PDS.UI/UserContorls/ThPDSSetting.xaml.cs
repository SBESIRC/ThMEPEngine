using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class ThPDSSetting : Window
    {
        public ThPDSSetting()
        {
            InitializeComponent();
        }

        private void btnSaveClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnBrowseClick(object sender, RoutedEventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

            }
        }
    }
}
