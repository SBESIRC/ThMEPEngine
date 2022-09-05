using System.Windows;
using ThControlLibraryWPF.CustomControl;
using TianHua.Platform3D.UI.ViewModels;

namespace Tianhua.Platform3D.UI.UI
{
    public partial class RelatePaperUI : ThCustomWindow
    {
        private bool _isSuccess = false;
        public bool IsSuccess => _isSuccess;
        private RelatePaperVM _vm;
        public RelatePaperUI(RelatePaperVM vm)
        {
            InitializeComponent();
            this._vm = vm;  
            this.DataContext = _vm;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.Topmost = true;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this._isSuccess = true;
            this.Close();
        }

        private void btnCanel_Click(object sender, RoutedEventArgs e)
        {
            this._isSuccess = false;
            this.Close();
        }
    }
}
