using System.Windows;
using TianHua.Electrical.PDS.UI.ViewModels;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    public partial class ThPDSCreateLoad : Window
    {
        public ThPDSCreateLoad()
        {
            InitializeComponent();
            this.DataContext = new ThPDSCreateLoadVM();
        }

        private void btnInsert(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
        private void btnSave(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
