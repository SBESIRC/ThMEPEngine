using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Common;
using ThMEPWSS.WaterSupplyPipeSystem;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class StoreyRect
    {
        public bool HasStoreyRect{ get; set; }//
        public Point3dCollection SelectedArea;//框定区域
        public Dictionary<string, Polyline> FloorRect;//楼层区域
        public Dictionary<string, Point3d> FloorPt;//楼层标准点

        public StoreyRect()
        {
            FloorRect = new Dictionary<string, Polyline>();
            FloorPt = new Dictionary<string, Point3d>();
        }

        public void Extract(Point3dCollection SelectedArea)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var storeysRecEngine = new ThStoreysRecognitionEngine();//创建楼板识别引擎
                storeysRecEngine.Recognize(acadDatabase.Database, SelectedArea);
                if (storeysRecEngine.Elements.Count == 0)
                {
                    HasStoreyRect =  false;
                    return;
                }
                var floorListDatas = storeysRecEngine.Elements
                    .Where(e => (e as ThStoreys).StoreyType.ToString().Contains("Storey"))
                    .Select(floor => (floor as ThStoreys).StoreyNumber).ToList()
                    .Where(e => e.Trim().StartsWith("B")).ToList();

                storeysRecEngine.Elements.ForEach(e => FloorRect.Add((e as ThStoreys).StoreyNumber, ThWCompute.CreateFloorAreaList(e)));
                var numDic = new Dictionary<string, int>();
                for (int i = 0; i < 10; i++)
                {
                    numDic.Add("B" + Convert.ToString(i), -i);
                    numDic.Add("-" + Convert.ToString(i), -i);
                }
                numDic.Add("B1M", -0);//这儿应该是-0.5，考虑到地下不会出现0层，故采用 0
                FloorRect = FloorRect.OrderByDescending(e => e.Key).ToDictionary(e => e.Key, e => e.Value);

                storeysRecEngine.Elements.ForEach(e => FloorPt.Add((e as ThStoreys).StoreyNumber, ThWCompute.CreateFloorPt(e)));

                if (floorListDatas.Count == 0)
                {
                    HasStoreyRect = false;
                    return;
                }
                HasStoreyRect = true;
            }
        }
    }
}
