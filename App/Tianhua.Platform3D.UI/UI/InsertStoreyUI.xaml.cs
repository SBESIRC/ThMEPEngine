using System.Windows;
using ThControlLibraryWPF.CustomControl;
using TianHua.Platform3D.UI.ViewModels;

namespace Tianhua.Platform3D.UI.UI
{
    public partial class InsertStoreyUI : ThCustomWindow
    {
        private InsertStoreyVM _vm;
        private bool _isSuccess = false;
        public bool IsSuccess => _isSuccess;
        public InsertStoreyUI(InsertStoreyVM vm) //TextInputVM vm
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this._vm = vm;
            this.DataContext = this._vm;
            this.Topmost = true;
        }

        private void btnOK_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string message = _vm.Confirm();
            if(!string.IsNullOrEmpty(message))
            {
                this._isSuccess = false;
                MessageBox.Show(message,"输入提示",MessageBoxButton.OK,MessageBoxImage.Information);
                return;
            }
            else
            {
                this._isSuccess = true;
                this.Close();
            }            
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this._isSuccess = false;
            this.Close();
        }

        private void cbStoreyType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _vm.UpdateInsertStoreys(this.cbStoreyType.SelectedItem.ToString());
            this.cbInsertStorey.ItemsSource = _vm.InsertStoreys;
            this.cbInsertStorey.SelectedItem = _vm.InsertStorey;
            //if(_vm.InsertStoreys.Count>0 && _vm.InsertStorey!=null)
            //{
            //    this.cbInsertStorey.SelectedIndex = _vm.InsertStoreys.IndexOf(_vm.InsertStorey);
            //}            
        }
    }
}
