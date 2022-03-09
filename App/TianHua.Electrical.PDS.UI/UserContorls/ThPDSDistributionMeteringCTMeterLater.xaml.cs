using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    /// <summary>
    /// ThPDSDistributionMeteringCTMeterLater.xaml 的交互逻辑
    /// </summary>
    public partial class ThPDSDistributionMeteringCTMeterLater : UserControl
    {
        public ThPDSDistributionMeteringCTMeterLater()
        {
            InitializeComponent();
            var Rects = new List<Rect>();
            Rects.Add(new Rect(100, 100, 200, 200));
            Rects.Add(new Rect(200, 200, 300, 300));
            this.DataContext = new { Rects };
        }

    }
}
