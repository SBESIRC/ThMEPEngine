using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System.Collections.Generic;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.DrainageSystemAG.DataEngine
{
    /// <summary>
    /// 这里获取非图块元素
    /// </summary>
    class ThBasicElementExtractor
    {
        private List<ThAnnotationElementExtractionVisitor> Visitors { get; set; }
        private EnumLoadDataSource loadDataSource;

        public ThBasicElementExtractor(EnumLoadDataSource dataSource = EnumLoadDataSource.Both)
        {
            loadDataSource = dataSource;
            Visitors = new List<ThAnnotationElementExtractionVisitor>();
        }
        public void Accept(ThAnnotationElementExtractionVisitor visitor) 
        {
            this.Visitors.Add(visitor);
        }
        public void Extract(Database database)
        {
            switch (loadDataSource) 
            {
                case EnumLoadDataSource.External:
                    ExtractExternal(database);
                    break;
                case EnumLoadDataSource.ModelSapce:
                    ExtractModelSpace(database);
                    break;
            }
            
        }
        void ExtractExternal(Database database) 
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    bool isBlcok = ent is BlockReference;
                    if (!isBlcok)
                        continue;
                    var blkRef = ent as BlockReference;
                    if (blkRef.BlockTableRecord.IsNull)
                        continue;
                    BlockTableRecord blockTableRecord = acadDatabase.Blocks.Element(blkRef.BlockTableRecord);
                    if (!blockTableRecord.IsFromExternalReference)
                        continue;
                    var mcs2wcs = blkRef.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                    Visitors.ForEach(o =>
                    {
                        var res = DoExtract(blkRef, mcs2wcs, o);
                        if (res != null && res.Count > 0)
                            o.Results.AddRange(res);
                    });
                }
            }
        }
        void ExtractModelSpace(Database database) 
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is BlockReference blkRef)
                    {
                        if (blkRef.BlockTableRecord.IsNull)
                            continue;
                        BlockTableRecord blockTableRecord = acadDatabase.Blocks.Element(blkRef.BlockTableRecord);
                        if (blockTableRecord.IsFromExternalReference)
                            continue;
                        var mcs2wcs = blkRef.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                        Visitors.ForEach(o =>
                        {
                            o.Results.AddRange(DoExtract(blkRef, mcs2wcs, o, true));
                        });
                    }
                    else 
                    {
                        var name = ent.GetType().Name.ToUpper();
                        var layerName = ent.Layer;
                        Visitors.ForEach(o =>
                        {
                            var results = new List<ThRawIfcAnnotationElementData>();
                            o.DoExtract(results, ent, Matrix3d.Identity);
                            o.Results.AddRange(results);
                        });
                    }

                }
            }
        }
        private List<ThRawIfcAnnotationElementData> DoExtract(BlockReference blockReference, Matrix3d matrix, ThAnnotationElementExtractionVisitor visitor,bool checkNotExternal=false)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blockReference.Database))
            {
                var name = blockReference.GetEffectiveName();
                var results = new List<ThRawIfcAnnotationElementData>();
                //if (!visitor.IsAnnotationElementBlockReference(blockReference))
                //    return results;
                var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                if (!visitor.IsAnnotationElementBlock(blockTableRecord))
                    return results;
                if (checkNotExternal && blockTableRecord.IsFromExternalReference)
                    return results;
                // 提取图元信息
                blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                foreach (var objId in blockTableRecord)
                {
                    var dbObj = acadDatabase.Element<Entity>(objId);
                    if (dbObj is BlockReference blockObj)
                    {
                        if (blockObj.BlockTableRecord.IsNull)
                        {
                            continue;
                        }
                        if (visitor.IsAnnotationElementBlockReference(blockObj))
                        {
                            var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                            results.AddRange(DoExtract(blockObj, mcs2wcs, visitor, checkNotExternal));
                        }
                    }
                    else
                    {
                        visitor.DoExtract(results, dbObj, matrix);
                    }
                }
                // 过滤XClip外的图元信息
                visitor.DoXClip(results, blockReference, matrix);

                return results;
            }
        }
    }
    /// <summary>
    /// 获取数据来源
    /// </summary>
    enum EnumLoadDataSource 
    {
        /// <summary>
        /// 本图纸中获取
        /// </summary>
        ModelSapce=1,
        /// <summary>
        /// 外参图纸中获取
        /// </summary>
        External=2,
        /// <summary>
        /// 全部数据
        /// </summary>
        Both=99,
    }
}
