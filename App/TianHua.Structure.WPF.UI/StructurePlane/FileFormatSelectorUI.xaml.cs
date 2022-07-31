using System.Windows;
using ThControlLibraryWPF.CustomControl;

namespace TianHua.Structure.WPF.UI.StructurePlane
{
    public partial class FileFormatSelectorUI : ThCustomWindow
    {
        private FileFormatSelectVM ViewModel;
        public FileFormatSelectorUI(FileFormatSelectVM vm)
        {
            InitializeComponent();
            ViewModel = vm;
            this.DataContext = ViewModel;
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Save();
            ViewModel.BrowseFile();
            this.Close();
        }
    }
}
