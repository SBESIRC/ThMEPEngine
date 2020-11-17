using System;
using Linq2Acad;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThStructureColumnDbExtension : ThStructureDbExtension, IDisposable
    {
        public List<Curve> ColumnCurves { get; set; }
        public ThStructureColumnDbExtension(Database db) : base(db)
        {
            LayerFilter = ThStructureColumnLayerManager.HatchXrefLayers(db);
            ColumnCurves = new List<Curve>();
        }
        public void Dispose()
        {
            foreach (var curve in ColumnCurves)
            {
                curve.Dispose();
            }
            ColumnCurves.Clear();
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
                        ColumnCurves.AddRange(BuildElementCurves(blkRef, mcs2wcs));
                    }
                }
            }
        }

        private IEnumerable<Curve> BuildElementCurves(BlockReference blockReference, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                List<Curve> curves = new List<Curve>();
                if (IsBuildElementBlockReference(blockReference) &&
                    blockReference.IsVisible(acadDatabase))
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
                                if (blockObj.IsBuildElementBlockReference() &&
                                    blockObj.IsVisible(acadDatabase))
                                {
                                    var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                                    curves.AddRange(BuildElementCurves(blockObj, mcs2wcs));
                                }
                            }
                            else if (dbObj is Hatch hatch)
                            {
                                if (IsBuildElement(hatch) && 
                                    CheckLayerValid(hatch) &&
                                    hatch.IsVisible(acadDatabase))
                                {
                                    // 暂时不支持有“洞”的填充
                                    hatch.Boundaries().ForEachDbObject(o =>
                                    {
                                        if (o is Polyline poly)
                                        {
                                            // 设计师会为矩形柱使用非比例的缩放
                                            // 从而获得不同形状的矩形柱
                                            // 考虑到多段线不能使用非比例的缩放
                                            // 这里采用一个变通方法：
                                            //  将矩形柱转化成实线，缩放后再转回多段线
                                            if (poly.IsRectangle())
                                            {
                                                var solid = poly.ToSolid();
                                                solid.TransformBy(matrix);
                                                curves.Add(solid.ToPolyline());
                                            }
                                            else
                                            {
                                                poly.TransformBy(matrix);
                                                curves.Add(poly);
                                            }
                                        }
                                        else if (o is Circle circle)
                                        {
                                            // 圆形柱
                                            var polyCircle = circle.ToPolyCircle();
                                            polyCircle.TransformBy(matrix);
                                            curves.Add(polyCircle);
                                        }
                                    });
                                }
                            }
                            else if (dbObj is Solid solid)
                            {
                                if (IsBuildElement(solid) &&
                                    CheckLayerValid(solid) &&
                                    solid.IsVisible(acadDatabase))
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
