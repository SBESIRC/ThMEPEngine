using System.Windows;
using System.Windows.Controls;
using TianHua.Electrical.PDS.Project;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.UI.Project;

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
            PDSProject.Instance.ProjectGlobalConfigurationChanged += (sender , e) =>
            {
                DataContext = new ThPDSUESandboxParameterModel();
            };
        }
        private void btnImportSetting(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".Config"; // Default file extension
            dlg.Filter = "Global Configuration|*.Config"; // Filter files by extension
            var result = dlg.ShowDialog();
            if (result == true)
            {
                PDSProject.Instance.LoadGlobalConfiguration(dlg.FileName);
            }
        }
        private void btnExportSetting(object sender, RoutedEventArgs e)
        {
            //选择路径
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "GlobalConfiguration"; // Default file name
            dlg.DefaultExt = ".Config"; // Default file extension
            dlg.Filter = "Global Configuration|*.Config"; // Filter files by extension
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                var filePathUrl = dlg.FileName.Substring(0, dlg.FileName.LastIndexOf("\\"));
                var fileName = dlg.SafeFileName;
                ThPDSProjectGraphService.ExportGlobalConfiguration(filePathUrl, fileName);
            }
        }
        private void btnApplySetting(object sender, RoutedEventArgs e)
        {
            ThPDSProjectGraphService.GlobalConfigurationUpdate();
        }
    }
}
