using System.Collections.Generic;
using System.ComponentModel;

namespace Tianhua.Platform3D.UI.ViewModels
{
    public class PQKInputVM : INotifyPropertyChanged
    {
        private List<string> _existedNames =new List<string>();
        public PQKInputVM()
        {
            //
        }
        public PQKInputVM(List<string> existedNames,string viewName)
        {
            _existedNames = existedNames;
            if(!string.IsNullOrEmpty(viewName))
            {
                for(int i=1;i<=10000;i++)
                {
                    var str = viewName + i;
                    if (!_existedNames.Contains(str))
                    {
                        Name = str;
                        break;
                    }
                }
            }
        }       

        private string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                RaisePropertyChanged("Name");
            }
        }

        private double _depth;
        public double Depth
        {
            get => _depth;
            set
            {
                _depth = value;
                RaisePropertyChanged("Depth");
            }
        }

        public bool IsValid => !string.IsNullOrEmpty(_name) && _depth > 0;
        
        public bool Confirm(out string errorMsg)
        {
            errorMsg = "";
            if (string.IsNullOrEmpty(_name))
            {
                errorMsg = "剖切框名称不能为空！";
                return false;
            }
            if(_existedNames.Contains(_name))
            {
                errorMsg = "剖切框名称已存在！";
                return false;
            }
            if (_depth <= 0)
            {
                errorMsg = "剖切框深度不能小于等于零！";
                return false;
            }
            return true;
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
