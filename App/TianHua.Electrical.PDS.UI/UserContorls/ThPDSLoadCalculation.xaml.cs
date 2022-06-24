using System.Windows.Controls;
using TianHua.Electrical.PDS.UI.ViewModels;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    /// <summary>
    /// Interaction logic for ThPDSLoadCalculation.xaml
    /// </summary>
    public partial class ThPDSLoadCalculation : UserControl
    {
        public ThPDSLoadCalculation()
        {
            InitializeComponent();
            this.DataContext = new ThPDSLoadCalculationVM();
        }
    }
}
