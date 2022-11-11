using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.ApplicationServices;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ThPlatform3D;
using ThPlatform3D.Model.Project;
using Tianhua.Platform3D.UI.Interfaces;
using Tianhua.Platform3D.UI.UControls;
using Tianhua.Platform3D.UI.ViewModels;
using CADApplication = Autodesk.AutoCAD.ApplicationServices.Application;

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
            mainViewModel.UserName = "未登录";
            mainViewModel.ProjectName = "未绑定";
            mainViewModel.SubProjectName = "未绑定";
            mainViewModel.MajorName = "未绑定";
            mainViewModel.FunctionTableItems.Add(new FunctionTabItem("楼层", new StoreyElevationSetUI()));
            mainViewModel.FunctionTableItems.Add(new FunctionTabItem("设计", new DesignUControl()));
            this.DataContext = mainViewModel;
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
            FocusToCAD();
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

        void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
                    Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            var login = new Login();
            var res = CADApplication.ShowModalWindow(CADApplication.MainWindow.Handle, login, false);
            if (res != true)
                return;
            //登录成功
            var loginUser = login.UserLoginInfo();
            ConfigService.ConfigInstance.LoginUser = loginUser;
            mainViewModel.UserName = loginUser.ChineseName;
        }

        private void btnProject_Click(object sender, RoutedEventArgs e)
        {
            var user = ConfigService.ConfigInstance.LoginUser;
#if DEBUG
            if (null == user)
            {
                user = new ThPlatform3D.Model.User.UserInfo();
                user.UserLogin = new ThPlatform3D.Model.User.UserLoginRes();
                user.UserLogin.Username = "thtestuser";
                user.PreSSOId = "TU1909XQ";
                user.ChineseName = "测试用户";
            }
#endif
            if(null == user) 
            {
                MessageBox.Show("没有登录，无法进行绑定操作,请登录后在进行相应的相应的操作。", "提醒", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var hisPrjId = ConfigService.ConfigInstance.BindingPrjId;
            var hisSubPrjId = ConfigService.ConfigInstance.BindingSubPrjId;
            var hisMajor = ConfigService.ConfigInstance.BindingMajor;
            var bindingPrj = new BindingProject(user, hisPrjId, hisSubPrjId, hisMajor);
            var res = CADApplication.ShowModalWindow(CADApplication.MainWindow.Handle, bindingPrj, false);
            if (res != true)
                return;
            var majorName = bindingPrj.GetBindingResult(out DBProject dbProject, out DBSubProject subProject);
            ConfigService.ConfigInstance.BindingProjectMajor(dbProject, subProject, majorName);
            mainViewModel.ProjectName = dbProject.PrjName;
            mainViewModel.SubProjectName = subProject.SubEntryName;
            mainViewModel.MajorName = majorName;
        }
    }
}
