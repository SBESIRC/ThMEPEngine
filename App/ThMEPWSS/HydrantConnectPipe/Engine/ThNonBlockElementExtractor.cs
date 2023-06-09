﻿using Linq2Acad;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.HydrantConnectPipe.Engine
{
    public class ThNonBlockElementExtractor
    {
        /// <summary>
        /// 是否提取外参对象
        /// </summary>
        public bool FindExternalReference { get; set; }
        private List<ThDistributionElementExtractionVisitor> Visitors { get; set; }

        public ThNonBlockElementExtractor()
        {
            FindExternalReference = false;
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
            // 获取本地元素，不考括块
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                acadDatabase.ModelSpace
                    .OfType<Entity>()
                    .ForEach(e =>
                    {
                        if(!(e is BlockReference))
                        {
                            Visitors.ForEach(v =>
                            {
                                if (v.CheckLayerValid(e) && v.IsDistributionElement(e))
                                {
                                    v.DoExtract(v.Results, e, Matrix3d.Identity);
                                }
                            });
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
                if (visitor.IsBuildElementBlockReference(blockReference))
                {                    
                    var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                    if (visitor.IsBuildElementBlock(blockTableRecord))
                    {
                        // 提取图元信息
                        foreach (var objId in blockTableRecord)
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
                                    if (visitor.IsDistributionElement(blockObj))
                                    {
                                        visitor.DoExtract(results, blockObj, matrix);
                                        continue;
                                    }
                                    var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                                    results.AddRange(DoExtract(blockObj, mcs2wcs, visitor));
                                }
                            }
                            else
                            {
                                if(visitor.IsDistributionElement(dbObj))
                                {
                                    visitor.DoExtract(results, dbObj, matrix);
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
