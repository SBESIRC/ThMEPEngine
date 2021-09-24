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
            PickParkingBlock(false);
        }

        private void btnPickExternal_Click(object sender, RoutedEventArgs e)
        {
            PickParkingBlock(true);
        }
        private void PickParkingBlock(bool isExternal) 
        {
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
                    return;
                string layerName = pickEntityCommand.GetEntityLayerName();
                var blockName = pickEntityCommand.GetBlockName();
                if (string.IsNullOrEmpty(layerName))
                    return;
                bool isAddToView = true;
                foreach (var item in parkingLightView.PickLayerNames) 
                {
                    if (!isAddToView)
                        break;
                    isAddToView = !item.Value.Equals(layerName);
                }
                if (isAddToView)
                    parkingLightView.PickLayerNames.Add(new MultiCheckItem(layerName, layerName, true));
            }
            catch (Exception ex) 
            { }
            finally
            {
                this.Show();
            }
        }

        private void btnLayoutLight_Click(object sender, RoutedEventArgs e)
        {
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
            else 
            {
                ThParkingStallService.Instance.GroupMaxLightCount = parkingLightView.GroupMaxCount;
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
