using System.Windows;
using ThControlLibraryWPF.CustomControl;
using Tianhua.Platform3D.UI.Command;

namespace Tianhua.Platform3D.UI.StructurePlane
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
            if (ViewModel.Model.FileFormatOption == FileFormatOps.GET)
            {
                Program.Run();
            }
            ViewModel.Save();
            ViewModel.BrowseFile();
            this.Close();
        }
    }
}
