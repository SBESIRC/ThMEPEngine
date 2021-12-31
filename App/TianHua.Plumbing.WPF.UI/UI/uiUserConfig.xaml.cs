using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;
using ThMEPWSS.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class uiUserConfig : ThCustomWindow
    {
        private ThTianHuaUserConfigVM VM;
        public uiUserConfig()
        {
            InitializeComponent();
            if(VM==null)
            {
                VM = new ThTianHuaUserConfigVM();
            }
            this.DataContext = VM;
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }
        private void ThCustomWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        private void btnSave_Click(object sender, System.Windows.RoutedEventArgs e)
        {
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
