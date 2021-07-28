using ThMEPElectrical.Model;
using ThControlLibraryWPF.CustomControl;

namespace TianHua.Electrical.UI.CapitalConverter
{
    public partial class CapitalConverterUI : ThCustomWindow
    {
        public ThCapitalConverterModel Parameter { get; set; }
        public bool GoOn { get; set; }
        
        public CapitalConverterUI()
        {
            Parameter = new ThCapitalConverterModel();
            GoOn = false;            
            InitializeComponent();
            this.DataContext = Parameter;
        }

        private void btnCapitalConvert_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            GoOn = true;
            this.Close();
        }

        private void btnUpdateCompare_Click(object sender, System.Windows.RoutedEventArgs e)
        {
        }
    }
}
