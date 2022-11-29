using ThControlLibraryWPF.ControlUtils;

namespace ThMEPElectrical.ViewModel
{
    public class MultiCheckItem : NotifyPropertyChangedBase
    {
        public MultiCheckItem(string name, string value)
            : this(name, value, false, null)
        { }
        public MultiCheckItem(string name, string value, bool isSelect)
            : this(name, value, isSelect, null)
        { }
        public MultiCheckItem(string name, string value, bool isSelect, object tag)
        {
            this.Name = name;
            this.Value = value;
            this.IsSelect = isSelect;
            this.Tag = tag;
        }
        private string _name { get; set; }
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                this.RaisePropertyChanged();
            }
        }
        private bool _isSelect { get; set; }
        public bool IsSelect
        {
            get { return _isSelect; }
            set
            {
                _isSelect = value;
                this.RaisePropertyChanged();
            }
        }
        private string _value { get; set; }
        public string Value
        {
            get { return _value; }
            set
            {
                _value = value;
                this.RaisePropertyChanged();
            }
        }
        private object _tag { get; set; }
        public object Tag
        {
            get { return _tag; }
            set
            {
                _tag = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
