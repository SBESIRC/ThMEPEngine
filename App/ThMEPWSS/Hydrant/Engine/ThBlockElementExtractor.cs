using Linq2Acad;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;

namespace ThMEPWSS.Hydrant.Engine
{
    public class ThBlockElementExtractor
    {
        /// <summary>
        /// 是否提取外参对象
        /// </summary>
        public bool FindExternalReference { get; set; }
        private List<ThDistributionElementExtractionVisitor> Visitors { get; set; }

        public ThBlockElementExtractor()
        {
            FindExternalReference = true;
            Visitors = new List<ThDistributionElementExtractionVisitor>();
        }

        public void Accept(ThDistributionElementExtractionVisitor visitor)
        {
            Visitors.Add(visitor);
        }

        public void Extract(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is BlockReference blkRef)
                    {
                        if (blkRef.BlockTableRecord.IsNull)
                        {
                            continue;
                        }
                        if(!FindExternalReference)
                        {
                            //不要从外部参照提取
                            var blockTableRecord = acadDatabase.Blocks.Element(blkRef.BlockTableRecord);
                            if(blockTableRecord.IsFromExternalReference)
                            {
                                continue;
                            }
                        }
                        var mcs2wcs = blkRef.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                        Visitors.ForEach(o =>
                        {
                            o.Results.AddRange(DoExtract(blkRef, mcs2wcs, o));
                        });
                    }
                }
            }
        }

        public void ExtractFromMS(Database database)
        {
            // 获取本地块，不考虑外参块
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .ForEach(e =>
                    {
                        if(e.BlockTableRecord.IsValid)
                        {
                            var blockTableRecord = acadDatabase.Blocks.Element(e.BlockTableRecord);
                            if (!blockTableRecord.IsFromExternalReference)
                            { 
                                Visitors.ForEach(v =>
                                {
                                    if (v.CheckLayerValid(e) && v.IsDistributionElement(e))
                                    {
                                        v.DoExtract(v.Results, e, Matrix3d.Identity);
                                    }
                                });
                            }
                        }
                    });
            }
        }

        private List<ThRawIfcDistributionElementData> DoExtract(BlockReference blockReference, Matrix3d matrix, 
            ThDistributionElementExtractionVisitor visitor)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blockReference.Database))
            {
                var results = new List<ThRawIfcDistributionElementData>();
                if (visitor.IsBuildElementBlockReference(blockReference) && blockReference.Visible)
                {                    
                    var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                    if (visitor.IsBuildElementBlock(blockTableRecord))
                    {
                        //支持动态块
                        var data = new ThBlockReferenceData(blockReference.ObjectId);
                        var objs = data.VisibleEntities();//仅拿动态块中可见的部分
                        // 提取图元信息                        
                        foreach (ObjectId objId in objs)
                        {
                            var dbObj = acadDatabase.Element<Entity>(objId);
                            if (dbObj is BlockReference blockObj)
                            {
                                if (blockObj.BlockTableRecord.IsNull)
                                {
                                    continue;
                                }
                                if (visitor.IsBuildElementBlockReference(blockObj))
                                {
                                    if (visitor.CheckLayerValid(blockObj) && visitor.IsDistributionElement(blockObj))
                                    {
                                        visitor.DoExtract(results, blockObj, matrix);
                                        continue;
                                    }
                                    var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                                    results.AddRange(DoExtract(blockObj, mcs2wcs, visitor));
                                }
                            }
                        }

                        // 过滤XClip外的图元信息
                        visitor.DoXClip(results, blockReference, matrix);
                    }
                }
                return results;
            }
        }
    }
}
