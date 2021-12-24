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

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class Vertical//提取管道末端标记
    {
        public DBObjectCollection DBobjsResults { get; private set; }
        public List<Point3dEx> VerticalPts { get; private set; }
        public DBObjectCollection Extract(AcadDatabase acadDatabase, Point3dCollection polygon)
        {
            var Results = acadDatabase.ModelSpace  //处理非块非圆
               .OfType<Entity>()
               .Where(e => e is not Circle)
               .Where(e => IsTargetLayer(e.Layer))
               .Where(e => IsTargetObject(e));

            var Results1 = acadDatabase.ModelSpace   //处理圆
                   .OfType<Circle>()
                   .Where(o => IsTargetLayer(o.Layer));

            var Results2 = ExtractBlocks(acadDatabase.Database, "定位立管");
            
            var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
            var DBObjs = spatialIndex.SelectCrossingPolygon(polygon);

            //spatialIndex不支持圆
            var map = new Dictionary<Polyline, Circle>();
            Results1.ToList().ForEach(o => map.Add(o.ToRectangle(), o));
            var spatialIndex1 = new ThCADCoreNTSSpatialIndex(map.Keys.ToCollection());
            var DBObjs1 = spatialIndex1.SelectCrossingPolygon(polygon);

            var spatialIndex2 = new ThCADCoreNTSSpatialIndex(Results2);
            var DBObjs2 = spatialIndex2.SelectCrossingPolygon(polygon);

            DBobjsResults = new DBObjectCollection();
            foreach (DBObject db in DBObjs)
            {
                if(db is BlockReference br)//图块
                {
                    ExplodeBlock(br, DBobjsResults);
                }
                else//天正对象
                {
                    ExplodeTZBlock(db as Entity, DBobjsResults);
                } 
            }
            foreach (var db in DBObjs1)//添加圆
            {
                DBobjsResults.Add(map[db as Polyline]);
            }
            foreach(var db in DBObjs2)
            {
                ExplodeDWLG(db as BlockReference, DBobjsResults);//添加定位立管
            }

            return DBobjsResults;
        }

        private static bool IsTargetLayer(string layer)//立管图层
        {
            return (layer.Contains("FRPT") && !layer.Contains("SPRL")) //包含FRPT且不包含SPRL
                 ||(layer.Contains("HYDT")); //包含HYDT
        }
        private static bool IsTargetObject(Entity ent)
        {
            var type = ent.GetType().Name;
            return type.Equals("BlockReference")
                || type.Equals("ImpEntity");
        }
        private static void ExplodeBlock(BlockReference br, DBObjectCollection DBobjsResults)
        {
            if (IsDWLGBlock(br))//如果是定位立管
            {
                return;
            }
            else
            {
                var objs = new DBObjectCollection();
                br.Explode(objs);//把块炸开
                foreach (var obj in objs)//遍历
                {
                    var ent = obj as Entity;
                    if (!ent.Visible)
                    {
                        continue;
                    }
                    if (obj is Circle circle)//圆
                    {
                        if (IsTargetLayer(circle.Layer))
                        {
                            DBobjsResults.Add(circle);
                        }
                    }
                    if (obj is BlockReference)//块
                    {
                        ExplodeBlock(obj as BlockReference, DBobjsResults);//炸块
                    }
                }
            }
        }
        private static void ExplodeTZBlock(Entity ent, DBObjectCollection DBobjsResults)
        {
            if (ent is null) return;
            //炸天正块
            var objs = new DBObjectCollection();
            ent.Explode(objs);//把块炸开
            foreach (var obj in objs)//遍历
            {
                if (obj is Circle circle)//圆
                {
                    DBobjsResults.Add(circle);
                }
                if (ent.GetType().Name.Equals("ImpEntity"))//天正对象
                {
                    ExplodeTZBlock(obj as BlockReference, DBobjsResults);//炸
                }
            }
        }
        private static bool IsDWLGBlock(BlockReference br)
        {
            return br.Name.Contains("定位立管");
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
                DBobjsResults.Add(new Circle(new Point3d(circle.Center.X, circle.Center.Y, 0), new Vector3d(0, 0, 1), 50));
            }
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
            extractor.ExtractFromMS(db);
            extractor.Extract(db);
            return blkVisitor.Results.Select(o => o.Geometry).ToCollection();
        }


        public List<Point3dEx> CreatePointList()
        {
            VerticalPts = new List<Point3dEx>();

            foreach (var db in DBobjsResults)
            {
                var centerPt = (db as Circle).Center;
                var pt = new Point3dEx(new Point3d(centerPt.X, centerPt.Y, 0));
                if (!VerticalPts.Contains(pt))
                {
                    VerticalPts.Add(pt);
                }
            }
            return VerticalPts;
        }
    }


    public class ThVerticalExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public ThVerticalExtractionVisitor()
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
            return (entity as BlockReference)?.Name?.Contains("定位立管") ?? false;
        }

        public override bool CheckLayerValid(Entity curve)
        {
            var layer = curve.Layer.ToUpper();
            return (layer.Contains("FRPT") && !layer.Contains("SPRL")) //包含FRPT且不包含SPRL
                || (layer.Contains("HYDT")); //包含HYDT
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
