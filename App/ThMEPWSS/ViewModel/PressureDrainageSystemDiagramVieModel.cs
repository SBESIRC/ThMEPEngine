using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ThControlLibraryWPF.ControlUtils;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Common;
using ThMEPWSS.Command;
using ThMEPWSS.Uitl;
using ThMEPWSS.WaterSupplyPipeSystem;
using static ThMEPWSS.PressureDrainageSystem.Utils.PressureDrainageUtils;

namespace ThMEPWSS.Diagram.ViewModel
{
    public class PressureDrainageSystemDiagramVieModel : NotifyPropertyChangedBase
    {
        public PressureDrainageSystemDiagramVieModel()
        {
            UndpdsFloorLineSpace = 5000;//楼层线间距
            HasInfoTablesRoRead = false;
        }
        public Point3dCollection SelectedArea;//框定区域
        public Extents3d InfoRegion;//款选提资范围
        public List<List<Point3dCollection>> FloorAreaList;//楼层区域
        public List<List<int>> FloorNumList;//楼层列表
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
        private double undpdsfloorLineSpace { get; set; }//楼层线间距
        public double UndpdsFloorLineSpace
        {
            get { return undpdsfloorLineSpace; }
            set
            {
                undpdsfloorLineSpace = value;
                this.RaisePropertyChanged("UndpdsFloorLineSpace");
            }
        }
        private bool hasInfoTablesRoRead { get; set; }//读取到提资表
        public bool HasInfoTablesRoRead
        {
            get { return hasInfoTablesRoRead; }
            set
            {
                hasInfoTablesRoRead = value;
                this.RaisePropertyChanged();
            }
        }
        public List<Point3dCollection> WellsAreas;
        public List<string> WellBlockKeyNames = new List<string>();

        /// <summary>
        /// 楼层框定
        /// </summary>
        public void CreateFloorFraming()
        {
            ThMEPWSS.Common.Utils.CreateFloorFraming();
        }
        /// <summary>
        /// 读取楼层信息
        /// </summary>
        public void InitListDatas()
        {
            UndpdsFloorListDatas = new List<string>();
            using (Active.Document.LockDocument())
            using (var acadDatabase = AcadDatabase.Active())
            {
                SelectedArea = Common.Utils.SelectAreas();
                if (SelectedArea.Count == 0)
                {
                    return;
                }
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
                FloorAreaList = CreateFloorAreaList(storeysRecEngine.Elements);
                undpdsfloorListDatas.Reverse();
                return;
            }
        }
        /// <summary>
        /// 框选提资表
        /// </summary>
        public Extents3d SelectRegionForInfoTable()
        {
            using (Active.Document.LockDocument())
            using (var acadDatabase = AcadDatabase.Active())
            {
                var InfoArea = ThMEPWSS.Common.Utils.SelectAreas();
                if (InfoArea.Count <= 3)
                {
                    return new Extents3d();
                }
                return new Extents3d(InfoArea[0], InfoArea[2]);
            }
        }
        /// <summary>
        /// 生成系统图前处理
        /// </summary>
        public void PreGenerateDiagram(ThUNDPDrainageSystemDiagramCmd cmd)
        {
            cmd.Execute();
        }
    }
}
