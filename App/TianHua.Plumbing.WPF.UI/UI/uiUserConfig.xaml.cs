using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;
using ThMEPWSS.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class uiUserConfig : ThCustomWindow
    {
        private ThTianHuaUserConfigVM VM;
        public uiUserConfig(ThTianHuaUserConfigVM vm)
        {
            InitializeComponent();
            this.VM = vm;
            this.DataContext = VM;
        }
        private void btnSave_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
            using (var cmd = new ThTianHuaUserConfigCmd(VM))
            {
                cmd.Execute();
            }
        }

        private void btnCanel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
