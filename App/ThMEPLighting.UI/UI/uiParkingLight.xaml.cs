using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
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
            string msg = ParkingStallCheckData();
            if (!string.IsNullOrEmpty(msg)) 
            {
                MessageBox.Show(msg, "天华-提醒");
                return;
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
                ThParkingStallService.Instance.SetLightDir(parkingLightView.LightDirSelect.Value == 2);
                ThParkingStallService.Instance.ParkingLayerNames.Clear();
                ThParkingStallService.Instance.ParkingBlockNames.Clear();
                ThParkingStallService.Instance.ParkingSource = (Common.EnumParkingSource)parkingLightView.ParkSourcesSelect.Value;
                ThParkingStallService.Instance.ParkingStallIllumination = parkingLightView.parkingStallIllumination;
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

        string ParkingStallCheckData() 
        {
            //现在改为获取块和名称都支持
            string errorMsg = "";
            var type = (Common.EnumParkingSource)parkingLightView.ParkSourcesSelect.Value;
            if (type == Common.EnumParkingSource.OnlyBlockName)
            {
                if (!HaveSelectBlock())
                {
                    errorMsg = "选择了仅块名称，但没有选择相应的块名称，请选择块名称后再进行后续操作";
                }
            }
            else if (type == Common.EnumParkingSource.BlokcAndLayer) 
            {
                if (!HaveSelectBlock() && !HaveSelectLayer())
                {
                    errorMsg = "没有选择任何块名称或图层名称，无法进行后续操作，请选择后再进行后续操作";
                }
            }
            return errorMsg;
        }

        bool HaveSelectBlock() 
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
            return haveSelectBlock;
        }

        bool HaveSelectLayer() 
        {
            bool haveSelectLayer = false;
            foreach (var item in parkingLightView.PickLayerNames)
            {
                if (item.IsSelect)
                {
                    haveSelectLayer = true;
                    break;
                }
            }
            return haveSelectLayer;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            HideLayer("AI-车位照度",false);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            HideLayer("AI-车位照度", true);
        }

        /// <summary>
        /// 图层的显隐
        /// </summary>
        /// <param name="hideLayer"></param>
        /// <param name="isHide"></param>
        void HideLayer(string layerName,bool isHide)
        {
            if (Active.Document == null)
                return;
            bool isRegen = false;
            using (Active.Document.LockDocument())
            using (var db = AcadDatabase.Active())
            {
                FocusToCAD();
                if (!isHide)
                {
                    bool layerIndb = false;
                    foreach (var layer in db.Layers)
                    {
                        if (layerName != layer.Name)
                            continue;
                        layerIndb = true;
                        break;
                    }
                    if (layerIndb)
                    {
                        DbHelper.EnsureLayerOn(layerName);
                        isRegen = true;
                    }
                } 
                else
                {
                    foreach (var layer in db.Layers)
                    {
                        if (layerName != layer.Name)
                            continue;
                        layer.UpgradeOpen();
                        layer.IsOff = isHide;
                        layer.DowngradeOpen();
                        break;
                    }
                }
            }
            if(isRegen)//图层解冻后，有些还是看不到
                CommandHandlerBase.ExecuteFromCommandLine(false, "REGEN");
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://thlearning.thape.com.cn/kng/view/video/902bbe26017242b58b255051f7bbbd79.html?m=1&view=1");
        }
    }
}
