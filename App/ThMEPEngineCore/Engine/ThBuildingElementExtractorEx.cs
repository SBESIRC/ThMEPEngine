using Linq2Acad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThBuildingElementExtractorEx: ThBuildingElementExtractor
    {
        /// <summary>
        /// WCS Points
        /// </summary>
        private Point3dCollection RangePts { get; set; }
        private Extents3d Envelop { get; set; }

        public ThBuildingElementExtractorEx()
        {
            RangePts = new Point3dCollection();
            Envelop = RangePts.Envelope();
            Visitors = new List<ThBuildingElementExtractionVisitor>();
        }
         
        public ThBuildingElementExtractorEx(Point3dCollection pts)
        {
            RangePts = pts;
            Envelop = RangePts.Envelope();
            Visitors = new List<ThBuildingElementExtractionVisitor>();
        }
        public void Accept(List<ThBuildingElementExtractionVisitor> visitors)
        {
            Visitors.AddRange(visitors);
        }

        public override void Extract(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => !o.BlockTableRecord.IsNull)
                    .ForEach(o =>
                    {
                        var mcs2wcs = o.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                        DoExtract(o, mcs2wcs, Visitors);
                    });
            }
        }

        private void DoExtract(BlockReference blockReference,Matrix3d matrix,
            List<ThBuildingElementExtractionVisitor> visitors)
        {
            using (var acadDb = AcadDatabase.Use(blockReference.Database))
            {
                if (!blockReference.BlockTableRecord.IsValid)
                {
                    return;
                }
                var blockTableRecord = acadDb.Blocks.Element(blockReference.BlockTableRecord);
                if (RangePts.Count > 2 && blockReference.Bounds!=null)
                {
                    var rec = blockTableRecord.GeometricExtents().ToRectangle();
                    rec.TransformBy(matrix);
                    if (!IsIntersect(rec.GeometricExtents))
                        return;
                }
                //筛选可以对当前块能操作的Visitor
                var executableVisitors = visitors.Where(v => v.IsBuildElementBlock(blockTableRecord)).ToList();
                if (executableVisitors.Count == 0)
                {
                    return;
                }
                var recordPreElements = new Dictionary<ThBuildingElementExtractionVisitor,
                    HashSet<ThRawIfcBuildingElementData>>();
                executableVisitors.ForEach(v =>
                {
                    var items = new List<ThRawIfcBuildingElementData>();
                    v.Results.ForEach(i => items.Add(i));
                    var elements = new HashSet<ThRawIfcBuildingElementData>(items);
                    recordPreElements.Add(v, elements);
                    v.Results = new List<ThRawIfcBuildingElementData>();
                });
                // 提取图元信息
                foreach (var objId in blockTableRecord)
                {
                    var dbObj = acadDb.Element<Entity>(objId);
                    if (dbObj is BlockReference blockObj)
                    {
                        if (blockObj.BlockTableRecord.IsNull)
                        {
                            continue;
                        }
                        var collectBlkVisitors = executableVisitors.
                            Where(v => v.IsBuildElementBlockReference(blockObj)).ToList();
                        collectBlkVisitors.ForEach(v => v.DoExtract(v.Results, blockObj, matrix));
                        var unCollectBlkVisitors = executableVisitors.
                            Where(v => !v.IsBuildElementBlockReference(blockObj)).ToList();
                        var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                        if (unCollectBlkVisitors.Count > 0)
                        {
                            DoExtract(blockObj, mcs2wcs, unCollectBlkVisitors);
                        }
                    }
                    else
                    {
                        executableVisitors.ForEach(v => v.DoExtract(v.Results, dbObj, matrix));
                    }
                }
                // 过滤XClip外的图元信息
                executableVisitors.ForEach(v => v.DoXClip(v.Results, blockReference, matrix));
                executableVisitors.ForEach(v =>
                {
                    v.Results.AddRange(recordPreElements[v]);
                });
            }
        }
        private bool IsIntersect(Extents3d recExtents)
        {
            return
                Envelop.MinPoint.X > recExtents.MaxPoint.X ||
                Envelop.MaxPoint.X < recExtents.MinPoint.X ||
                Envelop.MinPoint.Y > recExtents.MaxPoint.Y ||
                Envelop.MaxPoint.Y < recExtents.MinPoint.Y;
        }
    }
}
