﻿using Linq2Acad;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.SystemDiagram.Engine
{
    public class ThEntityData
    {
        public object Data { get; set; }
        public Entity Geometry { get; set; }
    }
    public class ThEntityCommonExtractor
    {
        private List<ThEntityCommonExtractionVistor> Visitors { get; set; }

        public ThEntityCommonExtractor()
        {
            Visitors = new List<ThEntityCommonExtractionVistor>();
        }

        public void Accept(ThEntityCommonExtractionVistor visitor)
        {
            Visitors.Add(visitor);
        }

        public void Extract(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is BlockReference blkref)
                    {
                        var mcs2wcs = blkref.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                        Visitors.ForEach(o =>
                        {
                            o.Results.AddRange(DoExtract(blkref, mcs2wcs, o));
                        });
                    }
                }
            }
        }

        public void ExtractFromMS(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                acadDatabase.ModelSpace
                    .OfType<Entity>()
                    .ForEach(e =>
                    {
                        Visitors.ForEach(v =>
                        {
                            v.Results.AddRange(DoExtract(e, v));
                        });
                    });
            }
        }

        private List<ThEntityData> DoExtract(Entity e, ThEntityCommonExtractionVistor visitor)
        {
            var results = new List<ThEntityData>();
            //处理天正元素
            if (e.GetType().Name.ToUpper().Contains("IMP"))
            {
                //自定义元素
                try
                {
                    if (!ThMEPTCHService.IsTCHElement(e))
                        return results;
                    var dBObject = ThMEPTCHService.ExplodeTCHElement(e);
                    if (null == dBObject || dBObject.Count < 1)
                        return results;
                    foreach (Entity entity in dBObject)
                    {
                        results.AddRange(DoExtract(entity, visitor));
                    }
                }
                catch
                {
                    //自定义元素可能会无法炸开，这里不进行处理
                }
            }
            //正常元素
            else
            {
                if (visitor.CheckLayerValid(e))
                {
                    visitor.DoExtract(results, e);
                }
            }
            return results;
        }

        private List<ThEntityData> DoExtract(
            BlockReference blockReference, Matrix3d matrix,
            ThEntityCommonExtractionVistor visitor)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blockReference.Database))
            {
                var results = new List<ThEntityData>();
                if (visitor.IsSpatialElementBlockReference(blockReference))
                {
                    var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                    if (visitor.IsSpatialElementBlock(blockTableRecord))
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
                                if (visitor.IsSpatialElementBlockReference(blockObj))
                                {
                                    if (visitor.CheckLayerValid(blockObj))
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
                                visitor.DoExtract(results, dbObj, matrix);
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
