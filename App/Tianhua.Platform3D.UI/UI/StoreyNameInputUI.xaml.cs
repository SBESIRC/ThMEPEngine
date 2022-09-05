using System.Windows;
using ThControlLibraryWPF.CustomControl;
using TianHua.Platform3D.UI.ViewModels;

namespace Tianhua.Platform3D.UI.UI
{
    public partial class StoreyNameInputUI : ThCustomWindow
    {
        private bool _isSuccess = false;
        public bool IsSuccess => _isSuccess;
        private StoreyNameInputVM _vm;
        public StoreyNameInputUI(StoreyNameInputVM vm)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this._vm = vm;
            this.DataContext = this._vm;            
        }

        private void btnOK_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string message = _vm.CheckInputValue();
            if (!string.IsNullOrEmpty(message))
            {
                _isSuccess = false;
                MessageBox.Show(message, "输入提示", MessageBoxButton.OK);
                return;
            }           
            else
            {
                _isSuccess = true;
                this.Close();
            }
        }
    }
}
