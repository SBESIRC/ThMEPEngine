using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using ThMEPElectrical.SystemDiagram.Model;
using ThMEPElectrical.SystemDiagram.Service;

namespace TianHua.Electrical.UI.SystemDiagram.UI
{
    public class ShowAlarmViewModel : NotifyPropertyChangedBase
    {
        private ObservableCollection<UIAlarmModel> alarmList { get; set; }
        public ShowAlarmViewModel()
        {
            alarmList = new ObservableCollection<UIAlarmModel>();
            foreach (var item in FireCompartmentParameter.WarningCache)
            {
                if (!item.Doc.Database.IsNull())
                {
                    var alarmModel = new UIAlarmModel()
                    {
                        Doc = item.Doc,
                        DocName = item.Doc.Name.Split('\\').Last()
                    };
                    item.UiAlarmList.ForEach(o =>
                    {
                        alarmModel.UiAlarmList.Add(new UIAlarmEntityModel() { AlarmMsg = o.Item1, AlarmObjID = o.Item2 });
                    });
                    alarmList.Add(alarmModel);
                }
            }
        }

        public ObservableCollection<UIAlarmModel> AlarmList
        {
            get { return alarmList; }
            set
            {
                alarmList = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public class UIAlarmModel
    {
        public UIAlarmModel()
        {
            UiAlarmList = new ObservableCollection<UIAlarmEntityModel>();
        }
        public Document Doc { get; set; }

        public string DocName { get; set; }
        public ObservableCollection<UIAlarmEntityModel> UiAlarmList { get; set; }
    }

    public class UIAlarmEntityModel
    {
        public string AlarmMsg { get; set; }
        public ObjectId AlarmObjID { get; set; }
    }
}
