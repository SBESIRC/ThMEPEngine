using System.Windows;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.ViewModel;

namespace TianHua.Plumbing.WPF.UI.FirstFloorDrainagePlaneSystemUI
{
    /// <summary>
    /// ParameterSetUI.xaml 的交互逻辑
    /// </summary>
    public partial class ParameterSetUI : ThCustomWindow
    {
        public static ParamSettingViewModel paramSetting = new ParamSettingViewModel();
        public ParameterSetUI()
        {
            this.DataContext = paramSetting;
            InitializeComponent();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("保存成功！");
            this.Close();
        }
    }
}
