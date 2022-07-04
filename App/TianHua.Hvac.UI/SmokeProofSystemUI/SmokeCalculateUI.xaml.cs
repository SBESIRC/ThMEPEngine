using AcHelper;
using AcHelper.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ThMEPHVAC;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels;
using TianHua.Hvac.UI.SmokeProofSystemUI.SmokeProofUserControl;

namespace TianHua.Hvac.UI.SmokeProofSystemUI
{
    /// <summary>
    /// SmokeCalculateUI.xaml 的交互逻辑
    /// </summary>
    public partial class SmokeCalculateUI : ThCustomWindow
    {
        public SmokeCalculateUI()
        {
            InitData();
            this.DataContext = ThMEPHVACStaticService.Instance.smokeCalculateViewModel;
            InitializeComponent();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        public void InitData()
        {
            if (ThMEPHVACStaticService.Instance.smokeCalculateViewModel == null)
            {
                ThMEPHVACStaticService.Instance.smokeCalculateViewModel = new SmokeCalculateViewModel();
                ThMEPHVACStaticService.Instance.smokeCalculateViewModel.FunctionTableItems = new ObservableCollection<UTableItem>() {
                    new UTableItem("消防电梯前室", new FireElevatorFrontRoomUserControl()),
                    new UTableItem("独立或合用前室（楼梯间自然）", new SeparateOrSharedNaturalUserControl()),
                    new UTableItem("独立或合用前室（楼梯间送风）", new SeparateOrSharedWindUserControl()),
                    new UTableItem("楼梯间（前室不送风）", new StaircaseNoWindUserControl()),
                    new UTableItem("楼梯间（前室送风）", new StaircaseWindUserControl()),
                    new UTableItem("封闭避难层（间）、避难走道", new EvacuationWalkUserControl()),
                    new UTableItem("避难走道前室", new EvacuationFrontUserControl()),
                };
                ThMEPHVACStaticService.Instance.smokeCalculateViewModel.SelectTableItem = ThMEPHVACStaticService.Instance.smokeCalculateViewModel.FunctionTableItems[0];
            }
            if (ThMEPHVACStaticService.Instance.smokeCalculateMappingModel != null)
            {
                ThMEPHVACStaticService.Instance.smokeCalculateViewModel.SelectTableItem =
                    ThMEPHVACStaticService.Instance.smokeCalculateViewModel.FunctionTableItems.FirstOrDefault(x => x.Title == ThMEPHVACStaticService.Instance.smokeCalculateMappingModel.ScenarioTitle);
                ThMEPHVACStaticService.Instance.smokeCalculateViewModel.AirSupplySelectTableItem =
                    ThMEPHVACStaticService.Instance.smokeCalculateViewModel.AirSupplyTableItems.FirstOrDefault(x => x.Title == ThMEPHVACStaticService.Instance.smokeCalculateMappingModel.AirSupplyTitle);
                ThMEPHVACStaticService.Instance.smokeCalculateMappingModel = null;
            }
        }

        private void btnLayoutBlock_Click(object sender, RoutedEventArgs e)
        {
            // 发送命令
            SetFocusToDwgView();
            CommandHandlerBase.ExecuteFromCommandLine(false, "THLXSPS");
        }

        /// <summary>
        /// 聚焦到CAD
        /// </summary>
        private void SetFocusToDwgView()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
    }
}
