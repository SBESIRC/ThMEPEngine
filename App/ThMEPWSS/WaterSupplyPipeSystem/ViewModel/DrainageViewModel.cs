using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
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

        public void InitListDatas2()
        {
            dynamicRadioButtons?.Clear();
            FloorListDatas = new List<string>();
            Common.Utils.FocusMainWindow();
            using (Active.Document.LockDocument())
            using (var acadDatabase = AcadDatabase.Active())
            {
                SelectedArea = Common.Utils.SelectAreas();
                //var frames = FramedReadUtil.ReadAllFloorFramed();//赵工的提取方法
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

        public void InitListDatas()//同时支持点选和框选
        {
            dynamicRadioButtons?.Clear();
            FloorListDatas = new List<string>();
            ThMEPWSS.Common.Utils.FocusMainWindow();
            using (Active.Document.LockDocument())
            using (var acadDatabase = AcadDatabase.Active())
            {
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择楼层框线",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filter = OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status == PromptStatus.OK)//框选择成功
                {
                    var selectedIds = result.Value.GetObjectIds();
                    double topLeftX = 0;
                    double topLeftY = 0;
                    double lowRightX = 0;
                    double lowRightY = 0;
                    var firstFlag = true;
                    foreach (var sID in selectedIds)
                    {
                        var br = acadDatabase.Element<BlockReference>(sID);
                        if (br.GetEffectiveName() == "楼层框定")
                        {
                            if (firstFlag)
                            {
                                topLeftX = GeoAlgorithm.GetBoundaryRect(sID.GetEntity()).LeftTop.X;//最左边的X
                                topLeftY = GeoAlgorithm.GetBoundaryRect(sID.GetEntity()).LeftTop.Y;//最左边的Y
                                lowRightX = GeoAlgorithm.GetBoundaryRect(sID.GetEntity()).RightButtom.X;//最右边的X
                                lowRightY = GeoAlgorithm.GetBoundaryRect(sID.GetEntity()).RightButtom.Y;//最右边的Y
                                firstFlag = false;
                            }
                            else
                            {
                                var bdRect = GeoAlgorithm.GetBoundaryRect(sID.GetEntity());
                                if (bdRect.LeftTop.X < topLeftX)
                                {
                                    topLeftX = bdRect.LeftTop.X;
                                }
                                if (bdRect.LeftTop.Y > topLeftY)
                                {
                                    topLeftY = bdRect.LeftTop.Y;
                                }
                                if (bdRect.RightButtom.X > lowRightX)
                                {
                                    lowRightX = bdRect.RightButtom.X;
                                }
                                if (bdRect.RightButtom.Y < lowRightY)
                                {
                                    lowRightY = bdRect.RightButtom.Y;
                                }
                            }
                        }
                    }

                    SelectedArea = ThWCompute.CreatePolyLine(new Point3d(topLeftX - 100, topLeftY + 100, 0), new Point3d(lowRightX + 100, lowRightY - 100, 0));
                    var rect = new Rectangle3d(SelectedArea[0], SelectedArea[1], SelectedArea[2], SelectedArea[3]);
                    var storeysRecEngine = new ThStoreysRecognitionEngine();//创建楼板识别引擎
                    storeysRecEngine.Recognize(acadDatabase.Database, SelectedArea);
                    if (storeysRecEngine.Elements.Count == 0)
                    {
                        MessageBox.Show("框选区域没有有效楼层");
                        return;
                    }
                    FloorListDatas = SystemDiagramUtils.GetStoreyInfoList(acadDatabase, storeysRecEngine.Elements.Select(e => (e as ThStoreys).ObjectId).ToArray());

                    var FloorNum = storeysRecEngine.Elements
                        .Where(e => (e as ThStoreys).StoreyType.ToString().Contains("Storey"))
                        .Select(floor => (floor as ThStoreys).StoreyNumber).ToList();

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
                        //var roomMarkEngine = new ThRoomMarkRecognitionEngine();
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
                }
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
        //public DrainageSetViewModel SetViewModel { get; set; }
    }
}
