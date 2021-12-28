using System.Windows.Input;
using ThControlLibraryWPF.CustomControl;

using ThMEPWSS;
using ThMEPWSS.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class SprinklerConnectionUI : ThCustomWindow
    {
        private ThSprinklerConnectVM VM;
        public static SprinklerConnectionUI Instance = null;
        static SprinklerConnectionUI()
        {
            Instance = new SprinklerConnectionUI();
        }
        
        private SprinklerConnectionUI()
        {
            InitializeComponent();
            if(VM==null)
            {
                VM = new ThSprinklerConnectVM();
            }
            this.DataContext = VM;
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }

        private void ThCustomWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }
    }
}
