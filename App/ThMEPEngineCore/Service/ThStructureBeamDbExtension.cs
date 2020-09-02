using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Service
{
    public class ThStructureBeamDbExtension : ThStructureDbExtension,IDisposable
    {
        public List<Curve> BeamCurves { get; set; }
        public ThStructureBeamDbExtension(Database db):base(db)
        {
            LayerFilter = ThBeamLayerManager.GeometryXrefLayers(db);
            BeamCurves = new List<Curve>();
        }
        public void Dispose()
        {
            foreach (var curve in BeamCurves)
            {
                curve.Dispose();
            }
            BeamCurves.Clear();
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
                            //if (ThStructureUtils.IsBeamXref(btr.PathName))
                            //{
                            //}
                            var mcs2wcs = blkRef.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                            BeamCurves.AddRange(BuildElementCurves(blkRef, mcs2wcs));                            
                        }
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
                    foreach (var objId in blockTableRecord)
                    {
                        var dbObj = acadDatabase.Element<Entity>(objId);
                        if (dbObj is BlockReference blockObj)
                        {
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
