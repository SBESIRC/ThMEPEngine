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
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPEngineCore;
using DotNetARX;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class Vertical//提取管道末端标记
    {
        public DBObjectCollection DBobjsResults { get; private set; }
        public List<Point3dEx> VerticalPts { get; private set; }
        public DBObjectCollection Extract(AcadDatabase acadDatabase, Point3dCollection polygon)
        {
            var Results = acadDatabase.ModelSpace //TCH_PIPE
               .OfType<Entity>()
               .Where(e => IsTargetLayer(e.Layer))
               .Where(e => e.IsTCHPipe());

            var Results1 = acadDatabase.ModelSpace   //处理圆
                   .OfType<Circle>()
                   .Where(o => IsTargetLayer(o.Layer));

            var Results2 = ExtractBlocks(acadDatabase.Database, "定位立管"); 

            var DBObjs = Results.ToCollection();

            //spatialIndex不支持圆
            var map = new Dictionary<Polyline, Circle>();
            Results1.ToList().ForEach(o => map.Add(o.ToRectangle(), o));
            var spatialIndex1 = new ThCADCoreNTSSpatialIndex(map.Keys.ToCollection());
            var DBObjs1 = spatialIndex1.SelectCrossingPolygon(polygon);

            DBobjsResults = new DBObjectCollection();

            foreach (DBObject db in DBObjs)
            {
                ExplodeTZBlock(db as Entity, DBobjsResults);
            }
            foreach (var db in DBObjs1)//添加圆
            {
                var circle = map[db as Polyline];
                var dbPt = new DBPoint(circle.Center);
                DBobjsResults.Add(dbPt);
            }
            foreach (var db in Results2)
            {
                ExplodeDWLG(db as BlockReference, DBobjsResults);//添加定位立管
            }

            var rstSpatialIndex = new ThCADCoreNTSSpatialIndex(DBobjsResults);

            return rstSpatialIndex.SelectCrossingPolygon(polygon);
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


        private static bool IsTargetLayer(string layer)//立管图层
        {
            return layer.Equals("W-FRPT-HYDT-VPIPE")
                 || layer.Equals("W-FRPT-HYDT-EQPM")
                 || layer.Equals("W-FRPT-HYDT")
                 || layer.Equals("W-FRPT-EXTG");
        }

        private static void ExplodeTZBlock(Entity ent, DBObjectCollection DBobjsResults)
        {
            try
            {
                var objs = new DBObjectCollection();
                ent.Explode(objs);

                objs.Cast<Entity>()
                    .Where(e => e is Circle)
                    .ForEach(e => DBobjsResults.Add(new DBPoint((e as Circle).Center)));
            }
            catch(Exception ex)
            {
                ;
            }
        }

        public static void ExplodeDWLG(BlockReference br, DBObjectCollection DBobjsResults)//炸定位立管
        {
            var objColl = new DBObjectCollection();
            var objs = new DBObjectCollection();
            br.Explode(objColl);
            objColl.Cast<Entity>().Where(e => e is Circle).ForEach(e => objs.Add(e));
            var circles = objs.OfType<Circle>().OrderByDescending(e => e.Radius);
            if(circles.Count() > 0)
            {
                var circle = circles.First();
                var dbPt = new DBPoint(circle.Center);
                DBobjsResults.Add(dbPt);
            }
        }

        private static bool IsRepeatedPt(Point3dEx pt, List<Point3dEx> verticalPts)
        {
            double tor = 100.0;

            foreach (var pt2 in verticalPts)//去掉在一定范围内的重复点
            {
                if (pt2._pt.DistanceTo(pt._pt) < tor)
                {
                    return true;
                }
            }
            return false;
        }
        public List<Point3dEx> CreatePointList()
        {
            VerticalPts = new List<Point3dEx>();
            foreach (var db in DBobjsResults)
            {
                var centerPt = (db as DBPoint).Position;
                var pt = new Point3dEx(new Point3d(centerPt.X, centerPt.Y, 0));
                if(!IsRepeatedPt(pt, VerticalPts))
                {
                    VerticalPts.Add(pt);
                }
            }
#if DEBUG
            var layer = "立管标记";
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains(layer))
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, layer, 2);
                }
            }

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (var pt in VerticalPts)
                {
                    var c = new Circle(pt._pt, new Vector3d(0, 0, 1), 200);
                    c.LayerId = DbHelper.GetLayerId(layer);
                    acadDatabase.CurrentSpace.Add(c);
                }
            }
#endif
            return VerticalPts;
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
