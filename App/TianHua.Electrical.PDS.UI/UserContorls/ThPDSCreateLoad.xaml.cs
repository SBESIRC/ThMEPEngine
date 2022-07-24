using System.Windows;
using TianHua.Electrical.PDS.UI.ViewModels;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    public partial class ThPDSCreateLoad : Window
    {
        public enum ActionMode
        {
            None    = 0x0,
            Create  = 0x1,
            Update  = 0x2,
            Insert  = 0x4,
        }
        public ActionMode Mode { get; set; }

        public ThPDSCreateLoad()
        {
            InitializeComponent();
            this.DataContext = new ThPDSCreateLoadVM();
        }

        private void btnInsert(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Mode = ActionMode.Insert | ActionMode.Create;
            Close();
        }
        private void btnSave(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Mode = ActionMode.Create;
            Close();
        }

        private void btnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Mode = ActionMode.None;
            Close();
        }
    }
}
