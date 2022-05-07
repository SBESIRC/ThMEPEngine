using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class ExtractBeamConfigUI : ThCustomWindow
    {
        private ThExtractBeamConfigVM ExtractBeamConfigVM { get; set; } 
        public ExtractBeamConfigUI()
        {
            ExtractBeamConfigVM= new ThExtractBeamConfigVM();
            InitializeComponent();
            this.DataContext = ExtractBeamConfigVM;
        }

        private void rbLayer_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            this.btnAddLayer.IsEnabled = false;
            this.listBox.IsEnabled = false;
        }
        private void rbDB_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            this.btnAddLayer.IsEnabled = false;
            this.listBox.IsEnabled = false;
        }

        private void btnAddLayer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExtractBeamConfigVM.SelectLayer();
        }

        private void btnOk_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExtractBeamConfigVM.Confirm();
            this.Close();
        }

        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void rbBeamArea_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            this.btnAddLayer.IsEnabled = true;
            this.listBox.IsEnabled = true;
        }
    }
}
