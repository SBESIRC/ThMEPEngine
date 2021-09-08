using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Forms;
using ThCADCore.NTS;
using ThControlLibraryWPF.ControlUtils;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Common;
using ThMEPWSS.Uitl;
using ThMEPWSS.WaterSupplyPipeSystem;

namespace ThMEPWSS.Diagram.ViewModel
{
    public class DrainageViewModel : NotifyPropertyChangedBase
    {
        public Point3d InsertPt;//插入点
        public int StartNum;
        public Point3dCollection SelectedArea;//框定区域
        public List<List<Point3dCollection>> FloorAreaList;//楼层区域
        public List<List<int>> FloorNumList;//楼层列表

        public DrainageViewModel()
        {
            InsertPt = new Point3d();
            StartNum = 1;
        }

       
        public void CreateFloorFraming()
        {
            Common.Utils.CreateFloorFraming();
        }

        public void InitListDatas()
        {
            dynamicRadioButtons?.Clear();
            FloorListDatas = new List<string>();
            Common.Utils.FocusMainWindow();
            using (Active.Document.LockDocument())
            using (var acadDatabase = AcadDatabase.Active())
            {
                SelectedArea = Common.Utils.SelectAreas();
                var storeysRecEngine = new ThStoreysRecognitionEngine();//创建楼板识别引擎
                storeysRecEngine.Recognize(acadDatabase.Database, SelectedArea);
                if (storeysRecEngine.Elements.Count == 0)
                {
                    MessageBox.Show("框选区域没有有效楼层");
                    return;
                }
                FloorListDatas = SystemDiagramUtils.GetStoreyInfoList(acadDatabase,
                    storeysRecEngine.Elements.Select(e => (e as ThStoreys).ObjectId).ToArray());

                var FloorNum = storeysRecEngine.Elements
                    .Where(e => (e as ThStoreys).StoreyType.ToString().Contains("Storey"))
                    .Select(floor => (floor as ThStoreys).StoreyNumber).ToList()
                    .Where(e => !e.Trim().StartsWith("-") && !e.Trim().StartsWith("B")).ToList();

                if (FloorNum.Count == 0)
                {
                    MessageBox.Show("框选区域没有标准楼层");
                    return;
                }
                FloorNumList = ThWCompute.CreateFloorNumList(FloorNum);
                FloorAreaList = ThWCompute.CreateFloorAreaList(storeysRecEngine.Elements);

                var AreaNums = 0;
                var roomBuilder = new ThRoomBuilderEngine();
                var rooms = roomBuilder.BuildFromMS(acadDatabase.Database, SelectedArea);
                if (rooms.Count != 0)
                {
                    var kitchenIndex = new ThCADCoreNTSSpatialIndex(rooms.Select(o => o.Boundary).ToCollection());
                    AreaNums = ThWCompute.CountAreaNums(FloorAreaList, kitchenIndex, ref StartNum);
                }
                else
                {
                    var roomMarkEngine = new ThDB3RoomMarkRecognitionEngine();
                    roomMarkEngine.Recognize(acadDatabase.Database, SelectedArea); //来源于参照
                    var newRooms = roomMarkEngine.Elements.Select(e => (e as ThIfcTextNote).Geometry);
                    var kitchenIndex = new ThCADCoreNTSSpatialIndex(newRooms.ToCollection());
                    AreaNums = ThWCompute.CountAreaNums(FloorAreaList, kitchenIndex, ref StartNum);
                }

                DynamicRadioButtons = new ObservableCollection<DynamicRadioButtonViewModel>();
                for (int i = 0; i < AreaNums; i++)
                {
                    DynamicRadioButtons.Add(new DynamicRadioButtonViewModel { Content = "分组" + Convert.ToString(i + 1), GroupName = "group", IsChecked = true });
                }
                DynamicRadioButtons.Add(new DynamicRadioButtonViewModel { Content = "整层", GroupName = "group", IsChecked = true });
            }
        }

        private List<string> floorListDatas { get; set; }
        public List<string> FloorListDatas
        {
            get { return floorListDatas; }
            set
            {
                floorListDatas = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<DynamicRadioButtonViewModel> dynamicRadioButtons { get; set; }
        public ObservableCollection<DynamicRadioButtonViewModel> DynamicRadioButtons
        {
            get { return dynamicRadioButtons; }
            set
            {
                dynamicRadioButtons = value;
                this.RaisePropertyChanged();
            }
        }

        public DynamicRadioButtonViewModel SelectRadionButton
        {
            get
            {
                if (null == dynamicRadioButtons || dynamicRadioButtons.Count < 1)
                    return null;
                return dynamicRadioButtons.Where(c => c.IsChecked).FirstOrDefault();
            }
        }

        public DrainageSetViewModel SetViewModel { get; set; } = new DrainageSetViewModel();
    }

    public class DynamicRadioButton
    {
        public string Content { get; set; }
        public string GroupName { get; set; }
        public bool IsChecked { get; set; }
    }

    public class DynamicRadioButtonViewModel : DynamicRadioButton
    {
    }
}
