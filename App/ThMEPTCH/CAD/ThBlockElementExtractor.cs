using System.Linq;
using System.Collections.Generic;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;

namespace ThMEPTCH.CAD
{
    public class ThBlockElementExtractor
    {
        private ThDistributionElementExtractionVisitor _visitor;

        public ThBlockElementExtractor(ThDistributionElementExtractionVisitor visitor)
        {
            _visitor = visitor;
        }

        public virtual void Extract(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => !o.BlockTableRecord.IsNull)
                    .ForEach(o =>
                    {
                        var mcs2wcs = o.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                        var uid2ms = ThMEPDbUniqueIdService.Combine(
                            ThMEPDbUniqueIdService.UniqueId(o.ObjectId),
                            ThMEPDbUniqueIdService.UniqueId(acadDatabase.ModelSpace.ObjectId));
                        var containers = new List<object> { "*MODELSPACE"};
                        _visitor.Results.AddRange(DoExtract(o, mcs2wcs, containers, uid2ms));
                    });
            }
        }

        private List<ThRawIfcDistributionElementData> DoExtract(BlockReference blockReference,
            Matrix3d matrix,List<object> containers, int uid)
        {
            // containers 是blockReference的名称集合
            using (var acadDb = AcadDatabase.Use(blockReference.Database))
            {
                var results = new List<ThRawIfcDistributionElementData>();
                if (blockReference.BlockTableRecord.IsValid)
                {
                    var blockTableRecord = acadDb.Blocks.Element(blockReference.BlockTableRecord);
                    if (_visitor.IsBuildElementBlock(blockTableRecord))
                    {
                        // 提取图元信息
                        var newContainers = containers.Select(o => o).ToList();
                        newContainers.Add(blockTableRecord.Name);
                        var objIds = new ObjectIdCollection();
                        if (blockTableRecord.IsDynamicBlock)
                        {
                            var data = new ThBlockReferenceData(blockReference.ObjectId);
                            objIds = data.VisibleEntities();
                        }
                        else
                        {
                            foreach (var objId in blockTableRecord)
                            {
                                var dbObj = acadDb.Element<Entity>(objId);
                                if (dbObj.Visible)
                                {
                                    objIds.Add(objId);
                                }
                            }
                        }
                        foreach (ObjectId objId in objIds)
                        {
                            var dbObj = acadDb.Element<Entity>(objId);
                            if (dbObj is BlockReference blockObj)
                            {
                                if (blockObj.BlockTableRecord.IsNull)
                                {
                                    continue;
                                }
                                if (_visitor.IsBuildElementBlockReference(blockObj))
                                {   
                                    if (_visitor.CheckLayerValid(blockObj) && _visitor.IsDistributionElement(blockObj))
                                    {
                                        _visitor.DoExtract(results, blockObj, matrix, newContainers, uid);
                                        continue;
                                    }                                    
                                }
                                var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                                var uid2ms = ThMEPDbUniqueIdService.Combine(uid, ThMEPDbUniqueIdService.UniqueId(objId));
                                results.AddRange(DoExtract(blockObj, mcs2wcs, newContainers, uid2ms));
                            }
                            else
                            {
                                _visitor.DoExtract(results, dbObj, matrix, newContainers, uid);
                            }
                        }
                        // 过滤XClip外的图元信息
                        _visitor.DoXClip(results, blockReference, matrix);
                    }
                }
                return results;
            }
        }
    }
}
