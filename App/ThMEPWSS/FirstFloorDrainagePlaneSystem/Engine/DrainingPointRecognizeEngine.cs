using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
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
        List<EquipmentBlcokVisitorModel> equipmentBlcokVisitors { get; }
        List<EquipmentBlcokVisitorModel> equipmentBlcokVisitorsModelSpace { get; }
        public DrainingPointRecognizeEngine(Dictionary<string, List<string>> layerNames)
        {
            equipmentBlcokVisitors = new List<EquipmentBlcokVisitorModel>();
            equipmentBlcokVisitorsModelSpace = new List<EquipmentBlcokVisitorModel>();
            ReadUIConfig(layerNames);
            InitBlockNames();

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                if (null != this.equipmentBlcokVisitorsModelSpace && this.equipmentBlcokVisitorsModelSpace.Count > 0)
                {
                    ThDistributionElementExtractor thDistributionMS = new ThDistributionElementExtractor();
                    foreach (var item in this.equipmentBlcokVisitorsModelSpace)
                    {
                        thDistributionMS.Accept(item.equipmentDataVisitor);
                    }
                    thDistributionMS.Extract(acdb.Database);

                    var blocks = acdb.ModelSpace.OfType<BlockReference>().ToList();
                    foreach (var block in blocks)
                    {
                        if (block == null || block.BlockTableRecord == null || !block.BlockTableRecord.IsValid)
                            continue;
                        foreach (var visitor in equipmentBlcokVisitorsModelSpace)
                        {
                            var elems = new List<ThRawIfcDistributionElementData>();
                            if (visitor.equipmentDataVisitor.IsBuildElementBlockReference(block))
                            {
                                if (visitor.equipmentDataVisitor.CheckLayerValid(block) && visitor.equipmentDataVisitor.IsDistributionElement(block))
                                {
                                    visitor.equipmentDataVisitor.DoExtract(elems, block, Matrix3d.Identity);
                                }
                            }
                            if (null != elems && elems.Count > 0)
                                visitor.equipmentDataVisitor.Results.AddRange(elems);
                        }
                    }
                }
                if (null != this.equipmentBlcokVisitors && this.equipmentBlcokVisitors.Count > 0)
                {
                    ThDistributionElementExtractor thDistribution = new ThDistributionElementExtractor();
                    foreach (var item in this.equipmentBlcokVisitors)
                    {
                        thDistribution.Accept(item.equipmentDataVisitor);
                    }
                    thDistribution.Extract(acdb.Database);
                }
            }
        }

        public List<DrainingEquipmentModel> Recognize(Polyline polyline, List<Polyline> wall, ThMEPOriginTransformer originTransformer)
        {
            var resEquipments = new List<DrainingEquipmentModel>();
            var equipments = GetPolylineEquipmentBlocks(polyline);
            foreach (var equip in equipments)
            {
                switch (equip.enumEquipmentType)
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
                originTransformer.Transform(model.BlockReference);
                model.DiranPoint = originTransformer.Transform(model.DiranPoint);
            }
            return models;
        }

        private List<DrainingEquipmentModel> CalRectanglePoint(EquipmentBlcokModel equipModel, List<Polyline> wall, double dis, bool isShortEdge = true)
        {
            List<DrainingEquipmentModel> resModel = new List<DrainingEquipmentModel>();
            foreach (var geom in equipModel.blockReferences)
            {
                var boundary = geom.ToOBB();
                var allLines = boundary.GetAllLineByPolyline().OrderByDescending(x=>x.Length).ToList();
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
                DrainingEquipmentModel model = new DrainingEquipmentModel(pt, equipModel.enumEquipmentType, geom);
                resModel.Add(model);
            }

            return resModel;
        }

        private List<DrainingEquipmentModel> CalCirclePoint(EquipmentBlcokModel equipModel)
        {
            List<DrainingEquipmentModel> resModel = new List<DrainingEquipmentModel>();
            foreach (var geom in equipModel.blockReferences)
            {
                var ent = EntityService.GetBasicEntityDic(new List<Entity>() { geom as Entity }).OfType<Circle>().FirstOrDefault();
                if (ent != null)
                {
                    DrainingEquipmentModel model = new DrainingEquipmentModel(ent.Center, equipModel.enumEquipmentType, geom);
                    resModel.Add(model);
                }
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

        public List<EquipmentBlcokModel> GetPolylineEquipmentBlocks(Polyline polyline, double disToDist = 30)
        {
            var equipments = new List<EquipmentBlcokModel>();
            var tempModel = GetModelSpaceEquipmentBlocks(polyline);
            if (null != tempModel && tempModel.Count > 0)
                equipments.AddRange(tempModel);
            var tempExts = GetExtractEquipmentBlocks(polyline);
            if (null != tempExts && tempExts.Count > 0)
                equipments.AddRange(tempExts);

            return Distinct(equipments, disToDist);
        }

        public List<EquipmentBlcokModel> GetModelSpaceEquipmentBlocks(Polyline polyline)
        {
            var equipments = new List<EquipmentBlcokModel>();
            if (null == polyline || this.equipmentBlcokVisitors == null || this.equipmentBlcokVisitors.Count < 1)
                return equipments;
            foreach (var item in this.equipmentBlcokVisitors)
            {
                if (item.equipmentDataVisitor == null || item.equipmentDataVisitor.Results == null || item.equipmentDataVisitor.Results.Count < 1)
                    continue;
                var blcokModel = new EquipmentBlcokModel(item.enumEquipmentType);
                foreach (var obj in item.equipmentDataVisitor.Results)
                {
                    if (null == obj)
                        continue;
                    BlockReference block = obj.Geometry as BlockReference;

                    if (null == block)
                        continue;
                    if (!block.Bounds.HasValue)
                        continue;
                    var centerPoint = DrainSysAGCommon.GetBlockGeometricCenter(block);
                    if (polyline.Contains(centerPoint))
                    {
                        blcokModel.blockReferences.Add(block);
                    }
                }
                if (blcokModel.blockReferences.Count > 0)
                    equipments.Add(blcokModel);
            }
            return equipments;
        }

        public List<EquipmentBlcokModel> GetExtractEquipmentBlocks(Polyline polyline)
        {
            var equipments = new List<EquipmentBlcokModel>();
            if (null == polyline || this.equipmentBlcokVisitors == null || this.equipmentBlcokVisitors.Count < 1)
                return equipments;
            foreach (var item in this.equipmentBlcokVisitorsModelSpace)
            {
                if (item.equipmentDataVisitor == null || item.equipmentDataVisitor.Results == null || item.equipmentDataVisitor.Results.Count < 1)
                    continue;
                var blcokModel = new EquipmentBlcokModel(item.enumEquipmentType);
                foreach (var obj in item.equipmentDataVisitor.Results)
                {
                    if (null == obj)
                        continue;
                    BlockReference block = obj.Geometry as BlockReference;

                    if (null == block)
                        continue;
                    var centerPoint = block.GeometricExtents.CenterPoint();
                    var pointP = new Point3d(centerPoint.X, centerPoint.Y, 0);
                    if (polyline.Contains(pointP))
                    {
                        blcokModel.blockReferences.Add(block);
                    }
                }
                if (blcokModel.blockReferences.Count > 0)
                    equipments.Add(blcokModel);
            }
            return equipments;
        }

        private List<EquipmentBlcokModel> Distinct(List<EquipmentBlcokModel> targetBlocks, double disToDist)
        {
            //去重，在一定范围内不能有同一类的数据
            var retBlocks = new List<EquipmentBlcokModel>();
            foreach (var item in targetBlocks)
            {
                if (null == item || item.blockReferences == null || item.blockReferences.Count < 1)
                    continue;
                var tempList = new List<BlockReference>();
                foreach (var block in item.blockReferences)
                {
                    var blockPt2d = new Point3d(block.Position.X, block.Position.Y, 0);
                    bool isAdd = true;
                    foreach (var checkBlock in tempList)
                    {
                        if (!isAdd)
                            break;
                        var checkPoint2d = new Point3d(checkBlock.Position.X, checkBlock.Position.Y, 0);
                        isAdd = checkPoint2d.DistanceTo(blockPt2d) > disToDist;
                    }
                    if (isAdd)
                        tempList.Add(block);
                }
                bool addType = true;
                foreach (var retItem in retBlocks)
                {
                    if (!addType || retItem.enumEquipmentType != item.enumEquipmentType)
                        continue;
                    addType = false;
                    foreach (var block in tempList)
                    {
                        bool isAdd = true;
                        var blockPt2d = new Point3d(block.Position.X, block.Position.Y, 0);
                        foreach (var checkBlock in retItem.blockReferences)
                        {
                            if (!isAdd)
                                break;
                            var checkPoint2d = new Point3d(checkBlock.Position.X, checkBlock.Position.Y, 0);
                            isAdd = checkPoint2d.DistanceTo(blockPt2d) > disToDist;
                        }
                        if (isAdd)
                            retItem.blockReferences.Add(block);
                    }
                    break;
                }
                if (addType && tempList.Count > 0)
                    retBlocks.Add(new EquipmentBlcokModel(item.enumEquipmentType, tempList));
            }
            return retBlocks;
        }

        private void InitBlockNames()
        {
            //拖布池
            Dictionary<string, int> mopPoolNames = new Dictionary<string, int>();
            GetVisitorDictionary(EnumEquipmentType.mopPool, ref mopPoolNames);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.mopPool, mopPoolNames));
            this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.mopPool, mopPoolNames));

            //单盆洗手台
            Dictionary<string, int> singleBasinWashingNames = new Dictionary<string, int>();
            GetVisitorDictionary(EnumEquipmentType.singleBasinWashingTable, ref singleBasinWashingNames);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.singleBasinWashingTable, singleBasinWashingNames));
            this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.singleBasinWashingTable, singleBasinWashingNames));

            //获取坐便器
            Dictionary<string, int> toiletNames = new Dictionary<string, int>();
            GetVisitorDictionary(EnumEquipmentType.toilet, ref toiletNames);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.toilet, toiletNames));
            this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.toilet, toiletNames));

            //获取厨房台盆（洗涤盆）
            Dictionary<string, int> kitchenSinkNames = new Dictionary<string, int>();
            GetVisitorDictionary(EnumEquipmentType.kitchenBasin, ref kitchenSinkNames);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.kitchenBasin, kitchenSinkNames));
            this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.kitchenBasin, kitchenSinkNames));

            //获取地漏
            Dictionary<string, int> floorDrainNames = new Dictionary<string, int>();
            GetVisitorDictionary(EnumEquipmentType.floorDrain, ref floorDrainNames);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.floorDrain, floorDrainNames));
            this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.floorDrain, floorDrainNames));
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
                    visirorDict.Add(name, 4);
                }
            }
        }
    }
}
