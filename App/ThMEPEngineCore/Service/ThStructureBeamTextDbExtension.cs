using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Service
{
    public class ThStructureBeamTextDbExtension : ThStructureDbExtension, IDisposable
    {
        public List<DBText> BeamTexts { get; set; }
        public ThStructureBeamTextDbExtension(Database db) : base(db)
        {
            BeamTexts = new List<DBText>();
            LayerFilter = ThBeamLayerManager.AnnotationXrefLayers(db);
        }
        public void Dispose()
        {
            foreach (var text in BeamTexts)
            {
                text.Dispose();
            }
            BeamTexts.Clear();
        }
        public override void BuildElementCurves()
        {
            throw new NotSupportedException();
        }
        public override void BuildElementTexts()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is BlockReference blkRef)
                    {
                        BlockTableRecord btr = acadDatabase.Element<BlockTableRecord>(blkRef.BlockTableRecord);
                        if (btr.IsFromExternalReference || btr.IsFromOverlayReference)
                        {
                            if (ThStructureUtils.IsBeamXref(btr.PathName))
                            {
                                var mcs2wcs = blkRef.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                                BeamTexts.AddRange(BuildElementTexts(blkRef, mcs2wcs));
                            }
                        }
                    }
                }
            }
        }
        private IEnumerable<DBText> BuildElementTexts(BlockReference blockReference, Matrix3d matrix)
        {
            List<DBText> texts = new List<DBText>();
            if (blockReference.BlockTableRecord == ObjectId.Null)
            {
                return texts;
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
                                texts.AddRange(BuildElementTexts(blockObj, mcs2wcs));
                            }
                        }
                        else if (dbObj is DBText dbtext)
                        {
                            if (CheckLayerValid(dbtext))
                            {
                                texts.Add(dbtext.GetTransformedCopy(matrix) as DBText);
                            }
                        }
                    }
                }
            }
            return texts;
        }
    }
}
