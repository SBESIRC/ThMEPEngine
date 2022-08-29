using System.Windows;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class uiStoreyFrame : ThCustomWindow
    {
        private ThStoreyFrameVM _vm;
        public uiStoreyFrame()
        {
            InitializeComponent();
            this._vm = new ThStoreyFrameVM();
            this.DataContext = _vm;
            this.Topmost = true;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }
}
