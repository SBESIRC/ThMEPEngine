using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class BlockExtractService
    {
        public static DBObjectCollection ExtractBlocks(Database db, string blockName)
        {
            Func<Entity, bool> IsBlkNameQualified = (e) =>
            {
                if (e is BlockReference br)
                {
                    return br.GetEffectiveName().ToUpper().Contains(blockName.ToUpper());
                }
                return false;
            };
            var blkVisitor = new ThBlockReferenceExtractionVisitor();
            blkVisitor.CheckQualifiedLayer = (e) => true;
            blkVisitor.CheckQualifiedBlockName = IsBlkNameQualified;

            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(blkVisitor);
            extractor.Extract(db); // 提取块中块(包括外参)
            extractor.ExtractFromMS(db); // 提取本地块

            using (var acadDb = AcadDatabase.Use(db))
            {
                blkVisitor.Results.ForEach(e =>
                {
                    var attribute = e.Data;
                });
            }
            return blkVisitor.Results.Select(o => o.Geometry).ToCollection();
        }
        public static DBObjectCollection ExtractBlocks(Database db, List<string> blockNames)
        {
            Func<Entity, bool> IsBlkNameQualified = (e) =>
            {
                if (e is BlockReference br)
                {
                    return br.GetEffectiveName().IsContainsIn(blockNames);
                }
                return false;
            };
            var blkVisitor = new ThBlockReferenceExtractionVisitor();
            blkVisitor.CheckQualifiedLayer = (e) => true;
            blkVisitor.CheckQualifiedBlockName = IsBlkNameQualified;

            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(blkVisitor);
            extractor.Extract(db); // 提取块中块(包括外参)
            extractor.ExtractFromMS(db); // 提取本地块

            using (var acadDb = AcadDatabase.Use(db))
            {
                blkVisitor.Results.ForEach(e =>
                {
                    var attribute = e.Data;
                });
            }
            return blkVisitor.Results.Select(o => o.Geometry).ToCollection();
        }

        public static DBObjectCollection ExtractBlocks(Database db, string blockName, out HashSet<BlockReference> hydrantWithReel)
        {
            Func<Entity, bool> IsBlkNameQualified = (e) =>
            {
                if (e is BlockReference br)
                {
                    return br.GetEffectiveName().ToUpper().Contains(blockName.ToUpper());
                }
                return false;
            };
            var blkVisitor = new ThBlockReferenceExtractionVisitor();
            blkVisitor.CheckQualifiedLayer = (e) => true;
            blkVisitor.CheckQualifiedBlockName = IsBlkNameQualified;

            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(blkVisitor);
            extractor.Extract(db); // 提取块中块(包括外参)
            extractor.ExtractFromMS(db); // 提取本地块

            var _hydrantWithReel = new HashSet<BlockReference>();
            using (var acadDb = AcadDatabase.Use(db))
            {
                blkVisitor.Results.ForEach(e =>
                {
                    var attribute = e.Data;
                    if (!(attribute is null))
                    {
                        var reel = attribute.ToString().Contains("卷盘");
                        if (reel)
                        {
                            _hydrantWithReel.Add(e.Geometry as BlockReference);
                        }
                    }
                });
            }
            hydrantWithReel = _hydrantWithReel;
            return blkVisitor.Results.Select(o => o.Geometry).ToCollection();
        }
    }

    public class ThBlockReferenceExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public Func<Entity, bool> CheckQualifiedBlockName { get; set; }

        public ThBlockReferenceExtractionVisitor()
        {
            CheckQualifiedLayer = base.CheckLayerValid;
            CheckQualifiedBlockName = (Entity entity) => true;
        }

        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference br)
            {
                elements.AddRange(Handle(br, matrix));
            }
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements,
            BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
            }
        }

        private List<ThRawIfcDistributionElementData> Handle(BlockReference br, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(br) && CheckLayerValid(br))
            {
                var clone = br.Clone() as BlockReference;
                if (clone != null)
                {
                    clone.TransformBy(matrix);
                    results.Add(new ThRawIfcDistributionElementData()
                    {
                        Geometry = clone,
                        Data = br.ObjectId.GetDynBlockValue("可见性"),
                    });
                }
            }
            return results;
        }

        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else
            {
                return false;
            }
        }

        public override bool IsDistributionElement(Entity entity)
        {
            return CheckQualifiedBlockName(entity);
        }

        public override bool CheckLayerValid(Entity curve)
        {
            return true;
        }

        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout)
            {
                return false;
            }
            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable)
            {
                return false;
            }
            return true;
        }
    }
}
