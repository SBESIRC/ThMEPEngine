using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSLocation : NotifyPropertyChangedBase
    {
        string _RoomType;
        public string RoomType
        {
            get => _RoomType;
            set
            {
                if (value != _RoomType)
                {
                    _RoomType = value;
                    OnPropertyChanged(nameof(RoomType));
                }
            }
        }

        string _FloorNumber;
        public string FloorNumber
        {
            get => _FloorNumber;
            set
            {
                if (value != _FloorNumber)
                {
                    _FloorNumber = value;
                    OnPropertyChanged(nameof(FloorNumber));
                }
            }
        }

        string _ReferenceDWG;
        public string ReferenceDWG
        {
            get => _ReferenceDWG;
            set
            {
                if (value != _ReferenceDWG)
                {
                    _ReferenceDWG = value;
                    OnPropertyChanged(nameof(ReferenceDWG));
                }
            }
        }
    }
}
