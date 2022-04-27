using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class ThExtractPipeMark
    {
        public IEnumerable<BlockReference> Results { get; private set; }
        public DBObjectCollection DBobj { get; private set; }
        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase
                   .ModelSpace
                   .OfType<BlockReference>()
                   .Where(o => IsTargetBlock(o)).ToList();
                
                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                DBobj = spatialIndex.SelectCrossingPolygon(polygon);
                //DBobj = ExtractBlocks(database, "消火栓环管标记");
                return DBobj;
            }
        }
        private bool IsTargetBlock(BlockReference block)
        {
            try
            {
                var valve = block.GetEffectiveName();
                return valve == "消火栓环管标记";
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public List<List<Point3d>> GetPipeMarkPoisition(ref Dictionary<Point3dEx, double> markAngleDic)
        {
            var poisition = new List<List<Point3d>>();
            foreach (var db in DBobj)
            {
                var pos = new List<Point3d>();
                var br = db as BlockReference;
                var offset1x = Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点1 X"));
                var offset1y = Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点1 Y"));
                var offset2x = Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点2 X"));
                var offset2y = Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点2 Y"));

                var offset1 = new Point3d(offset1x, offset1y, 0);
                var offset2 = new Point3d(offset2x, offset2y, 0);
                var pt1 = offset1.TransformBy(br.BlockTransform);
                var pt2 = offset2.TransformBy(br.BlockTransform);

                var ang1 = Convert.ToDouble(br.ObjectId.GetDynBlockValue("角度1")) + br.Rotation - Math.PI / 2;
                var ang2 = Convert.ToDouble(br.ObjectId.GetDynBlockValue("角度2")) + br.Rotation - Math.PI / 2;
                
                pos.Add(pt1);
                pos.Add(pt2);
                poisition.Add(pos);
                markAngleDic.Add(new Point3dEx(pt1), ang1);
                markAngleDic.Add(new Point3dEx(pt2), ang2);
            }
            return poisition;
        }
        private DBObjectCollection ExtractBlocks(Database db, string blockName)
        {
            Func<Entity, bool> IsBlkNameQualified = (e) =>
            {
                if (e is BlockReference br)
                {
                    return br.GetEffectiveName().ToUpper().Contains(blockName.ToUpper());
                }
                return false;
            };
            var blkVisitor = new ThPipemarkExtractionVisitor();
            blkVisitor.CheckQualifiedLayer = (e) => true;
            blkVisitor.CheckQualifiedBlockName = IsBlkNameQualified;

            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(blkVisitor);
            //extractor.Extract(db); // 提取块中块(包括外参)
            extractor.ExtractFromMS(db); // 提取本地块

            return blkVisitor.Results.Select(o => o.Geometry).ToCollection();
        }
    }
    public class ThPipemarkExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public Func<Entity, bool> CheckQualifiedBlockName { get; set; }
        public ThPipemarkExtractionVisitor()
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
