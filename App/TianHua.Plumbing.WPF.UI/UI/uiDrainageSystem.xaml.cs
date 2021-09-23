﻿using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Windows;
using System.Windows.Controls;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.Pipe.Model;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// DrainageSystemUI.xaml 的交互逻辑
    /// </summary>
    public partial class uiDrainageSystem : ThCustomWindow
    {
        static DrainageViewModel viewModel;
        public uiDrainageSystem()
        {
            InitializeComponent();
            //给水系统图相关
            if (null == viewModel)
                viewModel = new DrainageViewModel();
            this.DataContext = viewModel;
        }

        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            var oldViewModel = viewModel.SetViewModel?.Clone();
            uiDrainageSystemSet systemSet = new uiDrainageSystemSet(viewModel.SetViewModel);
            systemSet.Owner = this;
            var ret = systemSet.ShowDialog();
            if (ret == false)
            {
                //用户取消了操作
                viewModel.SetViewModel = oldViewModel;
                return;
            }
        }

        //run
        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var cmd = new ThWaterSuplySystemDiagramCmd(viewModel))
                {
                    var insertOpt = new PromptPointOptions("\n指定图纸的插入点");
                    var optRes = Active.Editor.GetPoint(insertOpt);
                    if (optRes.Status == PromptStatus.OK)
                    {
                        var blockConfig = uiBlockNameConfig.staticUIBlockName.GetBlockNameList();

                        viewModel.InsertPt = optRes.Value;
                        cmd.Execute(blockConfig);
                    }
                }
            }
            catch
            {

            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void btnSelectFloor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                viewModel.CreateFloorFraming();
            }
            catch
            {

            }
        }

        private void btnReadStoreys_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                viewModel.InitListDatas();
            }
            catch
            {

            }
        }

        private void ThCustomWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var cachedArea = CadCache.TryGetRange();
            if (cachedArea != null)
            {
                viewModel.InitListDatasByArea(cachedArea, false);
            }
        }
    }
}
