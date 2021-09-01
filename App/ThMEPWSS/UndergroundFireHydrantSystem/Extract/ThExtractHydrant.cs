using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class ThExtractHydrant//提取管道末端标记
    {
        public IEnumerable<Entity> Results { get; private set; }
        public IEnumerable<Circle> Results1 { get; private set; }
        public DBObjectCollection DBObjs { get; private set; }
        public DBObjectCollection DBobjsResults { get; private set; }
        public DBObjectCollection DBObjs1 { get; private set; }
        public List<Point3dEx> HydrantPosition { get; private set; }
        public DBObjectCollection Extract(AcadDatabase acadDatabase, Point3dCollection polygon)
        {

            Results = acadDatabase.ModelSpace
               .OfType<Entity>()
               .Where(o => o is not Circle)
               .Where(o => !IsTCHPipeFitting(o))
               .Where(o => o.Layer.ToUpper() == "W-FRPT-HYDT-EQPM" ||
                           o.Layer.ToUpper() == "W-WSUP-COOL-PIPE" ||
                           o.Layer.ToUpper() == "W-FRPT-EXTG" ||
                           o.Layer.ToUpper() == "W-FRPT-HYDT" ||
                           o.Layer.ToUpper() == "W-RAIN-EQPM" ||
                           o.Layer.ToUpper() == "W-WSUP-DIMS" ||
                           o.Layer.ToUpper() == "0");

            Results1 = acadDatabase.ModelSpace
                   .OfType<Circle>()
                   .Where(o => o.Layer.ToUpper() == "W-FRPT-HYDT-EQPM" ||
                          o.Layer.ToUpper() == "W-FRPT-EXTG" ||
                          o.Layer.ToUpper() == "W-FRPT-HYDT");

            var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
            DBObjs = spatialIndex.SelectCrossingPolygon(polygon);

            //spatialIndex不支持圆
            var map = new Dictionary<Polyline, Circle>();
            Results1.ToList().ForEach(o => map.Add(o.ToRectangle(), o));
            var spatialIndex1 = new ThCADCoreNTSSpatialIndex(map.Keys.ToCollection());
            DBObjs1 = spatialIndex1.SelectCrossingPolygon(polygon);

            DBobjsResults = new DBObjectCollection();
            foreach (DBObject db in DBObjs)
            {
                if (db is Entity entity)
                {
                    if (entity is BlockReference br)
                    {
                        if (br.GetEffectiveName().Contains("带定位立管"))
                        {
                            var circle = ExplodeDWLG(br);
                            DBobjsResults.Add(circle);
                            continue;
                        }
                    }
                    AddBlockReference(acadDatabase, DBobjsResults, entity);
                }
            }
            foreach (var db in DBObjs1)
            {
                DBobjsResults.Add(map[db as Polyline]);
            }

            return DBobjsResults;
        }

        public static Circle ExplodeDWLG(BlockReference br)//炸定位立管
        {
            var objColl = new DBObjectCollection();
            br.Explode(objColl);
            foreach (var c in objColl)
            {
                if (c is Circle circle)
                {
                    if (circle.Radius > 20)
                    {
                        return new Circle(new Point3d(circle.Center.X, circle.Center.Y, 0), new Vector3d(0, 0, 1), 50);
                    }
                }
            }
            return new Circle();
        }

        public static void AddBlockReference(AcadDatabase acadDatabase, DBObjectCollection DBobjs, Entity entity)
        {
            if (IsTCHPipeFitting(entity))
            {
                return;
            }
            if (IsTCHPipe(entity))//传入类型为天正pipe
            {
                var entity2 = acadDatabase.Element<Entity>(entity.ObjectId);
                var objCollection = new DBObjectCollection();
                if (entity2 is null)
                {
                    return;
                }
                entity2.Explode(objCollection);
                objCollection.Cast<Entity>()
                    .Where(o => o is Circle)
                    .ForEach(o => DBobjs.Add((DBObject)o));
                return;
            }
            else if (entity is BlockReference bkr)//传入类型为 BlockReference
            {
                if (bkr.Name.Contains("设计区"))
                {
                    return;
                }
                var objs = new DBObjectCollection();//创建 object 集合
                var blockRecordId = bkr.BlockTableRecord;//提取 block ID
                var btr = acadDatabase.Blocks.Element(blockRecordId);
                int indx = 0;
                foreach (var entId in btr)
                {
                    var dbObj = acadDatabase.Element<Entity>(entId);
                    if (dbObj.Bounds is null)
                    {
                        continue;
                    }
                    if (dbObj is BlockReference br)
                    {

                        if (br.GetEffectiveName().Contains("室内消火栓平面") || br.GetEffectiveName().Contains("蝶阀") ||
                            br.GetEffectiveName().Contains("灭火器") || br.GetEffectiveName().Contains("水流指示器") ||
                            br.GetEffectiveName().Contains("压力表"))
                        {
                            indx += 1;
                            continue;
                        }
                        else if (br.GetEffectiveName().Contains("带定位立管"))
                        {
                            var objs1 = new DBObjectCollection();//创建 object 集合
                            if (bkr is null)
                            {
                                continue;
                            }
                            bkr.Explode(objs1);
                            if (indx > objs1.Count - 1)
                            {
                                continue;
                            }
                            if (objs1[indx] is BlockReference bk)
                            {
                                var circle = ExplodeDWLG(bk);
                                DBobjs.Add(circle);
                            }
                            indx += 1;
                            continue;
                        }
                        else
                        {
                            var objs1 = new DBObjectCollection();//创建 object 集合
                            if (br is not null)
                            {
                                br.Explode(objs1);
                                if (indx > objs1.Count - 1)
                                {
                                    continue;
                                }
                                if (objs1[indx] is Entity ent)
                                    AddBlockReference(acadDatabase, DBobjs, ent);
                                indx += 1;
                                continue;
                            }
                        }
                    }

                    else if (dbObj is Circle circle)//为圆
                    {
                        var rstCircle = new Circle(new Point3d(circle.Center.X, circle.Center.Y, 0), new Vector3d(0, 0, 1), 50);
                        DBobjs.Add(rstCircle);
                    }

                    if (IsTCHPipe(dbObj))//传入类型为天正pipe
                    {
                        var entity2 = acadDatabase.Element<Entity>(entity.ObjectId);
                        var objCollection = new DBObjectCollection();
                        if (entity2 is null)
                        {
                            continue;
                        }
                        entity2.Explode(objCollection);
                        objCollection.Cast<Entity>()
                            .Where(o => o is Circle)
                            .ForEach(o => DBobjs.Add((DBObject)o));
                        foreach (Entity ent in objCollection)
                        {
                            if (IsTCHPipe(ent))
                            {
                                var subObjs = new DBObjectCollection();
                                ent.Explode(subObjs);
                                subObjs.Cast<Entity>()
                            .Where(o => o is Circle)
                            .ForEach(o => DBobjs.Add((DBObject)o));
                            }
                        }
                        return;
                    }
                }
            }
            else if (entity is Circle circle)//为圆
            {
                DBobjs.Add(circle);
            }
            else
            {
                return;
            }
        }

        public static bool IsTCHPipe(Entity entity)
        {
            string dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.Equals("TCH_PIPE");
        }

        public static bool IsTCHPipeFitting(Entity entity)
        {
            string dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.Equals("TCH_PIPEFITTING");
        }

        public List<Point3dEx> CreatePointList()
        {
            HydrantPosition = new List<Point3dEx>();

            foreach (var db in DBobjsResults)
            {
                var centerPt = (db as Circle).Center;
                var pt = new Point3dEx(new Point3d(centerPt.X, centerPt.Y, 0));
                if (!HydrantPosition.Contains(pt))
                {
                    HydrantPosition.Add(pt);
                }
            }
            return HydrantPosition;
        }
    }
}
