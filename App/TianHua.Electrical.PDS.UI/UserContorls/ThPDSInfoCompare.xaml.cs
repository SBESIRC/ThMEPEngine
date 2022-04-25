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
using TianHua.Electrical.PDS.UI.Services;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    /// <summary>
    /// ThPDSInfoCompare.xaml 的交互逻辑
    /// </summary>
    public partial class ThPDSInfoCompare : UserControl
    {
        ThPDSInfoCompareService Service = new();
        public ThPDSInfoCompare()
        {
            InitializeComponent();
            Service.Init(this);
        }

        private void btnCompareClick(object sender, RoutedEventArgs e)
        {
            new Service.ThPDSSecondaryPushDataService().Push();
            PDS.Project.PDSProject.Instance.DataChanged?.Invoke();
            Service.UpdateView(this);
        }

        private void btnAcceptClick(object sender, RoutedEventArgs e)
        {

        }

        private void btnCreateClick(object sender, RoutedEventArgs e)
        {

        }

        private void btnUpdateClick(object sender, RoutedEventArgs e)
        {

        }
    }
}
