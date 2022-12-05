using System.Windows;
using ThControlLibraryWPF.CustomControl;
using Tianhua.Platform3D.UI.ViewModels;

namespace Tianhua.Platform3D.UI.UI
{
    public partial class PQKInputUI : ThCustomWindow
    {
        private bool _isSuccess = false;
        public bool IsSuccess => _isSuccess;
        private PQKInputVM _vm;
        public PQKInputUI(PQKInputVM vm)
        {
            InitializeComponent();
            this.WindowStartupLocation = System.Windows.
                WindowStartupLocation.CenterScreen;
            this._vm = vm;
            this.DataContext = this._vm;
            this.Topmost = true;
        }

        private void btnOK_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string errorMsg = "";
            _isSuccess = _vm.Confirm(out errorMsg);
            if (_isSuccess==false)
            {
                MessageBox.Show(errorMsg, "输入提示", MessageBoxButton.OK);
                return;
            }
            else 
            {
                this.Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            _isSuccess = false;
            this.Close();
        }
    }
}
