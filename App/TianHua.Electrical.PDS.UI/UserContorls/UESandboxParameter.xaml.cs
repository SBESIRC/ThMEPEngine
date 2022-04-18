using System.Windows;
using System.Windows.Controls;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.UI.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    /// <summary>
    /// UESandboxParameter.xaml 的交互逻辑
    /// </summary>
    public partial class UESandboxParameter : UserControl
    {
        public UESandboxParameter()
        {
            InitializeComponent();
            DataContext = new ThPDSUESandboxParameterModel();
        }
        private void btnImportSetting(object sender, RoutedEventArgs e)
        {
        }
        private void btnSaveSetting(object sender, RoutedEventArgs e)
        {
            ThPDSProjectGraphService.MotorChoiseChange();
        }
        private void btnExportSetting(object sender, RoutedEventArgs e)
        {
        }
    }
}
