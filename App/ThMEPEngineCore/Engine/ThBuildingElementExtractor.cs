using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThBuildingElementExtractor
    {
        private List<ThBuildingElementExtractionVisitor> Visitors { get; set; }

        public ThBuildingElementExtractor()
        {
            Visitors = new List<ThBuildingElementExtractionVisitor>();
        }

        public void Accept(ThBuildingElementExtractionVisitor visitor)
        {
            Visitors.Add(visitor);
        }

        public void Extract(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is BlockReference blkRef)
                    {
                        if (blkRef.BlockTableRecord.IsNull)
                        {
                            continue;
                        }
                        var mcs2wcs = blkRef.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                        DoExtract(blkRef, mcs2wcs);
                    }
                }
            }
        }

        private void DoExtract(BlockReference blockReference, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blockReference.Database))
            {
                Visitors.ForEach(o =>
                {
                    if (o.IsBuildElementBlockReference(blockReference))
                    {
                        var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                        if (o.IsBuildElementBlock(blockTableRecord))
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
                                    if (o.IsBuildElementBlockReference(blockObj))
                                    {
                                        var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                                        DoExtract(blockObj, mcs2wcs);
                                    }
                                }
                                else
                                {
                                    o.DoExtract(dbObj, matrix);
                                }
                            }
                        }
                    }
                });
            }
        }
    }
}
