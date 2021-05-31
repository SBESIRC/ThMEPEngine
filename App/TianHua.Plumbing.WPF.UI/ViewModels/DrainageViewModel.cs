using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThCADCore.NTS;
using ThControlLibraryWPF.ControlUtils;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Common;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Diagram.ViewModel
{
    public class DrainageViewModel: NotifyPropertyChangedBase
    {
        public Point3dCollection SelectedArea;//框定区域
        public List<List<Point3dCollection>> FloorAreaList;//楼层区域
        public List<List<int>> FloorNumList;//楼层列表
        public DrainageViewModel() 
        {
            //测试数据
            FloorListDatas = new List<string>();
            using (var acadDatabase = AcadDatabase.Active())
            {
                {
                    var ptLeftRes = Active.Editor.GetPoint("\n请您框选范围，先选择左上角点");
                    Point3d leftDownPt = Point3d.Origin;
                    if (ptLeftRes.Status == PromptStatus.OK)
                    {
                        leftDownPt = ptLeftRes.Value;
                    }
                    
                    var ptRightRes = Active.Editor.GetCorner("\n再选择右下角点", leftDownPt);
                    if (ptRightRes.Status == PromptStatus.OK)//框选择成功
                    {
                        SelectedArea = ThWCompute.CreatePolyLine(ptLeftRes.Value, ptRightRes.Value);//创建分割区域
                        var storeysRecEngine = new ThStoreysRecognitionEngine();//创建楼板识别引擎
                        storeysRecEngine.Recognize(acadDatabase.Database, SelectedArea);

                        //楼层号识别
                        var FloorNum = storeysRecEngine.Elements.Select(floor => (floor as ThStoreys).StoreyNumber).ToList();
                        foreach (var f in FloorNum)
                        {
                            FloorListDatas.Add(f + "F");
                        }
                        FloorNumList = ThWCompute.CreateFloorNumList(FloorNum);

                        FloorAreaList = ThWCompute.CreateFloorAreaList(storeysRecEngine.Elements);
                        
                        var roomBuilder = new ThRoomBuilderEngine()
                        {
                            RoomBoundaryLayerFilter = new List<string> { "AI-空间框线" },
                            RoomMarkLayerFilter = new List<string> { "AI-空间名称"},
                        };
                        var rooms = roomBuilder.BuildFromMS(acadDatabase.Database, SelectedArea);

                        var kitchenIndex = new ThCADCoreNTSSpatialIndex(rooms.Select(o => o.Boundary).ToCollection());
                        //统计有效分区数
                        var AreaNums = ThWCompute.CountAreaNums(FloorAreaList, kitchenIndex);

                        DynamicRadioButtons = new ObservableCollection<DynamicRadioButtonViewModel>();
                        for (int i = 0; i < AreaNums; i++)
                        {
                            DynamicRadioButtons.Add(new DynamicRadioButtonViewModel { Content = "分组" + Convert.ToString(i+1), GroupName = "group", IsChecked = true, SetViewModel = new DrainageSetViewModel() });
                        }
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
    }
    public class DynamicRadioButton 
    {
        public string Content { get; set; }
        public string GroupName { get; set; }
        public bool IsChecked { get; set; }
    }
    public class DynamicRadioButtonViewModel: DynamicRadioButton
    {
        public DrainageSetViewModel SetViewModel { get; set; }
    }
}
