using ThControlLibraryWPF.CustomControl;
using TianHua.Platform3D.UI.ViewModels;

namespace Tianhua.Platform3D.UI.UI
{
    public partial class InitStoreyUI : ThCustomWindow
    {
        private InitStoreyVM _vm;
        public bool IsSuccess { get; private set; }
        public InitStoreyUI(InitStoreyVM vm)
        {
            InitializeComponent();
            this.WindowStartupLocation = System.Windows.
                WindowStartupLocation.CenterOwner;
            this._vm = vm;
            this.DataContext = _vm;
            this.Topmost = true;
        }

        private void btnOK_Click(object sender, System.Windows.RoutedEventArgs e)
        {           
            IsSuccess = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            IsSuccess = false;
            this.Close();
        }
    }
}
