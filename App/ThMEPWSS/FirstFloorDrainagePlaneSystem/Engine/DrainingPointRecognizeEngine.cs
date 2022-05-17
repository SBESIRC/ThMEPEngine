using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPWSS.DrainageSystemAG;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Engine
{
    public class DrainingPointRecognizeEngine
    {
        Dictionary<string, List<string>> _layerNameConfig;
        protected Dictionary<string, int> blockNames;
        List<DrainingEquipmentModel> equipmentBlcoks;

        public DrainingPointRecognizeEngine(Dictionary<string, List<string>> layerNames)
        {
            ReadUIConfig(layerNames);
            InitBlockNames();
            equipmentBlcoks = new List<DrainingEquipmentModel>();

            using (AcadDatabase acdb = AcadDatabase.Active())
            using (acdb.Database.GetDocument().LockDocument())
            {
                var blocks = acdb.ModelSpace.OfType<BlockReference>().ToList();
                foreach (var block in blocks)
                {
                    if (block == null || block.BlockTableRecord == null || !block.BlockTableRecord.IsValid)
                        continue;
                    var elems = new List<DrainingEquipmentModel>();
                    DoExtract(elems, block, Matrix3d.Identity);
                    equipmentBlcoks.AddRange(elems);
                }
                //var s = equipmentBlcoks.Where(x => x.EnumEquipmentType == EnumEquipmentType.singleBasinWashingTable).ToList();
                //foreach (var item in s)
                //{
                //    Circle cl = new Circle(item.BlockPoint, Vector3d.ZAxis, 1000);
                //    acdb.ModelSpace.Add(cl);
                //}
            }
        }

        private void DoExtract(List<DrainingEquipmentModel> elements, BlockReference blockReference, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blockReference.Database))
            {
                if (blockReference is BlockReference blkref && IsDistributionElement(blkref))
                {
                    HandleBlockReference(elements, blkref, matrix);
                    return;
                }
                var mcs2wcs = blockReference.BlockTransform.PreMultiplyBy(matrix);
                if (blockReference.BlockTableRecord.IsValid)
                {
                    var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                    var data = new ThBlockReferenceData(blockReference.ObjectId);
                    var objs = data.VisibleEntities();
                    if (objs.Count == 0)
                    {
                        foreach (var objId in blockTableRecord)
                        {
                            var dbObj = acadDatabase.Element<Entity>(objId);
                            if (dbObj.Visible)
                            {
                                objs.Add(objId);
                            }
                        }
                    }
                    foreach (ObjectId objId in objs)
                    {
                        var dbObj = acadDatabase.Element<Entity>(objId);
                        if (dbObj is BlockReference blockObj)
                        {
                            if (blockObj.BlockTableRecord.IsNull)
                            {
                                continue;
                            }
                            DoExtract(elements, blockObj, mcs2wcs);
                        }
                    }
                }
            }
        }

        private void HandleBlockReference(List<DrainingEquipmentModel> elements, BlockReference blkref, Matrix3d matrix)
        {
            var name = ThMEPXRefService.OriginalFromXref(blkref.GetEffectiveName());
            var type = GetEnumEquipmentType(name);
            var centerPoint = DrainSysAGCommon.GetBlockGeometricCenter(blkref);
            var obb = blkref.ToOBB();
            centerPoint = centerPoint.TransformBy(matrix);
            obb.TransformBy(matrix);
            elements.Add(new DrainingEquipmentModel(type, obb, centerPoint));
        }

        private bool IsDistributionElement(Entity entity)
        {
            if (blockNames == null || blockNames.Count < 1)
                return false;
            if (entity is BlockReference blockObj)
            {
                bool isAdd = false;
                var name = ThMEPXRefService.OriginalFromXref(blockObj.GetEffectiveName());
                foreach (var keyValue in this.blockNames)
                {
                    if (isAdd)
                        break;
                    if (string.IsNullOrEmpty(keyValue.Key))
                        continue;
                    string[] allNames = keyValue.Key.Split(',');
                    isAdd = true;
                    for (int i = 0; i < allNames.Length; i++)
                    {
                        if (!isAdd)
                            break;
                        string checkName = allNames[i];
                        if (keyValue.Value == 1)
                        {
                            //包含
                            isAdd = name.Contains(checkName);
                        }
                        else if (keyValue.Value == 2)
                        {
                            isAdd = name.Equals(checkName);
                        }
                        else if (keyValue.Value == 3)
                        {
                            isAdd = name.StartsWith(checkName);
                        }
                        else if (keyValue.Value == 4)
                        {
                            isAdd = name.EndsWith(checkName);
                        }
                    }
                }
                return isAdd;
            }
            return false;
        }

        private void InitBlockNames()
        {
            blockNames = new Dictionary<string, int>();
            //拖布池
            GetVisitorDictionary(EnumEquipmentType.mopPool, ref blockNames);

            //单盆洗手台
            GetVisitorDictionary(EnumEquipmentType.singleBasinWashingTable, ref blockNames);

            //获取坐便器
            GetVisitorDictionary(EnumEquipmentType.toilet, ref blockNames);

            //获取厨房台盆（洗涤盆）
            GetVisitorDictionary(EnumEquipmentType.kitchenBasin, ref blockNames);

            //获取地漏
            GetVisitorDictionary(EnumEquipmentType.floorDrain, ref blockNames);
        }

        private EnumEquipmentType GetEnumEquipmentType(string name)
        {
            var thisType = EnumEquipmentType.floorDrain;
            var matchDic = _layerNameConfig.FirstOrDefault(x => x.Value.Contains(name)).Key;
            if (matchDic != null)
            {
                switch (matchDic)
                {
                    case "地漏":
                        thisType = EnumEquipmentType.floorDrain;
                        break;
                    case "拖把池":
                        thisType = EnumEquipmentType.mopPool;
                        break;
                    case "单盆洗手台":
                        thisType = EnumEquipmentType.singleBasinWashingTable;
                        break;
                    case "坐便器":
                        thisType = EnumEquipmentType.toilet;
                        break;
                    case "厨房洗涤盆":
                        thisType = EnumEquipmentType.kitchenBasin;
                        break;
                }
            }
            
            return thisType;
        }

        public List<DrainingEquipmentModel> Recognize(Polyline polyline, List<Polyline> wall, ThMEPOriginTransformer originTransformer)
        {
            var resEquipments = new List<DrainingEquipmentModel>();
            var equipments = GetPolylineEquipmentBlocks(polyline);
            foreach (var equip in equipments)
            {
                switch (equip.EnumEquipmentType)
                {
                    case EnumEquipmentType.toilet:                      //坐便器
                        resEquipments.AddRange(CalRectanglePoint(equip, wall, 350));
                        break;
                    case EnumEquipmentType.mopPool:                     //拖把池
                    case EnumEquipmentType.kitchenBasin:                //厨房洗涤盆
                    case EnumEquipmentType.singleBasinWashingTable:     //单盆洗手台
                        resEquipments.AddRange(CalRectanglePoint(equip, wall, 150, false));
                        break;
                    case EnumEquipmentType.floorDrain:                  //地漏
                        resEquipments.AddRange(CalCirclePoint(equip));
                        break;
                    default:
                        break;
                }
            }
            return TransEquipmentModel(resEquipments, originTransformer);
        }

        private List<DrainingEquipmentModel> TransEquipmentModel(List<DrainingEquipmentModel> models, ThMEPOriginTransformer originTransformer)
        {
            foreach (var model in models)
            {
                model.DiranPoint = originTransformer.Transform(model.DiranPoint);
            }
            return models;
        }

        private List<DrainingEquipmentModel> CalRectanglePoint(DrainingEquipmentModel equipModel, List<Polyline> wall, double dis, bool isShortEdge = true)
        {
            List<DrainingEquipmentModel> resModel = new List<DrainingEquipmentModel>();
            var allLines = equipModel.BlockReferenceGeo.GetAllLineByPolyline().OrderByDescending(x => x.Length).ToList();
            var edges = new List<Line>() { allLines[0], allLines[1] };
            if (isShortEdge)
            {
                edges = new List<Line>() { allLines[2], allLines[3] };
            }
            var checkEdge = edges.OrderBy(x => wall.OrderBy(y => y.Distance(x)).First().Distance(x)).First();
            edges.Remove(checkEdge);
            var otherEdge = edges.First();

            var centerPt = new Point3d((checkEdge.EndPoint.X + checkEdge.StartPoint.X) / 2, (checkEdge.EndPoint.Y + checkEdge.StartPoint.Y) / 2, 0);
            var dir = (otherEdge.GetClosestPointTo(centerPt, false) - centerPt).GetNormal();
            var pt = centerPt + dir * dis;
            equipModel.DiranPoint = pt;
            resModel.Add(equipModel);

            return resModel;
        }

        private List<DrainingEquipmentModel> CalCirclePoint(DrainingEquipmentModel equipModel)
        {
            List<DrainingEquipmentModel> resModel = new List<DrainingEquipmentModel>();            
            if (equipModel.BlockReferenceGeo != null)
            {
                var pt1 = equipModel.BlockReferenceGeo.GetPoint3dAt(0);
                var pt2 = equipModel.BlockReferenceGeo.GetPoint3dAt(1);
                var centerPt = new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);
                equipModel.DiranPoint = centerPt;
                resModel.Add(equipModel);
            }

            return resModel;
        }

        private void ReadUIConfig(Dictionary<string, List<string>> layerNames)
        {
            _layerNameConfig = new Dictionary<string, List<string>>();
            if (null != layerNames && layerNames.Count > 0)
            {
                foreach (var keyValue in layerNames)
                {
                    if (string.IsNullOrEmpty(keyValue.Key) || keyValue.Value == null || keyValue.Value.Count < 1)
                        continue;
                    var tempListNames = new List<string>();
                    foreach (var str in keyValue.Value)
                    {
                        if (string.IsNullOrEmpty(str) || tempListNames.Any(c => c.Equals(str)))
                            continue;
                        tempListNames.Add(str);
                    }
                    if (tempListNames.Count < 1)
                        continue;
                    _layerNameConfig.Add(keyValue.Key, tempListNames);
                }
            }
        }

        public List<DrainingEquipmentModel> GetPolylineEquipmentBlocks(Polyline polyline, double disToDist = 30)
        {
            var equipments = new List<DrainingEquipmentModel>();
            foreach (var block in equipmentBlcoks)
            {
                if (polyline.Contains(block.BlockPoint))
                {
                    equipments.Add(block);
                }
            }
            
            return equipments;
        }

        private void GetVisitorDictionary(EnumEquipmentType type, ref Dictionary<string, int> visirorDict)
        {
            foreach (var keyValue in _layerNameConfig)
            {
                var thisType = -1;
                switch (keyValue.Key)
                {
                    case "地漏":
                        thisType = (int)EnumEquipmentType.floorDrain;
                        break;
                    case "拖把池":
                        thisType = (int)EnumEquipmentType.mopPool;
                        break;
                    case "单盆洗手台":
                        thisType = (int)EnumEquipmentType.singleBasinWashingTable;
                        break;
                    case "坐便器":
                        thisType = (int)EnumEquipmentType.toilet;
                        break;
                    case "厨房洗涤盆":
                        thisType = (int)EnumEquipmentType.kitchenBasin;
                        break;
                }
                if (thisType != (int)type)
                    continue;
                foreach (var name in keyValue.Value)
                {
                    if (visirorDict.Any(c => c.Key.ToUpper().Equals(name.ToUpper())))
                        continue;
                    visirorDict.Add(name, 3);
                }
            }
        }
    }
}
