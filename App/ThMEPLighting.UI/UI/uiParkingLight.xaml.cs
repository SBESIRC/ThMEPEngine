using AcHelper;
using AcHelper.Commands;
using System;
using System.Windows;
using ThControlLibraryWPF.ControlUtils;
using ThControlLibraryWPF.CustomControl;
using ThMEPLighting.ServiceModels;
using ThMEPLighting.UI.ViewModels;

namespace ThMEPLighting.UI.UI
{
    /// <summary>
    /// uiParkingLight.xaml 的交互逻辑
    /// </summary>
    public partial class uiParkingLight : ThCustomWindow
    {
        static ParkingLightViewModel parkingLightView;
        public uiParkingLight()
        {
            InitializeComponent();
            MutexName = "UIPARKINGLIGHT";
            if (null == parkingLightView)
                parkingLightView = new ParkingLightViewModel();
            this.DataContext = parkingLightView;
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
        private void PickParkingBlockLayers(bool isExternal)
        {
            var strLayer = PickParkingBlockLayerName(isExternal, out string blockName);
            if (string.IsNullOrEmpty(strLayer))
                return;
            bool isAddToView = true;
            foreach (var item in parkingLightView.PickLayerNames)
            {
                isAddToView = !item.Value.Equals(strLayer);
                if (!isAddToView)
                {
                    item.IsSelect = true;
                    break;
                }
            }
            if (isAddToView)
                parkingLightView.PickLayerNames.Add(new MultiCheckItem(strLayer, strLayer, true));
        }
        private void PickParkingBlockNames(bool isExternal)
        {
            var strLayer = PickParkingBlockLayerName(isExternal, out string blockName);
            if (string.IsNullOrEmpty(blockName))
                return;
            bool isAddToView = true;
            foreach (var item in parkingLightView.PickBlockNames)
            {
                isAddToView = !item.Value.Equals(blockName);
                if (!isAddToView)
                {
                    item.IsSelect = true;
                    break;
                }
            }
            if (isAddToView)
                parkingLightView.PickBlockNames.Add(new MultiCheckItem(blockName, blockName, true));
        }
        string PickParkingBlockLayerName(bool isExternal,out string blockName) 
        {
            string layerName = string.Empty;
            blockName = string.Empty;
            try
            {
                this.Hide();
                PickEntityCommand pickEntityCommand = new PickEntityCommand();
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

        private void btnLayoutLight_Click(object sender, RoutedEventArgs e)
        {
            var type = (Common.EnumParkingSource)parkingLightView.ParkSourcesSelect.Value;
            if (type == Common.EnumParkingSource.OnlyBlockName)
            {
                bool haveSelectBlock = false;
                foreach (var item in parkingLightView.PickBlockNames)
                {
                    if (item.IsSelect)
                    {
                        haveSelectBlock = true;
                        break;
                    }
                }
                if (!haveSelectBlock)
                {
                    MessageBox.Show("选择了仅块名称，但没有选择相应的块名称，请选择块名称后再进行后续操作");
                    return;
                }
            }
            BtnClick(true);
        }

        private void btnConnectLine_Click(object sender, RoutedEventArgs e)
        {
            BtnClick(false);
        }
        private void BtnClick(bool isLight) 
        {
            if (!base.CheckInputData())
            {
                MessageBox.Show("输入的数据有错误，请检查输入后在进行后续操作", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                if (Active.Document == null)
                    return;
                FormUtil.DisableForm(gridForm);
                SetConfig(isLight);
                string commandName = isLight ? "THCWZMBZ" : "THCWZMLX";
                CommandHandlerBase.ExecuteFromCommandLine(false, commandName);
                FocusToCAD();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "天华-错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                FormUtil.EnableForm(gridForm);
            }
        }
        private void SetConfig(bool isLight) 
        {
            if (isLight)
            {
                ThParkingStallService.Instance.ParkingLayerNames.Clear();
                ThParkingStallService.Instance.BlockScale = parkingLightView.ScaleSelect.Value;
                ThParkingStallService.Instance.SetLightDir(parkingLightView.LightDirSelect.Value == 1);
                ThParkingStallService.Instance.ParkingLayerNames.Clear();
                ThParkingStallService.Instance.ParkingBlockNames.Clear();
                ThParkingStallService.Instance.ParkingSource = (Common.EnumParkingSource)parkingLightView.ParkSourcesSelect.Value;
                switch (ThParkingStallService.Instance.ParkingSource) 
                {
                    case Common.EnumParkingSource.OnlyLayerName://仅图层名称
                        AddLayerNames();
                        break;
                    case Common.EnumParkingSource.OnlyBlockName://仅块名称
                        AddBlockNames();
                        break;
                    case Common.EnumParkingSource.BlokcAndLayer://图层和块
                        AddBlockNames();
                        AddLayerNames();
                        break;
                }
            }
            else 
            {
                ThParkingStallService.Instance.GroupMaxLightCount = parkingLightView.GroupMaxCount;
            }
        }
        void AddBlockNames() 
        {
            if (parkingLightView.PickBlockNames != null && parkingLightView.PickBlockNames.Count > 0)
            {
                foreach (var item in parkingLightView.PickBlockNames)
                {
                    if (!item.IsSelect)
                        continue;
                    ThParkingStallService.Instance.ParkingBlockNames.Add(item.Value);
                }
            }
        }
        void AddLayerNames()
        {
            if (parkingLightView.PickLayerNames != null && parkingLightView.PickLayerNames.Count > 0)
            {
                foreach (var item in parkingLightView.PickLayerNames)
                {
                    if (!item.IsSelect)
                        continue;
                    ThParkingStallService.Instance.ParkingLayerNames.Add(item.Value);
                }
            }
        }
        void FocusToCAD()
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
