namespace ThControlLibraryWPF.ControlUtils
{
    /// <summary>
    /// 当Checkbox有多个时，可以使用这种Item配合ItemControl使用。
    /// </summary>
    public class UListCheckItem
    {
        public string ShowText { get; }
        public int Value { get; }
        public object ItemTag { get;}
        public bool? IsChecked { get; set; }
        public UListCheckItem(string showText,int value) 
        {
            ShowText = showText;
            Value = value;
        }
        public UListCheckItem(string showText, int value,object tag)
        {
            ShowText = showText;
            Value = value;
            ItemTag = tag;
        }
        public UListCheckItem(string showText, int value,object tag,bool isChecked)
        {
            ShowText = showText;
            Value = value;
            ItemTag = tag;
            IsChecked = isChecked;
        }
    }
    public class UListCheckItemViewModel :NotifyPropertyChangedBase
    {
        public UListCheckItem Item { get; }
        public UListCheckItemViewModel(UListCheckItem item) 
        {
            Item = item;
        }
        public string ShowText 
        {
            get { return Item.ShowText; }
        }
        public int Value
        {
            get { return Item.Value; }
        }
        public object ItemTag
        {
            get { return Item.ItemTag; }
        }
        public bool? IsChecked 
        {
            get { return Item.IsChecked; }
            set 
            {
                Item.IsChecked = value;
                this.RaisePropertyChanged();
            }
        }
    }

}
