using System;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThStructureShearWallDbExtension : ThStructureDbExtension, IDisposable
    {
        public List<Curve> ShearWallCurves { get; set; }
        public ThStructureShearWallDbExtension(Database db) : base(db)
        {
            LayerFilter = ThStructureShearWallLayerManager.HatchXrefLayers(db);
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
                foreach (var ent in acadDatabase.ModelSpace)
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
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                List<Curve> curves = new List<Curve>();
                if (IsBuildElementBlockReference(blockReference))
                {
                    var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                    if (IsBuildElementBlock(blockTableRecord))
                    {
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
                            else if (dbObj is Hatch hatch)
                            {
                                if (IsBuildElement(hatch) && CheckLayerValid(hatch))
                                {
                                    // 暂时不支持有“洞”的填充
                                    var polys = hatch.ToPolylines();
                                    polys.ForEachDbObject(o => o.TransformBy(matrix));
                                    curves.AddRange(polys);
                                }
                            }
                            else if (dbObj is Solid solid)
                            {
                                if (IsBuildElement(solid) && CheckLayerValid(solid))
                                {
                                    var poly = solid.ToPolyline();
                                    poly.TransformBy(matrix);
                                    curves.Add(poly);
                                }
                            }
                        }

                        var xclip = blockReference.XClipInfo();
                        if (xclip.IsValid)
                        {
                            xclip.TransformBy(matrix);
                            return curves.Where(o => xclip.Contains(o));
                        }
                    }
                }
                return curves;
            }
        }

        public override void BuildElementTexts()
        {
            throw new NotImplementedException();
        }
    }
}
