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
using TianHua.Hvac.UI.SmokeProofSystemUI.ViewModels;

namespace TianHua.Hvac.UI.SmokeProofSystemUI
{
    /// <summary>
    /// SmokeCalculateUI.xaml 的交互逻辑
    /// </summary>
    public partial class SmokeCalculateUI : ThCustomWindow
    {
        static SmokeCalculateViewModel smokeCalculateViewModel;
        public SmokeCalculateUI()
        {
            InitData();
            this.DataContext = smokeCalculateViewModel;
            InitializeComponent();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        public void InitData()
        {
            if (smokeCalculateViewModel == null)
            {
                smokeCalculateViewModel = new SmokeCalculateViewModel();
            }
        }
    }
}
