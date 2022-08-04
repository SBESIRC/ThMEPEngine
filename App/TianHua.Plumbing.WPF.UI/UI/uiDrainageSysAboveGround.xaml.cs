using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ThControlLibraryWPF;
using ThControlLibraryWPF.ControlUtils;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;
using ThMEPWSS.Common;
using ThMEPWSS.Model;
using ThMEPWSS.Service;
using ThMEPWSS.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiDrainageSystemAboveGround.xaml 的交互逻辑
    /// </summary>
    public partial class uiDrainageSysAboveGround : ThCustomWindow
    {
        //防止用户重复点击按钮，这里将窗体隐藏用户选择完成后在显示，
        //或将按钮全部置为不可用状态，选择完成后在进行可用
        //或使用一个标志，根据标志的值判断是否再执行中，如果执行中，就不再执行代码
        //或使用进度条，或等待页面
        static ShowListViewModel viewModel = null;
        static DrainageSystemAGViewmodel setViewModel = null;
        bool _createFrame = false;
        bool _readFloor = false;
        uiPipeDrawControl uiPipeDraw;
        public uiDrainageSysAboveGround()
        {
            InitializeComponent();
            if(null == viewModel)
                viewModel = new ShowListViewModel();
            this.DataContext = viewModel;
            if(null == setViewModel)
                setViewModel = new DrainageSystemAGViewmodel();
        }

        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            var copyViewModel = ModelCloneUtil.Copy(setViewModel);
            var ui = new uiDrainageSysAboveGroundSet(copyViewModel);
            ui.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var result = ui.ShowDialog();
            if (result.Value)
            {
                setViewModel = ModelCloneUtil.Copy(copyViewModel);
            }

        }
        private void btnFloorFrame_Click(object sender, RoutedEventArgs e)
        {
            if (_createFrame)
                return;
            try
            {
                _createFrame = true;
                ThMEPWSS.Common.Utils.CreateFloorFraming();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "天华-错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally 
            {
                _createFrame = false;
            }
        }
        private void btnReadFloor_Click(object sender, RoutedEventArgs e)
        {
            if (_readFloor)
                return;
            try
            {
                _readFloor = true;
                if (viewModel.FloorFrameds == null)
                    viewModel.FloorFrameds = new ObservableCollection<FloorFramed>();
                var res = FramedReadUtil.SelectFloorFramed(out List<FloorFramed> selectList);
                if (res && null != selectList && selectList.Count>0) 
                {
                    viewModel.FloorFrameds.Clear();
                    selectList = FramedReadUtil.FloorFramedOrder(selectList,true);
                    foreach (var item in selectList)
                    {
                        viewModel.FloorFrameds.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "天华-错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally 
            {
                _readFloor = false;
            }
        }

        private void btnLayoutPipe_Click(object sender, RoutedEventArgs e)
        {
            if (null == viewModel || viewModel.FloorFrameds == null || viewModel.FloorFrameds.Count < 1)
            {
                MessageBox.Show("没有任何楼层信息，在读取楼层信息后在进行相应的操作，如果图纸中也没有楼层信息，请放置楼层信息后再进行后续操作",
                    "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            //放置用户重复点击按钮，先将按钮置为不可用，业务完成后再将按钮置为可用
            //直接设置后，后续的页面逻辑会卡UI线程，需要刷新一下界面
            try
            {
                var config = uiBlockNameConfig.staticUIBlockName.GetBlockNameList();
                FormUtil.DisableForm(gridForm);
                ThDrainSystemAboveGroundCmd thDrainSystem = new ThDrainSystemAboveGroundCmd(viewModel.FloorFrameds.ToList(), setViewModel, config);
                thDrainSystem.Execute();
                //ThDrainSysADUIService.Instance.selectFloors = viewModel.FloorFrameds.ToList();
                //ThDrainSysADUIService.Instance.viewmodel = setViewModel;
                //ThDrainSysADUIService.Instance.layerNames = config;
                //执行完成后窗口焦点不在CAD上，CAD界面不会及时更新，触发焦点到CAD
                ThMEPWSS.Common.Utils.FocusToCAD();
                //CommandHandlerBase.ExecuteFromCommandLine(false, "THTCHPIPIMP");
                //ThMEPWSS.Common.Utils.FocusToCAD();
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

        private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //导航定位选中的楼层框
            var selectItem = ((ListBox)sender).SelectedValue;
            if (null == selectItem)
                return;
            var selectFloor = (FloorFramed)selectItem;
            if (null == selectItem)
                return;
            Interaction.ZoomObjects(new List<ObjectId> { selectFloor.blockId });
        }

        private void btnDrawPipe_Click(object sender, RoutedEventArgs e)
        {
            //放置重复打开
            if (null != uiPipeDraw && uiPipeDraw.IsLoaded)
            {
                uiPipeDraw.ShowActivated = true;
                return;
            }
            try
            {
                this.Hide();
                uiPipeDraw = new uiPipeDrawControl();
                uiPipeDraw.Owner = this;
                uiPipeDraw.Closed += ChildWindowClosed;
                uiPipeDraw.Show();
            }
            catch
            {
                this.Show();
            }
        }
        public void ChildWindowClosed(object sender, EventArgs e)
        {
            this.Show();
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

        private void btn_Help_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var web = "http://thlearning.thape.com.cn/kng/view/video/a0420d20d3ca402d89fdf88be1fb73b2.html";
                System.Diagnostics.Process.Start(web);
            }
            catch (Exception ex)
            {
                MessageBox.Show("抱歉，出现未知错误\r\n" + ex.Message);
            }
        }
    }
    class ShowListViewModel : NotifyPropertyChangedBase
    {
        private ObservableCollection<FloorFramed> floorFrameds { get; set; }
        public ObservableCollection<FloorFramed> FloorFrameds
        {
            get { return floorFrameds; }
            set
            {
                floorFrameds = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
