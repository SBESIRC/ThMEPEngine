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
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Uitl;
using ThMEPWSS.WaterSupplyPipeSystem;

namespace ThMEPWSS.Diagram.ViewModel
{
    public class WaterSupplyVM : NotifyPropertyChangedBase
    {
        public Point3d InsertPt;//插入点
        public int StartNum;
        public Point3dCollection SelectedArea;//框定区域
        public List<List<Point3dCollection>> FloorAreaList;//楼层区域
        public List<List<int>> FloorNumList;//楼层列表
        public int MaxFloor=1;//最大楼层号

        public WaterSupplyVM()
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
            SelectedArea = Common.Utils.SelectAreas();
            if (SelectedArea.Count != 0)
            {
                CadCache.SetCache(CadCache.CurrentFile, "SelectedRange", SelectedArea);
                InitListDatasByArea(SelectedArea);
                CadCache.UpdateByRange(SelectedArea);
            }
        }

        public void InitListDatasByArea(Point3dCollection selectedArea, bool showMessageBox = true)
        {
            using (Active.Document.LockDocument())
            using (var acadDatabase = AcadDatabase.Active())
            {
                var storeysRecEngine = new ThStoreysRecognitionEngine();//创建楼板识别引擎
                storeysRecEngine.Recognize(acadDatabase.Database, selectedArea);
                if (storeysRecEngine.Elements.Count == 0)
                {
                    if (showMessageBox)
                        MessageBox.Show("框选区域没有有效楼层");
                    return;
                }
                var objIds = storeysRecEngine.Elements.Select(e => (e as ThStoreys).ObjectId).ToArray();
                FloorListDatas = SystemDiagramUtils.GetStoreyInfoList(acadDatabase, objIds);

                var FloorNum = storeysRecEngine.Elements
                    .Where(e => (e as ThStoreys).StoreyType.ToString().Contains("Storey"))
                    .Select(floor => (floor as ThStoreys).StoreyNumber).ToList()
                    .Where(e => !e.Trim().StartsWith("-") && !e.Trim().StartsWith("B")).ToList();

                if (FloorNum.Count == 0)
                {
                    if (showMessageBox)
                        MessageBox.Show("框选区域没有标准楼层");
                    return;
                }
                int maxFloor = 1;
                FloorNumList = ThWCompute.CreateFloorNumList(FloorNum,ref maxFloor);
                MaxFloor = maxFloor;

                var areaSegs = acadDatabase.ModelSpace.OfType<Polyline>().Where(o => o.Layer == "AI-单元分割线").ToList();
                if(areaSegs.Count()>0)
                {
                    FloorAreaList = ThWCompute.CreateFloorAreaList(storeysRecEngine.Elements, areaSegs);
                    int maxArea = 0;
                    foreach(var floor in FloorAreaList)
                    {
                        if(floor.Count>maxArea)
                        {
                            maxArea = floor.Count;
                        }
                    }
                    for(int i =0; i < FloorAreaList.Count;i++)
                    {
                        var cnt = FloorAreaList[i].Count;
                        for(int j =0; j < maxArea-cnt;j++)
                        {
                            FloorAreaList[i].Add(new Point3dCollection());
                        }
                    }
                }
                else
                {
                    FloorAreaList = ThWCompute.CreateFloorAreaList(storeysRecEngine.Elements);
                }

                var AreaNums = 0;
                var roomBuilder = new ThRoomBuilderEngine();
                var rooms = roomBuilder.BuildFromMS(acadDatabase.Database, selectedArea);
                if (rooms.Count != 0)
                {
                    var kitchenIndex = new ThCADCoreNTSSpatialIndex(rooms.Select(o => o.Boundary).ToCollection());
                    AreaNums = ThWCompute.CountAreaNums(FloorAreaList, kitchenIndex, ref StartNum);
                }
                else
                {
                    var roomMarkEngine = new ThDB3RoomMarkRecognitionEngine();
                    roomMarkEngine.Recognize(acadDatabase.Database, selectedArea); //来源于参照
                    var newRooms = roomMarkEngine.Elements.Select(e => (e as ThIfcTextNote).Geometry);
                    var kitchenIndex = new ThCADCoreNTSSpatialIndex(newRooms.ToCollection());
                    AreaNums = ThWCompute.CountAreaNums(FloorAreaList, kitchenIndex, ref StartNum);
                }
                foreach(var fAreaLs in FloorAreaList)
                {
                    ;
                    for(int i = fAreaLs.Count-1;i> AreaNums;i--)
                    {
                        fAreaLs.RemoveAt(i);
                    }
                }
                DynamicRadioButtons = new ObservableCollection<DynamicRadioButtonViewModel>();
                for (int i = 0; i < AreaNums; i++)
                {
                    DynamicRadioButtons.Add(new DynamicRadioButtonViewModel { Content = "单元" + Convert.ToString(i + 1), GroupName = "group", IsChecked = true });
                }
                DynamicRadioButtons.Add(new DynamicRadioButtonViewModel { Content = "整层", GroupName = "group", IsChecked = true });

                SetViewModel = new WaterSupplySetVM(maxFloor);
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

        public WaterSupplySetVM SetViewModel { get; set; } 
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
