using AcHelper;
using System.Windows;
using ThControlLibraryWPF.CustomControl;
using ThMEPElectrical;
using ThMEPElectrical.ChargerDistribution.Command;
using ThMEPElectrical.ChargerDistribution.Service;
using ThMEPElectrical.ViewModel;

namespace TianHua.Electrical.UI.UI
{
    /// <summary>
    /// ChargerDistributionUI.xaml 的交互逻辑
    /// </summary>
    public partial class ChargerDistributionUI : ThCustomWindow
    {
        private static ThChargerDistributionVM ChargingEquipmentVM;

        public ChargerDistributionUI()
        {
            InitializeComponent();
            if (null == ChargingEquipmentVM)
                ChargingEquipmentVM = new ThChargerDistributionVM();
            this.DataContext = ChargingEquipmentVM;
        }

        private void btnPickModel_Click(object sender, RoutedEventArgs e)
        {
            PickParkingBlockLayers(false);
        }

        private void btnPickExternal_Click(object sender, RoutedEventArgs e)
        {
            PickParkingBlockLayers(true);
        }

        private void btnPickBlockModel_Click(object sender, RoutedEventArgs e)
        {
            PickParkingBlockNames(false);
        }

        private void btnPickBlockExternal_Click(object sender, RoutedEventArgs e)
        {
            PickParkingBlockNames(true);
        }

        private void PickParkingBlockNames(bool isExternal)
        {
            PickParkingBlockLayerName(isExternal, out string blockName);
            if (string.IsNullOrEmpty(blockName))
                return;
            bool isAddToView = true;
            foreach (var item in ChargingEquipmentVM.PickBlockNames)
            {
                isAddToView = !item.Value.Equals(blockName);
                if (!isAddToView)
                {
                    item.IsSelect = true;
                    break;
                }
            }
            if (isAddToView)
                ChargingEquipmentVM.PickBlockNames.Add(new MultiCheckItem(blockName, blockName, true));
        }

        private void PickParkingBlockLayers(bool isExternal)
        {
            var strLayer = PickParkingBlockLayerName(isExternal, out string blockName);
            if (string.IsNullOrEmpty(strLayer))
                return;
            bool isAddToView = true;
            foreach (var item in ChargingEquipmentVM.PickLayerNames)
            {
                isAddToView = !item.Value.Equals(strLayer);
                if (!isAddToView)
                {
                    item.IsSelect = true;
                    break;
                }
            }
            if (isAddToView)
                ChargingEquipmentVM.PickLayerNames.Add(new MultiCheckItem(strLayer, strLayer, true));
        }

        private string PickParkingBlockLayerName(bool isExternal, out string blockName)
        {
            string layerName = string.Empty;
            blockName = string.Empty;
            try
            {
                this.Hide();
                var pickEntityCommand = new PickEntityCommand();
                bool selectSucceed = false;
                if (isExternal)
                    selectSucceed = pickEntityCommand.PickExternalBlock("请选择外参中的车位块");
                else
                    selectSucceed = pickEntityCommand.PickModelSpaceBlock("请选择本图纸中的车位块");
                if (!selectSucceed)
                    return layerName;
                layerName = pickEntityCommand.GetEntityLayerName();
                blockName = pickEntityCommand.GetBlockName();
                return layerName;
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                this.Show();
            }
        }

        private void SetConfig()
        {
            ThParkingStallService.Instance.ParkingLayerNames.Clear();
            ThParkingStallService.Instance.ParkingLayerNames.Clear();
            ThParkingStallService.Instance.ParkingBlockNames.Clear();
            AddBlockNames();
            AddLayerNames();
        }

        private void AddBlockNames()
        {
            if (ChargingEquipmentVM.PickBlockNames != null && ChargingEquipmentVM.PickBlockNames.Count > 0)
            {
                foreach (var item in ChargingEquipmentVM.PickBlockNames)
                {
                    if (!item.IsSelect)
                        continue;
                    ThParkingStallService.Instance.ParkingBlockNames.Add(item.Value);
                }
            }
        }

        private void AddLayerNames()
        {
            if (ChargingEquipmentVM.PickLayerNames != null && ChargingEquipmentVM.PickLayerNames.Count > 0)
            {
                foreach (var item in ChargingEquipmentVM.PickLayerNames)
                {
                    if (!item.IsSelect)
                        continue;
                    ThParkingStallService.Instance.ParkingLayerNames.Add(item.Value);
                }
            }
        }

        /// <summary>
        /// 布置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLayout_Click(object sender, RoutedEventArgs e)
        {
            SetConfig();
            FocusToCAD();
            var cmd = new ThChargerLayoutCmd();
            cmd.Execute();
        }

        /// <summary>
        /// 统计
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCount_Click(object sender, RoutedEventArgs e)
        {
            FocusToCAD();
            var cmd = new ThChargerCountCmd();
            cmd.Execute();
        }

        /// <summary>
        /// 分组
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGroup_Click(object sender, RoutedEventArgs e)
        {
            FocusToCAD();
            var cmd = new ThChargerGroupingCmd();
            cmd.Execute();
        }

        /// <summary>
        /// 调整分组
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnModify_Click(object sender, RoutedEventArgs e)
        {
            FocusToCAD();
            var cmd = new ThChargerGroupModifyCmd();
            cmd.Execute();
        }

        /// <summary>
        /// 编号
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNumber_Click(object sender, RoutedEventArgs e)
        {
            SetConfig();
            FocusToCAD();
            var cmd = new ThChargerNumberCmd();
            cmd.Execute();
        }

        private void FocusToCAD()
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
