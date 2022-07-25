using System.Windows;
using ThControlLibraryWPF.CustomControl;

namespace TianHua.Structure.WPF.UI.StructurePlane
{
    public partial class FileFormatSelectorUI : ThCustomWindow
    {
        private FileFormatSelectVM ViewModel;
        public FileFormatSelectorUI()
        {
            InitializeComponent();
            ViewModel = new FileFormatSelectVM();
            this.DataContext = ViewModel;
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            ViewModel.Run();
        }
    }
}
