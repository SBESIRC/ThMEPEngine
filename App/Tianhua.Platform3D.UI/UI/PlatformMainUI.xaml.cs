using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.ApplicationServices;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Tianhua.Platform3D.UI.Interfaces;
using Tianhua.Platform3D.UI.UControls;
using Tianhua.Platform3D.UI.ViewModels;

namespace Tianhua.Platform3D.UI.UI
{
    /// <summary>
    /// PlatformMainUI.xaml 的交互逻辑
    /// </summary>
    public partial class PlatformMainUI : UserControl, IMultiDocument
    {
        MainFunctionViewModel mainViewModel;
        private List<IMultiDocument> cacheFuctionPages;
        public PlatformMainUI()
        {
            InitializeComponent();
            cacheFuctionPages = new List<IMultiDocument>();
            this.Loaded += PlatformMainUI_Loaded;
        }

        private void PlatformMainUI_Loaded(object sender, RoutedEventArgs e)
        {
            InitMainViewModel();
            InitPropertyViewModel();
        }

        private void InitMainViewModel() 
        {
            mainViewModel = new MainFunctionViewModel();
            mainViewModel.FunctionTableItems.Add(new FunctionTabItem("楼层", new StoreyElevationSetUI()));
            mainViewModel.FunctionTableItems.Add(new FunctionTabItem("设计", new DesignUControl()));
            tabTopFunction.DataContext = mainViewModel;
            tabTopFunction.SelectedIndex = 1;
            cacheFuctionPages = AllTablePage();
        }

        private void InitPropertyViewModel() 
        {
            PropertiesViewModel.Instacne.InitPropertyGrid(propertyGrid);
            propGrid.DataContext = PropertiesViewModel.Instacne;
        }
        #region 页面相应事件
        private void btnPushToSU_Click(object sender, RoutedEventArgs e)
        {
            SendCommand("THCAD2SUPUSH");
        }

        private void btnPushToViewer_Click(object sender, RoutedEventArgs e)
        {
            SendCommand("THCAD2P3DPUSH");
        }
        private void SendCommand(string cmdName) 
        {
            if (Active.Document == null)
                return;
            CommandHandlerBase.ExecuteFromCommandLine(false, cmdName);
        }
        #endregion

        private void propertyGrid_SelectedObjectChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

        }

        #region 多文档相关事件
        public void MainUIShowInDocument()
        {
            if (cacheFuctionPages.Count < 1)
                return;
            foreach (var item in cacheFuctionPages)
                item.MainUIShowInDocument();
        }
        public void DocumentActivated(DocumentCollectionEventArgs e)
        {
            if (cacheFuctionPages.Count < 1)
                return;
            foreach (var item in cacheFuctionPages)
                item.DocumentActivated(e);
        }

        public void DocumentDestroyed(DocumentDestroyedEventArgs e)
        {
            if (cacheFuctionPages.Count < 1)
                return;
            foreach (var item in cacheFuctionPages)
                item.DocumentDestroyed(e);
        }

        public void DocumentToBeActivated(DocumentCollectionEventArgs e)
        {
            if (cacheFuctionPages.Count < 1)
                return;
            foreach (var item in cacheFuctionPages)
                item.DocumentToBeActivated(e);
        }

        public void DocumentToBeDestroyed(DocumentCollectionEventArgs e)
        {
            if (cacheFuctionPages.Count < 1)
                return;
            foreach (var item in cacheFuctionPages)
                item.DocumentToBeDestroyed(e);
        }
        private List<IMultiDocument> AllTablePage() 
        {
            var res = new List<IMultiDocument>();
            foreach (var item in mainViewModel.FunctionTableItems) 
            {
                if(item.UControl is IMultiDocument iMutil)
                {
                    res.Add(iMutil);
                }
            }
            return res;
        }
        #endregion
    }
}
