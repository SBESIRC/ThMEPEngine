using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TianHua.Hvac.UI.UI.IndoorFan
{
    /// <summary>
    /// uTabRadio.xaml 的交互逻辑
    /// </summary>
    public partial class uTabRadio : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// 声明路由事件
        /// 参数:要注册的路由事件名称，路由事件的路由策略，事件处理程序的委托类型(可自定义)，路由事件的所有者类类型
        /// </summary>
        public static readonly RoutedEvent OnTabRadioItemAdd = EventManager.RegisterRoutedEvent("TabRadioItemAdd", RoutingStrategy.Bubble, typeof(RoutedEventArgs), typeof(uTabRadio));
        /// <summary>
        /// 处理各种路由事件的方法 
        /// </summary>
        public event RoutedEventHandler TabRadioItemAdd
        {
            //将路由事件添加路由事件处理程序
            add { AddHandler(OnTabRadioItemAdd, value, false); }
            //从路由事件处理程序中移除路由事件
            remove { RemoveHandler(OnTabRadioItemAdd, value); }
        }
        /// <summary>
        /// 声明路由事件
        /// 参数:要注册的路由事件名称，路由事件的路由策略，事件处理程序的委托类型(可自定义)，路由事件的所有者类类型
        /// </summary>
        public static readonly RoutedEvent OnTabRadioItemDeleted = EventManager.RegisterRoutedEvent("TabRadioItemDeleted", RoutingStrategy.Bubble, typeof(RoutedEventArgs), typeof(uTabRadio));
        /// <summary>
        /// 处理各种路由事件的方法 
        /// </summary>
        public event RoutedEventHandler TabRadioItemDeleted
        {
            //将路由事件添加路由事件处理程序
            add { AddHandler(OnTabRadioItemDeleted, value, false); }
            //从路由事件处理程序中移除路由事件
            remove { RemoveHandler(OnTabRadioItemDeleted, value); }
        }
        public static readonly RoutedEvent OnTabRadioSelectChanged = EventManager.RegisterRoutedEvent("TabRadioSelectChanged", RoutingStrategy.Bubble, typeof(RoutedEventArgs), typeof(uTabRadio));
        /// <summary>
        /// 处理各种路由事件的方法 
        /// </summary>
        public event RoutedEventHandler TabRadioSelectChanged
        {
            //将路由事件添加路由事件处理程序
            add { AddHandler(OnTabRadioSelectChanged, value, false); }
            //从路由事件处理程序中移除路由事件
            remove { RemoveHandler(OnTabRadioSelectChanged, value); }
        }
        public string GroupName { get; set; } = System.Guid.NewGuid().ToString();
        public int MinTabCount { get; set; } = 1;
        public readonly static DependencyProperty HaveAddButtonProperty = DependencyProperty.Register("HaveAddButton", typeof(bool), typeof(uTabRadio), new PropertyMetadata(false));
        public bool HaveAddButton
        {
            get { return (bool)GetValue(HaveAddButtonProperty); }
            set { SetValue(HaveAddButtonProperty, value); }
        }
        public static readonly DependencyProperty SelectRadioTabItemProperty = DependencyProperty.Register("SelectRadioTabItem", typeof(TabRadioItem), typeof(uTabRadio), new PropertyMetadata(null));
        public TabRadioItem SelectRadioTabItem
        {
            get { return (TabRadioItem)GetValue(SelectRadioTabItemProperty); }
            set
            {
                if (SelectRadioTabItem != null)
                {
                    SelectRadioTabItem.InEdit = false;
                }
                SetValue(SelectRadioTabItemProperty, value);
                var selectId = value == null ? "" : value.Id.ToString();
                SelectRadioId = selectId;
                OnPropertyChanged("SelectRadioId");
                this.RaiseEvent(new RoutedEventArgs(OnTabRadioSelectChanged, value));
            }
        }
        public static readonly DependencyProperty SelectRadioIdProperty = DependencyProperty.Register("SelectRadioId", typeof(string), typeof(uTabRadio), new PropertyMetadata(null));
        public string SelectRadioId
        {
            get { return (string)GetValue(SelectRadioIdProperty); }
            set { SetValue(SelectRadioIdProperty, value); }
        }
        public readonly static DependencyProperty TabRadioItemsProperty = DependencyProperty.Register("TabRadioItems", typeof(ObservableCollection<TabRadioItem>), typeof(uTabRadio), new PropertyMetadata(new ObservableCollection<TabRadioItem>()));
        public ObservableCollection<TabRadioItem> TabRadioItems
        {
            get { return (ObservableCollection<TabRadioItem>)GetValue(TabRadioItemsProperty); }
            set { SetValue(TabRadioItemsProperty, value); }
        }
        public uTabRadio()
        {
            InitializeComponent();
            gridTabRadio.DataContext = this;
        }


        #region 实现INotifyPropertyChanged接口
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion


        public void AddRadioButton(TabRadioButton tabRadioButton)
        {
            TabRadioItems.Add(new TabRadioItem(tabRadioButton));
        }
        public void ClearTabRadionButtonItems()
        {
            TabRadioItems.Clear();
            SelectRadioTabItem = null;
        }
        private void IconPathButton_Click(object sender, RoutedEventArgs e)
        {
            //删除按钮点击
            var btn = (Button)sender;
            var id = btn.Tag.ToString();
            var model = GetItemById(id);
            //判断是否可以删除
            bool canDel = true;
            if (HaveAddButton && MinTabCount > 0)
            {
                canDel = TabRadioItems.Count - 1 >= MinTabCount;
            }
            var showMsg = "";
            if (!canDel)
            {
                showMsg = string.Format("该项无法删除，删除后不能满足最小个数");
                MessageBox.Show(showMsg, "提醒", MessageBoxButton.OK);
                return;
            }
            showMsg = string.Format("确认删除\"{0}\"吗？，该过程是不可逆的，请谨慎删除。", model.Content);
            var result = MessageBox.Show(showMsg, "删除提醒", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (result == MessageBoxResult.OK || result == MessageBoxResult.Yes)
            {
                //执行删除
                var currentSelect = SelectRadioTabItem;
                var delIndex = GetItemLocation(model);
                bool changeSelect = false;
                if (currentSelect != null && currentSelect.Id == model.Id)
                {
                    changeSelect = true;
                }
                TabRadioItems.Remove(model);
                if (changeSelect)
                {
                    var count = TabRadioItems.Count;
                    delIndex = delIndex >= count ? delIndex -= 1 : delIndex;
                    SelectRadioTabItem = (count > 0 && count > delIndex) ? TabRadioItems[delIndex] : null;
                }
                this.RaiseEvent(new RoutedEventArgs(OnTabRadioItemDeleted, model));
            }
        }
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var model = GetItemById(textBox.Tag.ToString());
            model.InEdit = false;
        }
        private void RadioButton_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var radioBtn = (RadioButton)sender;
            var model = GetItemById(radioBtn.Tag.ToString());
            model.InEdit = true;
        }
        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectRadioTabItem != null)
            {
                SelectRadioTabItem.InEdit = false;
            }
            string name = GetAddDefaultName();
            var dynamicTabRadio = new TabRadioButton();
            dynamicTabRadio.IsAddBtn = false;
            dynamicTabRadio.CanEdit = true;
            dynamicTabRadio.CanDelete = true;
            dynamicTabRadio.GroupName = GroupName;
            dynamicTabRadio.Content = name;
            var addBtn = new TabRadioItem(dynamicTabRadio);
            TabRadioItems.Add(addBtn);
            this.RaiseEvent(new RoutedEventArgs(OnTabRadioItemAdd, addBtn));
        }
        private string GetAddDefaultName()
        {
            string attrName = "选项";
            if (TabRadioItems.Count < 1)
                return attrName;
            foreach (var item in TabRadioItems)
            {
                var str = item.Content;
                attrName = System.Text.RegularExpressions.Regex.Replace(str, @"\d", "");
                break;
            }
            int num = 0;
            var allNames = new List<string>();
            foreach (var item in TabRadioItems)
            {
                if (item.IsAddBtn)
                    continue;
                int thisNum = 0;
                var str = item.Content;
                allNames.Add(str);
                str = System.Text.RegularExpressions.Regex.Replace(str, @"[^\d.\d]", "");
                if (string.IsNullOrEmpty(str))
                    continue;
                // 如果是数字，则转换为decimal类型
                if (System.Text.RegularExpressions.Regex.IsMatch(str, @"^[+-]?\d*[.]?\d*$"))
                {
                    thisNum = int.Parse(str);
                }
                if (thisNum == int.MaxValue)
                {
                    num = 0;
                    break;
                }
                if (thisNum > num)
                    num = thisNum;
            }
            num += 1;

            while (true)
            {
                string checkName = string.Format("{0}{1}", attrName, num);
                bool nameInHis = allNames.Any(c => c == checkName);
                if (nameInHis)
                {
                    num += 1;
                    continue;
                }
                attrName = checkName;
                break;
            }
            return attrName;
        }

        public TabRadioItem GetItemById(string id)
        {
            if (null == TabRadioItems || TabRadioItems.Count < 1)
                return null;
            foreach (var item in TabRadioItems)
            {
                if (item.Id == id)
                    return item;
            }
            return null;
        }
        public int GetItemLocation(TabRadioItem tabRadioItem)
        {
            for (int i = 0; i < TabRadioItems.Count; i++)
            {
                var ribbonTabItem = TabRadioItems[i];
                if (ribbonTabItem.Id == tabRadioItem.Id)
                    return i;
            }
            return -1;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var radioBtn = (RadioButton)sender;
            var id = radioBtn.Tag.ToString();
            this.SelectRadioTabItem = GetItemById(id);
        }
    }
}
