using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Service
{
    public class ThStructureBeamAnnotationDbExtension : ThStructureDbExtension, IDisposable
    {
        public List<ThIfcBeamAnnotation> Annotations { get; set; }
        public ThStructureBeamAnnotationDbExtension(Database db) : base(db)
        {
            Annotations = new List<ThIfcBeamAnnotation>();
            LayerFilter = ThBeamLayerManager.AnnotationXrefLayers(db);
        }

        public override void BuildElementCurves()
        {
            //
        }
        public void Dispose()
        {
            //
        }

        public override void BuildElementTexts()
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
                        Annotations.AddRange(BuildElementTexts(blkRef, mcs2wcs));
                    }
                }
            }
        }

        private IEnumerable<ThIfcBeamAnnotation> BuildElementTexts(BlockReference blockReference, Matrix3d matrix)
        {
            List<ThIfcBeamAnnotation> annotations = new List<ThIfcBeamAnnotation>();
            if (blockReference.BlockTableRecord == ObjectId.Null)
            {
                return annotations;
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
                            if (blockObj.BlockTableRecord.IsNull)
                            {
                                continue;
                            }
                            if (blockObj.IsBuildElementBlockReference())
                            {
                                var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                                annotations.AddRange(BuildElementTexts(blockObj, mcs2wcs));
                            }
                        }
                        else if (dbObj is DBText dbtext)
                        {
                            if (CheckLayerValid(dbtext) && 
                                IsBuildElement(dbtext) && 
                                IsAnnotation(dbtext))
                            {
                                annotations.Add(new ThIfcBeamAnnotation(dbtext, matrix));
                            }
                        }
                    }
                }
            }
            return annotations;
        }

        protected override bool IsBuildElement(Entity entity)
        {
            return entity.Hyperlinks.Count > 0;
        }

        private bool IsAnnotation(DBText dbtext)
        {
            return dbtext.TextString.Contains('x');
        }
    }
}
