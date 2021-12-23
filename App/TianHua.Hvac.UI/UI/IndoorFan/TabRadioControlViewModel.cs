using System.Collections.ObjectModel;
using System.Linq;
using ThControlLibraryWPF;
using ThControlLibraryWPF.ControlUtils;

namespace TianHua.Hvac.UI.UI.IndoorFan
{
    class TabRadioControlViewModel : NotifyPropertyChangedBase
    {
        public string GroupName { get; }
        public string AddItemId { get; private set; }
        public TabRadioControlViewModel()
        {
            GroupName = System.Guid.NewGuid().ToString();
            DynamicRadioButtons = new ObservableCollection<TabRadioItem>();
        }
        private ObservableCollection<TabRadioItem> dynamicRadioButtons { get; set; }
        public ObservableCollection<TabRadioItem> DynamicRadioButtons
        {
            get { return dynamicRadioButtons; }
            set
            {
                dynamicRadioButtons = value;
                this.RaisePropertyChanged();
            }
        }
        public TabRadioItem GetItemById(string id)
        {
            foreach (var item in DynamicRadioButtons)
            {
                if (item.Id == id)
                    return item;
            }
            return null;
        }
        private TabRadioItem _selectRadioTabItem { get; set; }
        public TabRadioItem SelectRadioTabItem
        {
            get { return _selectRadioTabItem; }
            set
            {
                _selectRadioTabItem = value;
                SelectRadioId = _selectRadioTabItem == null ? "" : _selectRadioTabItem.Id;
                this.RaisePropertyChanged();
            }
        }
        private string _selectRadioTabItemId { get; set; }
        public string SelectRadioId
        {
            get { return _selectRadioTabItemId; }
            set
            {
                _selectRadioTabItemId = value;
                this.RaisePropertyChanged();
            }
        }
        private bool haveAddButton { get; set; }
        public bool HaveAddButton
        {
            get { return haveAddButton; }
            set
            {
                haveAddButton = value;
                this.RaisePropertyChanged();
                CheckAndAddButton();
            }
        }
        void CheckAndAddButton()
        {
            var tempList = dynamicRadioButtons.Cast<TabRadioItem>().ToList();
            var tempAddBtn = tempList.Where(c => c.IsAddBtn).FirstOrDefault();
            if ((tempAddBtn != null && haveAddButton) || (null == tempAddBtn && !haveAddButton))
                return;
            if (haveAddButton)
            {
                //需要添加
                var dynamicTabRadio = new TabRadioButton();
                dynamicTabRadio.IsAddBtn = true;
                dynamicTabRadio.CanDelete = false;
                dynamicTabRadio.CanEdit = false;
                dynamicTabRadio.GroupName = GroupName;
                dynamicTabRadio.Content = "+";
                AddItemId = dynamicTabRadio.Id;
                DynamicRadioButtons.Add(new TabRadioItem(dynamicTabRadio));
            }
            else
            {
                //需要删除
                DynamicRadioButtons.Remove(tempAddBtn);
            }
        }
        public void DeleteItemById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

        }
        public int AddButtonLocation()
        {
            for (int i = 0; i < DynamicRadioButtons.Count; i++)
            {
                var ribbonTabItem = DynamicRadioButtons[i];
                if (ribbonTabItem.IsAddBtn)
                    return i;
            }
            return -1;
        }
        public int GetItemLocation(TabRadioItem tabRadioItem)
        {
            for (int i = 0; i < DynamicRadioButtons.Count; i++)
            {
                var ribbonTabItem = DynamicRadioButtons[i];
                if (ribbonTabItem.Id == tabRadioItem.Id)
                    return i;
            }
            return -1;
        }
        public void AddRabbioTabItem(string addName)
        {
            AddTabRadioItem(GetAddTabButton(addName));
        }
        public void AddTabRadioItem(TabRadioButton tabRadioButton)
        {
            var modelClone = ModelCloneUtil.Copy(tabRadioButton);
            modelClone.GroupName = GroupName;
            var addIndex = AddButtonLocation();
            if (addIndex < 0)
                DynamicRadioButtons.Add(new TabRadioItem(modelClone));
            else
                DynamicRadioButtons.Insert(addIndex, new TabRadioItem(modelClone));
        }
        public TabRadioButton GetAddTabButton(string addName)
        {
            var dynamicTabRadio = new TabRadioButton();
            dynamicTabRadio.IsAddBtn = false;
            dynamicTabRadio.CanEdit = true;
            dynamicTabRadio.CanDelete = true;
            dynamicTabRadio.GroupName = GroupName;
            dynamicTabRadio.Content = addName;
            return dynamicTabRadio;
        }
    }
}
