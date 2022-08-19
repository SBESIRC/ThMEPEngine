using AcHelper;
using Linq2Acad;
using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThBuildingElementExtractor
    {
        protected List<ThBuildingElementExtractionVisitor> Visitors { get; set; }

        public ThBuildingElementExtractor()
        {
            Visitors = new List<ThBuildingElementExtractionVisitor>();
        }

        public void Accept(ThBuildingElementExtractionVisitor visitor)
        {
            Visitors.Add(visitor);
        }

        public void Accept(ThBuildingElementExtractionVisitor[] visitors)
        {
            Visitors.AddRange(visitors);
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
                        Visitors.ForEach(v =>
                        {
                            v.Results.AddRange(DoExtract(o, mcs2wcs, uid2ms, v));
                        });
                    });
            }
        }

        public virtual void ExtractFromMS(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var uid2ms = UniqueIdMS();
                acadDatabase.ModelSpace
                    .OfType<Entity>()
                    .ForEach(e =>
                    {
                        Visitors.ForEach(v =>
                        {
                            v.Results.AddRange(DoExtract(e, uid2ms, v));
                        });
                    });
            }
        }

        public virtual void ExtractFromEditor(Point3dCollection frame)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var uid2ms = UniqueIdMS();
                var psr = Active.Editor.SelectCrossingPolygon(frame);
                if (psr.Status == PromptStatus.OK)
                {
                    psr.Value.GetObjectIds().ForEach(o =>
                    {
                        var e = acadDatabase.ElementOrDefault<Entity>(o);
                        if (e != null)
                        {
                            Visitors.ForEach(v =>
                            {
                                v.Results.AddRange(DoExtract(e, uid2ms, v));
                            });
                        }
                    });
                }
            }
        }

        public virtual void ExtractFromEditor(Point3dCollection frame, SelectionFilter filter)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var uid2ms = UniqueIdMS();
                var psr = Active.Editor.SelectCrossingPolygon(frame, filter);
                if (psr.Status == PromptStatus.OK)
                {
                    psr.Value.GetObjectIds().ForEach(o =>
                    {
                        var e = acadDatabase.ElementOrDefault<Entity>(o);
                        if (e != null)
                        {
                            Visitors.ForEach(v =>
                            {
                                v.Results.AddRange(DoExtract(e, uid2ms, v));
                            });
                        }
                    });
                }
            }
        }

        protected List<ThRawIfcBuildingElementData> DoExtract(Entity e, int uid, ThBuildingElementExtractionVisitor visitor)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (visitor.IsBuildElement(e) && visitor.CheckLayerValid(e))
            {
                visitor.DoExtract(results, e, Matrix3d.Identity, uid);
            }
            return results;
        }

        private List<ThRawIfcBuildingElementData> DoExtract(
            BlockReference blockReference,Matrix3d matrix, int uid,
            ThBuildingElementExtractionVisitor visitor)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blockReference.Database))
            {
                var results = new List<ThRawIfcBuildingElementData>();
                if (blockReference.BlockTableRecord.IsValid)
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
                                    visitor.DoExtract(results, blockObj, matrix, uid);
                                    continue;
                                }
                                var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                                var uid2ms = ThMEPDbUniqueIdService.Combine(uid, ThMEPDbUniqueIdService.UniqueId(objId));
                                results.AddRange(DoExtract(blockObj, mcs2wcs, uid2ms, visitor));
                            }
                            else
                            {
                                 visitor.DoExtract(results, dbObj, matrix, uid);
                            }
                        }

                        // 过滤XClip外的图元信息
                        visitor.DoXClip(results, blockReference, matrix);
                    }
                }
                return results;
            }
        }

        private int UniqueIdMS()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                return ThMEPDbUniqueIdService.UniqueId(acadDatabase.ModelSpace.ObjectId);
            }
        }
    }
}
