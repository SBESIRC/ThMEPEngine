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
    public class ThDoorMarkDbExtension : ThStructureDbExtension, IDisposable
    {
        public List<Entity> Texts { get; set; }
        public ThDoorMarkDbExtension(Database db) : base(db)
        {
            Texts = new List<Entity>();
        }
        public void Dispose()
        {
        }

        public override void BuildElementCurves()
        {
            throw new NotImplementedException();
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
                        Texts.AddRange(BuildElementTexts(blkRef, mcs2wcs));
                    }
                }
            }            
        }
        private IEnumerable<Entity> BuildElementTexts(BlockReference blockReference, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                var texts = new List<Entity>();
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
                                    texts.AddRange(BuildElementTexts(blockObj, mcs2wcs));
                                }
                            }
                            else if (dbObj is DBText dbText)
                            {
                                if (IsBuildElement(dbText) &&
                                    IsDoorMark(dbText))
                                {
                                    texts.Add(dbText.GetTransformedCopy(matrix) as DBText);
                                }
                            }
                            else if (dbObj is MText mText)
                            {
                                if (IsBuildElement(mText) &&
                                    IsDoorMark(mText))
                                {
                                    texts.Add(mText.GetTransformedCopy(matrix) as MText);
                                }
                            }
                        }
                        var xclip = blockReference.XClipInfo();
                        if (xclip.IsValid)
                        {
                            xclip.TransformBy(matrix);
                            return texts.Where(o => xclip.Contains(GetTextPosition(o)));
                        }
                    }
                }
                return texts;
            }
        }
        protected override bool IsBuildElement(Entity entity)
        {
            return entity.Hyperlinks.Count > 0;
        }
        private bool IsDoorMark(Entity entity)
        {
            var thPropertySet = ThPropertySet.CreateWithHyperlink(entity.Hyperlinks[0].Description);
            return thPropertySet.IsDoor;
        }
        private Point3d GetTextPosition(Entity ent)
        {
            if(ent is DBText dbText)
            {
                return dbText.Position;
            }
            else if(ent is MText mText)
            {
                return mText.Location;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
