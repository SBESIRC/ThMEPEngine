using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Service
{
    public class ThStructureColumnDbExtension : IDisposable
    {
        public Database HostDb { get; set; }
        public List<Curve> ColumnCurves { get; set; }
        public List<string> LayerFilter { get; set; }

        public ThStructureColumnDbExtension(Database db)
        {
            HostDb = db;
            ColumnCurves = new List<Curve>();
            LayerFilter = ThColumnLayerManager.GeometryLayers(db);
        }
        public void Dispose()
        {
            foreach(var curve in ColumnCurves)
            {
                curve.Dispose();
            }
            ColumnCurves.Clear();
        }

        public void BuildElementCurves()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                foreach(var ent in acadDatabase.ModelSpace)
                {
                    if (ent is BlockReference blkRef)
                    {
                        BlockTableRecord btr = acadDatabase.Element<BlockTableRecord>(blkRef.BlockTableRecord);
                        if (btr.IsFromExternalReference || btr.IsFromOverlayReference)
                        {
                            var mcs2wcs = blkRef.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                            ColumnCurves.AddRange(BuildElementCurves(blkRef, mcs2wcs));
                        }
                    }
                }
            }
        }
        private IEnumerable<Curve> BuildElementCurves(BlockReference blockReference, Matrix3d matrix)
        {
            List<Curve> curves = new List<Curve>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                if (IsBuildElementBlock(blockTableRecord))
                {
                    foreach (var objId in blockTableRecord)
                    {
                        var dbObj = acadDatabase.Element<Entity>(objId);
                        if (dbObj is BlockReference blockObj)
                        {
                            if(blockObj.IsBuildElementBlockReference())
                            {
                                var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                                curves.AddRange(BuildElementCurves(blockObj, mcs2wcs));
                            }
                        }
                        else if(dbObj is Curve curve)
                        {
                            if(CheckCurveLayerValid(curve))
                            {
                                curves.Add(curve.GetTransformedCopy(matrix) as Curve);
                            }
                        }
                    }
                }
            }
            return curves;
        }
        private bool CheckCurveLayerValid(Curve curve)
        {
            return LayerFilter.Where(o => string.Compare(curve.Layer, o, true) == 0).Any();
        }
        private bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 暂时不支持动态块，外部参照，覆盖
            if (blockTableRecord.IsDynamicBlock )
            {
                return false;
            }

            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return false;
            }

            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable)
            {
                return false;
            }

            return true;
        }
    }
}
