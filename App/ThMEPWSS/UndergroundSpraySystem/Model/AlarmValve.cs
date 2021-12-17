using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.ViewModel;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class AlarmValve//提取报警阀
    {
        public DBObjectCollection DBObjs { get; set; }
        public AlarmValve()
        {
            DBObjs = new DBObjectCollection();
        }
        public List<Point3d> Extract(Database database, Point3dCollection polygon)
        {
            var objs = new DBObjectCollection();
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = ExtractBlocks(acadDatabase.Database);

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results);
                var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);

                var pts = new List<Point3d>();
                foreach(var obj in dbObjs)
                {
                    var position = (obj as BlockReference).Position;
                    pts.Add(new Point3d(position.X, position.Y, 0));
                }
                return pts;
            }
        }


        private DBObjectCollection ExtractBlocks(Database db)
        {
            Func<Entity, bool> IsBlkNameQualified = (e) =>
            {
                if (e is BlockReference br)
                {
                    var blkName = br.GetEffectiveName();
                    return blkName.Contains("湿式报警阀平面") ||
                        (blkName.Contains("VALVE") && (blkName.Contains("520") || blkName.Contains("701")));
                }
                return false;
            };
            var blkVisitor = new ThBlockReferenceExtractionVisitor();
            blkVisitor.CheckQualifiedLayer = (e) => true;
            blkVisitor.CheckQualifiedBlockName = IsBlkNameQualified;

            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(blkVisitor);
            extractor.ExtractFromMS(db);
            extractor.Extract(db);
            return blkVisitor.Results.Select(o => o.Geometry).ToCollection();
        }
    }



    public class ThAlarmValveExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public ThAlarmValveExtractionVisitor()
        {
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
            try
            {
                var blkName = (entity as BlockReference).GetEffectiveName();
                return blkName.Contains("湿式报警阀平面") ||
                    (blkName.Contains("VALVE") && (blkName.Contains("520") || blkName.Contains("701")));
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public override bool CheckLayerValid(Entity curve)
        {
            var layer = curve.Layer.ToUpper();
            return layer == "W-FRPT-SPRL-EQPM";
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
