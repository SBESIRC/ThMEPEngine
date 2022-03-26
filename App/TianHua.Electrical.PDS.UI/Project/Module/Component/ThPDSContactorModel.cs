using System;
using ThCADExtension;
using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    /// <summary>
    /// 接触器
    /// </summary>
    public class ThPDSContactorModel : NotifyPropertyChangedBase
    {
        private readonly Contactor _contactor;

        public ThPDSContactorModel(Contactor contactor)
        {
            _contactor = contactor;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        [DisplayName("内容")]
        public string Content => _contactor.Content;


        [ReadOnly(true)]
        [DisplayName("元器件类型")]
        public string Type => _contactor.ComponentType.GetDescription();


        [DisplayName("型号")]
        public ContactorModel Model
        {
            get => (ContactorModel)Enum.Parse(typeof(ContactorModel), _contactor.ContactorType);
            set
            {
                _contactor.ContactorType = value.ToString();
                OnPropertyChanged(nameof(Model));
            }
        }

        [DisplayName("极数")]
        public string PolesNum
        {
            get => _contactor.PolesNum;
            set
            {
                _contactor.PolesNum = value;
                OnPropertyChanged(nameof(PolesNum));
            }
        }

        [DisplayName("额定电流")]
        public string RatedCurrent
        {
            get => _contactor.RatedCurrent;
            set
            {
                _contactor.RatedCurrent = value;
                OnPropertyChanged(nameof(RatedCurrent));
            }
        }
    }
}
