using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Service
{
    public class ThLaneLineDbExtension : ThOtherDbExtension,IDisposable
    {
        public List<Curve> LaneCurves { get; set; }
        public ThLaneLineDbExtension(Database db):base(db)
        {
            LayerFilter = ThLaneLineLayerManager.GeometryXrefLayers(db);
            LaneCurves = new List<Curve>();
        }
        public void Dispose()
        {
            foreach (var curve in LaneCurves)
            {
                curve.Dispose();
            }
            LaneCurves.Clear();
        }
        public override void BuildElementCurves()
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
                            LaneCurves.AddRange(BuildElementCurves(blkRef, mcs2wcs));
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
                            if(CheckLayerValid(curve))
                            {
                                curves.Add(curve.GetTransformedCopy(matrix) as Curve);
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
