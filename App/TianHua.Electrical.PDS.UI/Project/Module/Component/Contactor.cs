using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class Contactor : NotifyPropertyChangedBase
    {
        string _Content;
        public string Content
        {
            get => _Content;
            set
            {
                if (value != _Content)
                {
                    _Content = value;
                    OnPropertyChanged(nameof(Content));
                }
            }
        }

        string _ContactorType;
        public string ContactorType
        {
            get => _ContactorType;
            set
            {
                if (value != _ContactorType)
                {
                    _ContactorType = value;
                    OnPropertyChanged(nameof(ContactorType));
                }
            }
        }

        string _PolesNum;
        public string PolesNum
        {
            get => _PolesNum;
            set
            {
                if (value != _PolesNum)
                {
                    _PolesNum = value;
                    OnPropertyChanged(nameof(PolesNum));
                }
            }
        }

        string _RatedCurrent;
        public string RatedCurrent
        {
            get => _RatedCurrent;
            set
            {
                if (value != _RatedCurrent)
                {
                    _RatedCurrent = value;
                    OnPropertyChanged(nameof(RatedCurrent));
                }
            }
        }
    }
}
