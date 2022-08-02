using System.Windows;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.ViewModel;
using TianHua.Plumbing.WPF.UI.Command;

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
            this.Close();
        }

        private void btnRainwaterInlet_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            ThLayoutRainwaterInletCmd thLayoutRainwaterInletCmd = new ThLayoutRainwaterInletCmd("W-RAIN-EQPM", "13雨水口");
            thLayoutRainwaterInletCmd.SubExecute();
            this.ShowDialog();
        }

        private void btnOverflowTunnel_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            ThLayoutOverflowTunnelCmd thLayoutOverflowTunnelCmd = new ThLayoutOverflowTunnelCmd("W-RAIN-EQPM");
            thLayoutOverflowTunnelCmd.SubExecute();
            this.ShowDialog();
        }
    }
}
