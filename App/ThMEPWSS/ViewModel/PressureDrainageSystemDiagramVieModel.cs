﻿using AcHelper;
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
                SelectedArea = Common.Utils.SelectAreas();
                if (SelectedArea.Count >= 4)
                {
                    var rect = new Rectangle3d(SelectedArea[0], SelectedArea[1], SelectedArea[2], SelectedArea[3]);
                }
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
                return;
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
