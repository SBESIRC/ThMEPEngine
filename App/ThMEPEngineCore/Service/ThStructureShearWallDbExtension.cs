using System;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThStructureShearWallDbExtension : ThStructureDbExtension,IDisposable
    {
        public List<Curve> ShearWallCurves { get; set; }
        public ThStructureShearWallDbExtension(Database db):base(db)
        {
            LayerFilter = ThShearWallLayerManager.GeometryXrefLayers(db);
            ShearWallCurves = new List<Curve>();
        }
        public void Dispose()
        {
            foreach (var curve in ShearWallCurves)
            {
                curve.Dispose();
            }
            ShearWallCurves.Clear();
        }
        public override void BuildElementCurves()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                foreach(var ent in acadDatabase.ModelSpace)
                {
                    if (ent is BlockReference blkRef)
                    {
                        if (blkRef.BlockTableRecord.IsNull)
                        {
                            continue;
                        }
                        BlockTableRecord btr = acadDatabase.Element<BlockTableRecord>(blkRef.BlockTableRecord);
                        var mcs2wcs = blkRef.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                        ShearWallCurves.AddRange(BuildElementCurves(blkRef, mcs2wcs));
                    }
                }
            }
        }
        private IEnumerable<Curve> BuildElementCurves(BlockReference blockReference, Matrix3d matrix)
        {
            List<Curve> curves = new List<Curve>();
            if(blockReference.BlockTableRecord==ObjectId.Null)
            {
                return curves;
            }
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                if (IsBuildElementBlock(blockTableRecord))
                {
                    var xclip = blockReference.XClipInfo();
                    foreach (var objId in blockTableRecord)
                    {
                        var dbObj = acadDatabase.Element<Entity>(objId);
                        if (dbObj is BlockReference blockObj)
                        {
                            if (blockObj.BlockTableRecord.IsNull)
                            {
                                continue;
                            }
                            if (blockObj.IsBuildElementBlockReference())
                            {
                                var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                                curves.AddRange(BuildElementCurves(blockObj, mcs2wcs));
                            }
                        }
                        else if (dbObj is Curve curve)
                        {
                            if (CheckLayerValid(curve))
                            {
                                var wcsCurve = curve.GetTransformedCopy(matrix) as Curve;
                                if (xclip.IsValid)
                                {
                                    // 暂时不裁剪剪力墙
                                    if (xclip.Polygon.Contains(wcsCurve))
                                    {
                                        curves.Add(wcsCurve);
                                    }
                                }
                                else
                                {
                                    curves.Add(wcsCurve);
                                }
                            }
                        }
                    }
                }
            }
            return curves;
        }
        public override void BuildElementTexts()
        {
            throw new NotImplementedException();
        }
    }
}
