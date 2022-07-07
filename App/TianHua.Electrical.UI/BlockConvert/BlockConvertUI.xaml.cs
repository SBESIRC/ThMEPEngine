using ThMEPElectrical.Model;
using ThControlLibraryWPF.CustomControl;

namespace TianHua.Electrical.UI.BlockConvert
{
    public partial class BlockConvertUI : ThCustomWindow
    {
        public ThBlockConvertModel Parameter { get; set; }
        public bool GoOn { get; set; }
        
        public BlockConvertUI()
        {
            Parameter = new ThBlockConvertModel();
            GoOn = false;            
            InitializeComponent();
            this.DataContext = Parameter;
        }

        private void btnBlockConvert_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            GoOn = true;
            this.Close();
        }

        private void btnUpdateCompare_Click(object sender, System.Windows.RoutedEventArgs e)
        {
        }

        private void wssConvertStrongEquip_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            convertManualActuator.IsEnabled = false;
            convertManualActuator.IsChecked = false;
        }

        private void wssConvertWeakEquip_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            convertManualActuator.IsEnabled = true;
        }

        private void wssConvertAllEquip_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            convertManualActuator.IsEnabled = true;
        }

        private void btnSinglePointUpdate_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void btnBlockUpdate_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }
    }
}
