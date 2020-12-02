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
using NetTopologySuite.Geometries;
using Dreambuild.AutoCAD;

namespace ThMEPEngineCore.Service
{
    public class ThStructureShearWallDbExtension : ThStructureDbExtension, IDisposable
    {
        public List<Entity> ShearWallCurves { get; set; }
        public ThStructureShearWallDbExtension(Database db) : base(db)
        {
            LayerFilter = ThStructureShearWallLayerManager.HatchXrefLayers(db);
            ShearWallCurves = new List<Entity>();
        }
        public void Dispose()
        {
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

        private IEnumerable<Entity> BuildElementCurves(BlockReference blockReference, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                List<Entity> ents = new List<Entity>();
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
                                if (IsBuildElementBlockReference(blockObj))
                                {
                                    var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                                    ents.AddRange(BuildElementCurves(blockObj, mcs2wcs));
                                }
                            }
                            else if (dbObj is Hatch hatch)
                            {
                                if (IsBuildElement(hatch) && 
                                    CheckLayerValid(hatch))
                                {
                                    var newHatch= hatch.GetTransformedCopy(matrix) as Hatch;
                                    var polygons = newHatch.ToPolygons();
                                    foreach(var polygon in polygons)
                                    {
                                        // 把“甜甜圈”式的带洞的Polygon（有且只有洞）转成不带洞的Polygon
                                        // 在区域划分时，剪力墙是“不可以用”区域，剪力墙的外部和内部都是“可用区域”
                                        // 通过这种处理，将剪力墙的外部和内部区域联通起来，从而获取正确的可用区域
                                        List<Polyline> gapOutlines = ThPolygonToGapPolylineService.ToGapPolyline(polygon);
                                        gapOutlines.ForEach(o => ents.Add(o));
                                    }
                                }
                            }
                            else if (dbObj is Solid solid)
                            {
                                if (IsBuildElement(solid) && 
                                    CheckLayerValid(solid))
                                {
                                    var poly = solid.ToPolyline();
                                    poly.TransformBy(matrix);
                                    ents.Add(poly);
                                }
                            }
                        }

                        var xclip = blockReference.XClipInfo();
                        if (xclip.IsValid)
                        {
                            xclip.TransformBy(matrix);
                            return ents.Where(o =>
                            {
                                if (o is Polyline polyline)
                                {
                                    return xclip.Contains(polyline);
                                }
                                else if (o is MPolygon mPolygon)
                                {
                                    return xclip.Contains(mPolygon);
                                }
                                else
                                {
                                    throw new NotSupportedException();
                                }
                            });
                        }
                    }
                }
                return ents;
            }
        }
        public override void BuildElementTexts()
        {
            throw new NotImplementedException();
        }
    }
}
