﻿using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using GeometryExtensions;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ThCADExtension;
using ThControlLibraryWPF.ControlUtils;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Common;
using ThMEPWSS.Uitl;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.WaterSupplyPipeSystem;

namespace ThMEPWSS.UndergroundSpraySystem.ViewModel
{
    public class SprayVM : NotifyPropertyChangedBase
    {
        public Point3dCollection SelectedArea;//框定区域
        public Dictionary<string, Polyline> FloorRect;//楼层区域
        public Dictionary<string, Point3d> FloorPt;//楼层标准点

        public SprayVM()
        {
            FloorRect = new Dictionary<string, Polyline>();
            FloorPt = new Dictionary<string, Point3d>();
        }
        public void CreateFloorFraming()
        {
            Common.Utils.CreateFloorFraming();
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
        public SprayVMSet SetViewModel { get; set; } = new SprayVMSet();
        public void InitListDatas()
        {
            FloorListDatas = new List<string>();
            Common.Utils.FocusMainWindow();
            using (Active.Document.LockDocument())
            using (var acadDatabase = AcadDatabase.Active())
            {
                try
                {
                    SelectedArea = Common.Utils.SelectAreas();
                    var storeysRecEngine = new ThStoreysRecognitionEngine();//创建楼板识别引擎
                    storeysRecEngine.Recognize(acadDatabase.Database, SelectedArea);
                    if (storeysRecEngine.Elements.Count == 0)
                    {
                        MessageBox.Show("\n 框选区域没有有效楼层");
                        return;
                    }
                    FloorListDatas = storeysRecEngine.Elements
                        .Where(e => (e as ThStoreys).StoreyType.ToString().Contains("Storey"))
                        .Select(floor => (floor as ThStoreys).StoreyNumber).ToList()
                        .Where(e => e.Trim().StartsWith("B")).ToList();

                    storeysRecEngine.Elements
                        .ForEach(e => FloorRect.Add((e as ThStoreys).StoreyNumber, ThWCompute.CreateFloorAreaList(e)));
                    var numDic = new Dictionary<string, int>();
                    for(int i = 0; i < 10; i++)
                    {
                        numDic.Add("B" + Convert.ToString(i), -i);
                        numDic.Add("-" + Convert.ToString(i), -i);
                    }
                    
                    FloorRect = FloorRect.OrderByDescending(e => e.Key).ToDictionary(e=>e.Key, e=>e.Value);
                    
                    storeysRecEngine.Elements
                        .ForEach(e => FloorPt.Add((e as ThStoreys).StoreyNumber, ThWCompute.CreateFloorPt(e)));


                    if (FloorListDatas.Count == 0)
                    {
                        MessageBox.Show("\n 框选区域没有标准楼层");
                        return;
                    }
                }
                catch
                {

                }
            }
        }
    }

    public static class SprayViewModel
    {
        public static void InsertNodeMark()
        {
            Common.Utils.FocusMainWindow();
            using (Active.Document.LockDocument())
            {
                using (var acadDatabase = AcadDatabase.Active())
                {
                    using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
                    {
                        acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault("喷淋总管标记"), true);
                    }
                }

                while (true)
                {
                    var opt = new PromptPointOptions("\n请指定喷淋总管标记插入点");
                    var pt = Active.Editor.GetPoint(opt);
                    if (pt.Status != PromptStatus.OK)
                    {
                        break;
                    }
                    using (var acadDatabase = AcadDatabase.Active())  //要插入图纸的空间
                    {
                        acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-NOTE", "喷淋总管标记",
                                    pt.Value.Point3dZ0().TransformBy(Active.Editor.UCS2WCS()), new Scale3d(1, 1, 1), 0);
                    }
                }
            }
            
            
        }
    }
}

