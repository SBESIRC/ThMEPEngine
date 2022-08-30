using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using ThCADExtension;

namespace TianHua.Platform3D.UI.ViewModels
{
    public class StoreyNameInputVM : INotifyPropertyChanged
    {
        public ObservableCollection<string> Prefixes { get; set; }
        private int _storeyCount;//用户选择的楼层数量
        public StoreyNameInputVM(string oldStoreyName,int storeyCount=1)
        {
            _storeyCount = storeyCount;
            if(storeyCount<=1)
            {
                _isChkAscendingVisible = Visibility.Hidden;
            }
            else
            {
                _isChkAscendingVisible = Visibility.Visible;
            }
            _inputValue = Parse(oldStoreyName);
            Prefixes = new ObservableCollection<string>() { "","B","R"};
            if(oldStoreyName.StartsWith("R") || oldStoreyName.StartsWith("r"))
            {
                _prefix = "R";
            }
            else if(oldStoreyName.StartsWith("B") || oldStoreyName.StartsWith("b"))
            {
                _prefix = "B";
            }
            else
            {
                _prefix = "";
            }
        }

        public string CheckInputValue()
        {
            if (_inputValue <= 0)
            {   
                return "输入的楼层编号为正整数，请重新输入！";
            }
            if(!_isAscending)
            {
                if (_inputValue < _storeyCount)
                {
                    return "输入的楼层编号不能小于所选的楼层数！";
                }
            }
            return "";
        }

        private int Parse(string storeyName)
        {
            var newStoreyName = storeyName.Trim();
            if (newStoreyName.StartsWith("R") || newStoreyName.StartsWith("B"))
            {
                newStoreyName = newStoreyName.Substring(1);
            }
            if (newStoreyName.EndsWith("F"))
            {
                newStoreyName = newStoreyName.Substring(0, newStoreyName.Length-1);
            }
            if(ThStringTools.IsInteger(newStoreyName))
            {
                return int.Parse(newStoreyName);
            }
            else
            {
                return 0;
            }
        }

        private Visibility _isChkAscendingVisible = Visibility.Hidden;
        public Visibility IsChkAscendingVisible
        {
            get => _isChkAscendingVisible;
            set
            {
                _isChkAscendingVisible = value;
                RaisePropertyChanged("IsChkAscendingVisible");
            }
        }

        private bool _isAscending =true;
        public bool IsAscending
        {
            get => _isAscending;
            set
            {
                _isAscending = value;
                RaisePropertyChanged("IsAscending");
            }
        }

        private string _prefix = "";
        public string Prefix
        {
            get => _prefix;
            set
            {
                _prefix = value;
                RaisePropertyChanged("Prefix");
            }
        }

        private int _inputValue = 1;
        public int InputValue
        {
            get => _inputValue;
            set
            {
                _inputValue = value;
                RaisePropertyChanged("InputValue");
            }
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
