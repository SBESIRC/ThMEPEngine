using System.Windows;
using System.Windows.Controls;
using TianHua.Electrical.PDS.UI.Project;
using TianHua.Electrical.PDS.UI.ViewModels;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    /// <summary>
    /// UESandboxParameter.xaml 的交互逻辑
    /// </summary>
    public partial class UESandboxParameter : UserControl
    {
        #region 测试部分代码，示例事件，后期删除删除
        //外部可以监听到相应的事件，事件要再内部触发，将事件抛出，交给外部处理
        /// <summary>
        /// 声明路由事件
        /// 参数:要注册的路由事件名称，路由事件的路由策略，事件处理程序的委托类型(可自定义)，路由事件的所有者类类型
        /// </summary>
        public static readonly RoutedEvent OnTestEvevnt = EventManager.RegisterRoutedEvent("TestEvevnt", RoutingStrategy.Bubble, typeof(RoutedEventArgs), typeof(UESandboxParameter));
        /// <summary>
        /// 处理各种路由事件的方法 
        /// </summary>
        public event RoutedEventHandler TestEvevnt
        {
            //将路由事件添加路由事件处理程序
            add { AddHandler(OnTestEvevnt, value, false); }
            //从路由事件处理程序中移除路由事件
            remove { RemoveHandler(OnTestEvevnt, value); }
        }
        #endregion
        TestUControlViewModel testUControlView;
        public UESandboxParameter()
        {
            InitializeComponent();
            InitViewModelData();
        }

        #region
        void InitViewModelData() 
        {
            //该处写的测试ViewModel.给该UserControl单独绑定ViewModel,不受外部的影响。后期删除删除
            testUControlView = new TestUControlViewModel();
            testUControlView.FunctionTableItems.Add(new Models.UTableItem("Test1", null));
            testUControlView.FunctionTableItems.Add(new Models.UTableItem("Test2", null));
            gridUESDParameter.DataContext = testUControlView;
        }
        #endregion

        #region 界面下方按钮响应事件
        private void btnExportSet_Click(object sender, RoutedEventArgs e)
        {
            #region 测试代码触发事件，外部可以监听到
            this.RaiseEvent(new RoutedEventArgs(OnTestEvevnt, sender as Button));
            #endregion
        }

        private void btnImportSet_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSaveSet_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion
    }
}
