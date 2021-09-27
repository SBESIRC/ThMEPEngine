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
using ThControlLibraryWPF.CustomControl;
using ThMEPHVAC.FanLayout.ViewModel;

namespace TianHua.Hvac.UI.UI
{
    /// <summary>
    /// uiFanInfoWidget.xaml 的交互逻辑
    /// </summary>
    public partial class uiFanInfoWidget : ThCustomWindow
    {
        public uiFanInfoWidget()
        {
            InitializeComponent();
        }

        public ThFanConfigInfo GetFanConfigInfo()
        {
            var fanInfo = new ThFanConfigInfo();
            return fanInfo;
        }
    }
}
