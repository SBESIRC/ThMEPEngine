﻿using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Dreambuild.AutoCAD;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.WaterWellPumpLayout.Command;
using ThMEPWSS.WaterWellPumpLayout.Model;
using ThMEPWSS.WaterWellPumpLayout.ViewModel;
using ThMEPWSS.WaterWellPumpLayout.Service;
using cadGraph = Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Geometry;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiWaterWellPumpInfo.xaml 的交互逻辑
    /// </summary>
    public partial class uiWaterWellPumpInfo : ThCustomWindow
    {
        public List<ThWaterWellModel> WaterWellList { set; get; }//选取到的集水井
        public WaterWellIdentifyConfigInfo IdentifyInfo { set; get; }//名单配置
        private static ThWaterWellConfigViewModel ViewModel = null;
        public uiWaterWellPumpInfo()
        {
            WaterWellList = new List<ThWaterWellModel>();
            InitializeComponent();
            if (ViewModel == null)
            {
                ViewModel = new ThWaterWellConfigViewModel();
            }
            DataContext = ViewModel;
        }
        public static void FocusMainWindow()
        {
#if ACAD_ABOVE_2014
            Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Focus();
#else
            FocusToCAD();
#endif
        }
        public static void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
        public ThWaterWellConfigViewModel GetViewModel()
        {
            return ViewModel;
        }
        private void btnCleanData_Click(object sender, RoutedEventArgs e)
        {
            WaterWellList.Clear();
            ViewModel.WellConfigInfo.Clear();
        }
        private void btnSelectWell_Click(object sender, RoutedEventArgs e)
        {
            WaterWellIdentifyConfigInfo identifyInfo = new WaterWellIdentifyConfigInfo();
            var config = uiBlockNameConfig.staticUIBlockName.GetBlockNameList();
            identifyInfo.WhiteList = config["集水井"];
            IdentifyInfo = identifyInfo;

            var selectWellCmd = new ThSelectWaterWellCmd(IdentifyInfo);
            selectWellCmd.Execute();
            var wellList = selectWellCmd.WaterWellList;

            UpdateWellList(wellList);
            UpdateUIWellList();

        }
        public void HighlightWell()
        {
            FocusMainWindow();
            using (var acadDatabase = AcadDatabase.Active())
            {
                var tm = cadGraph.TransientManager.CurrentTransientManager;
                IntegerCollection intCol = new IntegerCollection();
                if (ViewModel.WellConfigInfo != null)
                {
                    foreach (var info in ViewModel.WellConfigInfo)
                    {
                        if (info.IsDisplay)
                        {
                            info.WellModelList.ForEach(w =>
                            {
                                tm.AddTransient(w.WellObb, cadGraph.TransientDrawingMode.Highlight, 1, intCol);
                            });
                        }
                    }
                }
            }
        }
        private void ThCustomWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            FocusMainWindow();
            using (var acadDatabase = AcadDatabase.Active())
            {
                var tm = cadGraph.TransientManager.CurrentTransientManager;
                IntegerCollection intCol = new IntegerCollection();
                if (ViewModel.WellConfigInfo != null)
                {
                    foreach (var info in ViewModel.WellConfigInfo)
                    {
                        if (info.IsDisplay)
                        {
                            info.WellModelList.ForEach(w =>
                            {
                                tm.EraseTransient(w.WellObb, intCol);
                            });
                        }
                    }
                }
            }
            this.Hide();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            var configInfo = checkbox.DataContext as ThWaterWellConfigInfo;
            FocusMainWindow();
            configInfo.IsDisplay = true;
            using (var acadDatabase = AcadDatabase.Active())
            {
                var tm = cadGraph.TransientManager.CurrentTransientManager;
                IntegerCollection intCol = new IntegerCollection();
                foreach (var well in configInfo.WellModelList)
                {
                    tm.AddTransient(well.WellObb, cadGraph.TransientDrawingMode.Highlight, 1, intCol);
                }
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            var configInfo = checkbox.DataContext as ThWaterWellConfigInfo;
            FocusMainWindow();
            configInfo.IsDisplay = false;
            using (var acadDatabase = AcadDatabase.Active())
            {
                var tm = cadGraph.TransientManager.CurrentTransientManager;
                IntegerCollection intCol = new IntegerCollection();
                configInfo.WellModelList.ForEach(w =>
                {
                    tm.EraseTransient(w.WellObb, intCol);
                });
            }
        }

        private void cbNotMergeDiffExRef_Click(object sender, RoutedEventArgs e)
        {
            UpdateUIWellList();
        }

        private void UpdateWellList(List<ThWaterWellModel> wellList)
        {
            foreach (var selectWell in wellList)
            {
                bool isHave = false;
                foreach (var listWell in WaterWellList)
                {
                    if (selectWell.IsEqual(listWell))
                    {
                        isHave = true;
                        listWell.PumpModel = selectWell.PumpModel;
                        listWell.FullName = selectWell.FullName;
                        //listWell.IsHavePump = selectWell.IsHavePump;
                        break;
                    }
                }
                if (!isHave)
                {
                    WaterWellList.Add(selectWell);
                }
            }
        }

        private void UpdateUIWellList()
        {
            var notMergeDiffExRef = (bool)cbNotMergeDiffExRef.IsChecked;

            var groups = new ObservableCollection<ThWaterWellConfigInfo>();

            //var tmpList = WaterWellList.Select(o => o).ToList();
            //while (tmpList.Count > 0)
            //{
            //    var first = tmpList.First();
            //    var sameTypes = tmpList.Where(o => o.IsSameType(first, notMergeDiffExRef)).ToList();

            //    ThWaterWellConfigInfo info = new ThWaterWellConfigInfo();
            //    info.WellCount = sameTypes.Count;
            //    info.WellArea = first.GetAcreage();
            //    info.BlockName = first.EffName;
            //    info.FullName = first.FullName;
            //    info.WellSize = first.GetWellSize();
            //    if (first.PumpModel != null)
            //    {
            //        info.PumpCount = first.PumpModel.VisibilityValue;
            //        info.PumpNumber = first.PumpModel.AttriValue;
            //    }
            //    info.WellModelList = sameTypes;
            //    //info需要增加 泵数量 编号
            //    sameTypes.ForEach(s => tmpList.Remove(s));
            //    groups.Add(info);
            //}

            var groupList = ThWaterWellPumpUtils.MergeWellList(WaterWellList, notMergeDiffExRef);
           
            groupList.ForEach(x => groups.Add(x));

            //处理viewModel里面的数据
            ViewModel.WellConfigInfo = groups;
        }
    }
}
