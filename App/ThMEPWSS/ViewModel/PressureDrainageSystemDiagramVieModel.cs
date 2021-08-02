using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ThControlLibraryWPF.ControlUtils;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Common;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Uitl;

namespace ThMEPWSS.Diagram.ViewModel
{
    public class PressureDrainageSystemDiagramVieModel : NotifyPropertyChangedBase
    {
        public Point3dCollection SelectedArea;//框定区域
        public List<List<Point3dCollection>> FloorAreaList;//楼层区域
        public List<List<int>> FloorNumList;//楼层列表
        public PressureDrainageSystemDiagramVieModel()
        {
            UndpdsFloorLineSpace = 5000;//楼层线间距
            HasInfoTablesRoRead = false;
        }
        public void CreateFloorFraming()
        {
            ThMEPWSS.Common.Utils.CreateFloorFraming();
        }
        public void InitListDatas()
        {
            UndpdsFloorListDatas = new List<string>();
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
                    var storeysRecEngine = new ThStoreysRecognitionEngine();//创建楼板识别引擎R
                    storeysRecEngine.Recognize(acadDatabase.Database, SelectedArea);
                    if (storeysRecEngine.Elements.Count == 0)
                    {
                        MessageBox.Show("框选区域没有有效楼层");
                        return;
                    }
                    UndpdsFloorListDatas = SystemDiagramUtils.GetStoreyInfoList(acadDatabase, storeysRecEngine.Elements.Select(e => (e as ThStoreys).ObjectId).ToArray());
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
                    undpdsfloorListDatas.Reverse();
                }
            }
        }
        private List<string> undpdsfloorListDatas { get; set; }//楼层表
        public List<string> UndpdsFloorListDatas
        {
            get { return undpdsfloorListDatas; }
            set
            {
                undpdsfloorListDatas = value;
                this.RaisePropertyChanged();
            }
        }
        private double undpdsfloorLineSpace { get; set; }
        public double UndpdsFloorLineSpace
        {
            get { return undpdsfloorLineSpace; }
            set
            {
                undpdsfloorLineSpace = value;
                this.RaisePropertyChanged("UndpdsFloorLineSpace");
            }
        }
        private bool hasInfoTablesRoRead { get; set; }
        public bool HasInfoTablesRoRead
        {
            get { return hasInfoTablesRoRead; }
            set
            {
                hasInfoTablesRoRead = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
