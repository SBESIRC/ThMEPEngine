using System.Collections.Generic;
using System.ComponentModel;

namespace TianHua.Platform3D.UI.ViewModels
{
    public class TextInputVM : INotifyPropertyChanged
    {
        private List<string> _blackNames =new List<string>();
        public TextInputVM(List<string> blackNames)
        {
            _blackNames = blackNames;
        }

        private string _inputTip = "";
        public string InputTip
        {
            get => _inputTip;
            set
            {
                _inputTip = value;
                RaisePropertyChanged("InputTip");
            }
        }

        private string _inputValue = "";
        public string InputValue
        {
            get => _inputValue;
            set
            {
                _inputValue = value;
                RaisePropertyChanged("InputValue");
            }
        }

        public bool IsExisted
        {
            get
            {
                return CheckIsExisted(InputValue);
            }
        }     
        
        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty(_inputValue) || 
                    string.IsNullOrEmpty(_inputValue.Trim());
            }
        }

        public void Clear()
        {
            _inputValue = "";
        }
        private bool CheckIsExisted(string name)
        {
            return _blackNames.Contains(name);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
