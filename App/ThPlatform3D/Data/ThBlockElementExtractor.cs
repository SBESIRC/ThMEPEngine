using AcHelper;
using Linq2Acad;
using System.Linq;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThCADExtension;

namespace ThPlatform3D.Data
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
                        var containers = new List<string> { "*MODELSPACE"};
                        _visitor.Results.AddRange(DoExtract(o, mcs2wcs, containers));
                    });
            }
        }

        private List<ThRawIfcDistributionElementData> DoExtract(BlockReference blockReference,Matrix3d matrix,List<string> containers)
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
                                    if (_visitor is ISetContainer iSetContainer)
                                    {
                                        iSetContainer.SetContainers(newContainers);
                                    }
                                    if (_visitor.CheckLayerValid(blockObj) && _visitor.IsDistributionElement(blockObj))
                                    {
                                        _visitor.DoExtract(results, blockObj, matrix);
                                        continue;
                                    }                                    
                                }
                                var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                                results.AddRange(DoExtract(blockObj, mcs2wcs, newContainers));
                            }
                            else
                            {
                                if (_visitor is ISetContainer iSetContainer)
                                {
                                    iSetContainer.SetContainers(newContainers);
                                }                               
                                _visitor.DoExtract(results, dbObj, matrix);
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
