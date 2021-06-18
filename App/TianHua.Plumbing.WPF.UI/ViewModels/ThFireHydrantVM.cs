using TianHua.Plumbing.WPF.UI.Model;
using System.Collections.ObjectModel;
using TianHua.Plumbing.WPF.UI.Service;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using AcHelper.Commands;
using AcHelper;
using TianHua.Plumbing.WPF.UI.UI;

namespace TianHua.Plumbing.WPF.UI.ViewModels
{
    public class ThFireHydrantVM
    {
        public ThFireHydrantModel Parameter { get; set; }
        public ObservableCollection<string> DangerLevels { get; set; }
        public ObservableCollection<string> FireTypes { get; set; }

        private ThFireTypeDataManager FireTypeDataManager { get; set; }
        public ThFireHydrantVM()
        {
            Parameter = new ThFireHydrantModel();
            FireTypeDataManager = new ThFireTypeDataManager();
            DangerLevels = new ObservableCollection<string>(FireTypeDataManager.DangerLevels);
            FireTypes = new ObservableCollection<string>(FireTypeDataManager.FireTypes);
        }
        public ICommand RegionCheckCmd
        {
            get
            {
                return new RelayCommand(RegionCheckClick);
            }
        }
        public ICommand ProtectionStrengthCmd
        {
            get
            {
                return new RelayCommand(ProtectionStrengthClick);
            }
        }

        public ICommand WaterColumnLengthCmd
        {
            get
            {
                return new RelayCommand(WaterColumnLengthClick);
            }
        }
        private void ProtectionStrengthClick()
        {
            string tip = "室内消火栓的布置应满足同一平面有2支消防水枪的2" +
                "股充实水柱同时达到任何部位的要求，且楼梯间及其" +
                "休息平台等安全区域可仅与一层视为同一平面。但当" +
                "建筑高度小于等于24.0米且体积小于等于5000m3的" +
                "多层仓库，可采用1支水枪充实水柱到达室内任何部位。";
            var tipDialog = new ThTipDialog("保护强度", tip);
            tipDialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            tipDialog.ShowDialog();
        }
        private void WaterColumnLengthClick()
        {
            string tip = "高层建筑、厂房、库房和室内净空高度超过8m的民" +
                "用建筑等场所的消火栓栓口动压，不应小于" +
                "0.35MPa，且消防防水枪充实水柱应按13m计算；其" +
                "他场所的消火栓栓口动压不应小于0.25MPa，且消防" +
                "水枪充实水柱应按10m计算";
            var tipDialog = new ThTipDialog("水柱长度", tip);
            tipDialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            tipDialog.ShowDialog();
        }

        private void RegionCheckClick()
        {
            CollectParameter();
            SetFocusToDwgView();
            CommandHandlerBase.ExecuteFromCommandLine(false, "ThHydrantProtectRadiusCheck");
        }
        private void CollectParameter()
        {

        }
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
