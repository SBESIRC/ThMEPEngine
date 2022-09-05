using System.Windows;
using ThControlLibraryWPF.CustomControl;
using TianHua.Platform3D.UI.ViewModels;

namespace Tianhua.Platform3D.UI.UI
{
    public partial class TextInputUI : ThCustomWindow
    {
        private bool _isSuccess = false;
        public bool IsSuccess => _isSuccess;
        private TextInputVM _vm;
        public TextInputUI(TextInputVM vm)
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
            _isSuccess = false;
            if (_vm.IsEmpty)
            {
                MessageBox.Show("输入的单体名称不能为空，请重新输入！", "输入提示", MessageBoxButton.OK);
                _vm.Clear();
            }
            else if(_vm.IsExisted)
            {
                MessageBox.Show("输入的单体名称已存在，请重新输入！", "输入提示",MessageBoxButton.OK);
                _vm.Clear();
            }
            else
            {
                _isSuccess = true;
                this.Close();
            }
        }
    }
}
