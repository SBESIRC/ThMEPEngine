using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NetTopologySuite.Geometries;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPWSS.Assistant;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public class ThExtractHYDTPipeService//提取管道
    {
        public ThExtractHYDTPipeService()
        {
        }
        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var lines = ThDrainageSystemServiceGeoCollector.GetLines(
                    acadDatabase.ModelSpace.OfType<Entity>().ToList(),
                    layer => layer is "W-FRPT-1-HYDT-PIPE" or "W-FRPT-HYDT-PIPE" || layer.Contains("W-FRPT-HYDT-PIPE"));
                return GeoFac.CreateIntersectsSelector(lines.Select(x => x.ToLineString()).ToList())
                    (polygon.ToRect().ToPolygon()).
                    SelectMany(x => x.ToDbCollection().OfType<DBObject>()).ToCollection();
            }
        }
    }

    public class ThExtractValveService//提取阀门
    {
        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            var objs = new DBObjectCollection();
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsHYDTPipeLayer(o.Layer));
                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                // 阀块
                dbObjs.Cast<Entity>()
                    .Where(e => e is BlockReference)
                    .Where(e => IsValveBlock((BlockReference)e))
                    .ForEach(e => objs.Add(e));
                // 天正阀
                dbObjs.Cast<Entity>()
                    .Where(e => e.IsTCHValve())
                    .ForEach(e => objs.Add(e));
                return objs;
            }
        }
        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-FRPT-HYDT-EQPM";
        }

        private bool IsValveBlock(BlockReference blockReference)
        {
            var blkName = blockReference.GetEffectiveName().ToUpper();
            return blkName.Contains("阀") || blkName.Contains("VALVE");
        }
    }

    public class ThExtractGateValveService//提取闸阀
    {
        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            var objs = new DBObjectCollection();
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsHYDTPipeLayer(o.Layer));
                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                // 阀块
                dbObjs.Cast<Entity>()
                    .Where(e => e is BlockReference)
                    .Where(e => IsValveBlock((BlockReference)e))
                    .ForEach(e => objs.Add(e));
                // 天正阀
                foreach (var obj in dbObjs)
                {
                    if ((obj as Entity).IsTCHValve())
                    {
                        var dbColl = new DBObjectCollection();
                        (obj as Entity).Explode(dbColl);
                        dbColl.Cast<Entity>()
                            .Where(e => e is BlockReference)
                            .Where(e => IsValve((e as BlockReference).Name))
                            .ForEach(e => objs.Add(e));
                    }
                }
                return objs;
            }
        }
        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-FRPT-HYDT-EQPM";
        }

        private bool IsValveBlock(BlockReference blockReference)
        {
            var blkName = blockReference.GetEffectiveName().ToUpper();
            return blkName.Contains("截止阀") ||
                   blkName.Contains("闸阀") ||
                   blkName.Contains("296");
        }
        private bool IsValve(string valveName)
        {
            return valveName.Contains("截止阀") ||
                   valveName.Contains("296") ||
                   valveName.Contains("闸阀");
        }

        public List<Point3d> GetGateValveSite(DBObjectCollection objs)
        {
            var pts = new List<Point3d>();
            foreach (var db in objs)
            {
                var br = db as BlockReference;
                var pt1 = br.GeometricExtents.MaxPoint;
                var pt2 = br.GeometricExtents.MinPoint;
                var pt = General.GetMidPt(pt1, pt2);
                pts.Add(pt);
            }
            return pts;
        }
    }

    public class ThExtractCasing//提取套管
    {
        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            var objs = new List<Point3dEx>();
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                   .ModelSpace
                   .OfType<BlockReference>()
                   .Where(o => IsHYDTPipeLayer(o.Layer) && IsValveBlock(o));
                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);

                return dbObjs;
            }
        }
        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-BUSH";
        }

        private bool IsValveBlock(BlockReference blockReference)
        {
            var blkName = blockReference.GetEffectiveName().ToUpper();
            return blkName.Contains("套管");
        }
    }

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
                if(entity2 is null)
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
                            if(bkr is null)
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
                            if(br is not null)
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
                        if(entity2 is null)
                        {
                            continue;
                        }
                        entity2.Explode(objCollection);
                        objCollection.Cast<Entity>()
                            .Where(o => o is Circle)
                            .ForEach(o => DBobjs.Add((DBObject)o));
                        foreach(Entity ent in objCollection)
                        {
                            if(IsTCHPipe(ent))
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

        public bool IsNotTermCircle(BlockReference br)
        {
            if (br.GetEffectiveName().Contains("室内消火栓平面"))
            {
                return true;
            }
            if (br.GetEffectiveName().Contains("蝶阀"))
            {
                return true;
            }
            if (br.GetEffectiveName().Contains("灭火器"))
            {
                return true;
            }
            if (br.GetEffectiveName().Contains("水流指示器"))
            {
                return true;
            }
            return false;
        }

        public bool IsPipeTermBlock(BlockReference br)
        {
            if (br.GetEffectiveName().Contains("带定位立管"))
            {
                return true;
            }

            return false;
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
                   .Where(o => IsValve(o.GetEffectiveName()));
                //.Where(o => IsHYDTPipeLayer(o.Layer) && IsValve(o.GetEffectiveName()));

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                DBobj = spatialIndex.SelectCrossingPolygon(polygon);
                return DBobj;
            }
        }

        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-FRPT-NOTE";
        }

        private bool IsValve(string valve)
        {
            return valve.ToUpper() == "消火栓环管标记";
        }

        public List<List<Point3d>> GetPipeMarkPoisition()
        {
            var poisition = new List<List<Point3d>>();
            foreach (var db in DBobj)
            {
                var pos = new List<Point3d>();
                var br = db as BlockReference;
                var pt1 = new Point3d(br.Position.X + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点1 X")),
                    br.Position.Y + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点1 Y")), 0);
                var pt2 = new Point3d(br.Position.X + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点2 X")),
                    br.Position.Y + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点2 Y")), 0);
                pos.Add(pt1);
                pos.Add(pt2);
                poisition.Add(pos);
            }

            return poisition;
        }
    }

    public class ThExtractNodeTag//消火栓环管节点标记
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
                   .Where(o => IsHYDTPipeLayer(o.Layer) && IsNode(o.GetEffectiveName()));

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                DBobj = spatialIndex.SelectCrossingPolygon(polygon);

                return DBobj;
            }
        }

        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-FRPT-NOTE";
        }

        private bool IsNode(string node)
        {
            return node.ToUpper() == "消火栓环管节点标记";
        }

        public List<List<Point3dEx>> GetPointList()
        {
            var PointList = new List<List<Point3dEx>>();
            foreach (var db in DBobj)
            {
                var br = db as BlockReference;
                var ptls = new List<Point3dEx>();
                var pt1 = new Point3dEx(br.Position.X + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点1 X")),
                    br.Position.Y + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点1 Y")), 0);
                var pt2 = new Point3dEx(br.Position.X + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点2 X")),
                    br.Position.Y + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点2 Y")), 0);
                ptls.Add(pt1);
                ptls.Add(pt2);
                PointList.Add(ptls);
            }
            return PointList;
        }

        public Dictionary<Point3dEx, double> GetAngle()
        {
            var angList = new Dictionary<Point3dEx, double>();
            foreach (var db in DBobj)
            {
                var br = db as BlockReference;
                var ang1 = Convert.ToDouble(br.ObjectId.GetDynBlockValue("角度1"));
                var ang2 = Convert.ToDouble(br.ObjectId.GetDynBlockValue("角度2"));
                var pt1 = new Point3dEx(br.Position.X + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点1 X")),
                    br.Position.Y + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点1 Y")), 0);
                var pt2 = new Point3dEx(br.Position.X + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点2 X")),
                    br.Position.Y + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点2 Y")), 0);
                angList.Add(pt1, ang1);
                angList.Add(pt2, ang2);
            }
            return angList;
        }

        public Dictionary<Point3dEx, string> GetMark()
        {
            var markList = new Dictionary<Point3dEx, string>();

            foreach (var db in DBobj)
            {
                var br = db as BlockReference;
                var mark1 = br.ObjectId.GetAttributeInBlockReference("节点1");
                var mark2 = br.ObjectId.GetAttributeInBlockReference("节点2");
                var pt1 = new Point3dEx(br.Position.X + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点1 X")),
                    br.Position.Y + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点1 Y")), 0);
                var pt2 = new Point3dEx(br.Position.X + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点2 X")),
                    br.Position.Y + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点2 Y")), 0);
                markList.Add(pt1, mark1);
                markList.Add(pt2, mark2);
            }

            return markList;
        }
    }
    public class ThExtractLabelLine//引线提取
    {
        public DBObjectCollection DbTextCollection { get; private set; }
        public List<Line> LabelPosition { get; private set; }

        public ThExtractLabelLine()
        {
        }

        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsHYDTPipeLayer(o.Layer));

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var DBObjs = spatialIndex.SelectCrossingPolygon(polygon);

                DbTextCollection = new DBObjectCollection();

                var bkrCollection = new DBObjectCollection();
                DBObjs.Cast<Entity>()
                    .Where(o => o is Entity)
                    .ForEach(o => bkrCollection.Add(o));
                foreach (var bkr in bkrCollection)
                {
                    if (bkr is Entity ent)
                    {
                        ExplodeLabelLine(ent, DbTextCollection);
                    }
                }

                return DbTextCollection;
            }
        }
        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-RAIN-DIMS" ||
                   layer.ToUpper() == "W-RAIN-NOTE" ||
                   layer.ToUpper() == "W-DRAI-DIMS" ||
                   layer.ToUpper() == "W-WSUP-DIMS" ||
                   layer.ToUpper() == "W-FRPT-NOTE" ||
                   layer.ToUpper() == "W-FRPT-HYDT-DIMS";
        }

        private bool IsMarkLine(string str)
        {
            return str.Equals("TDbSymbMultiLeader") || str.Equals("TDbText");
        }


        public List<Line> CreateLabelLineList()
        {
            var LabelPosition = new List<Line>();

            if (DbTextCollection.Count != 0)
            {
                foreach (var db in DbTextCollection)
                {
                    var line = db as Line;
                    var br = db as BlockReference;
                    var pt1 = new Point3d(line.StartPoint.X, line.StartPoint.Y, 0);
                    var pt2 = new Point3d(line.EndPoint.X, line.EndPoint.Y, 0);
                    LabelPosition.Add(new Line(pt1, pt2));
                }
            }
            LabelPosition = PipeLineList.CleanLaneLines3(LabelPosition);
            return LabelPosition;
        }

        private void ExplodeLabelLine(Entity ent, DBObjectCollection dBObjects)
        {
            if (ent == null) return;

            if (ent is Line line)// Line 直接添加
            {
                if (!line.Layer.ToUpper().Contains("DEFPOINTS"))
                {
                    dBObjects.Add(line);
                }
                return;
            }
            if (ent is Polyline pline)
            {
                if (pline.Layer.ToUpper().Contains("DEFPOINTS"))
                {
                    return;
                }
            }
            if (ent is AlignedDimension || ent is Arc || ent is DBText || ent is Circle || ent.IsTCHText())//炸出圆 和 天正单行文字 就退出
            {
                return;
            }
            try
            {
                var dbObjs = new DBObjectCollection();
                ent.Explode(dbObjs);
                foreach (var obj in dbObjs)
                {
                    if (obj is Entity ent1)
                    {
                        ExplodeLabelLine(ent1, dBObjects);
                    }
                }
            }
            catch (Exception)
            {

            }

        }
    }

    public class ThExtractLabelText//文字提取
    {
        public List<Entity> Results { get; private set; }
        public DBObjectCollection DBObjs { get; private set; }

        public ThExtractLabelText()
        {
            Results = new List<Entity>();
        }

        public DBObjectCollection Extract(Database database, Point3dCollection polygon, ref double textWidth, ref string textModel)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsHYDTPipeLayer(o.Layer)).ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                DBObjs = spatialIndex.SelectCrossingPolygon(polygon);
                var dbTextCollection = new DBObjectCollection();

                var bkrCollection = new DBObjectCollection();//筛选BlockRefrence
                DBObjs.Cast<Entity>()
                    .Where(o => o is Entity)
                    .ForEach(o => bkrCollection.Add(o));
                foreach (var bkr in bkrCollection)
                {
                    if (bkr is Entity ent)
                    {
                        ExplodeText(ent, dbTextCollection, ref textWidth, ref textModel);
                    }
                }
                return dbTextCollection;
            }
        }
        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-RAIN-DIMS" ||
                   layer.ToUpper() == "W-FRPT-HYDT-DIMS" ||
                   layer.ToUpper() == "W-WSUP-DIMS" ||
                   layer.ToUpper() == "W-DRAI-DIMS" ||
                   layer.ToUpper() == "W-FRPT-NOTE" ||
                   layer.ToUpper() == "W-RAIN-NOTE";
        }

        private void ExplodeText(Entity ent, DBObjectCollection dBObjects, ref double textWidth, ref string textModel)
        {
            if (ent is DBText dbText)//DBText直接添加
            {
                if(dbText.TextString.Contains("De"))
                {
                    return; ;
                }
                if (!dbText.TextString.Contains("DN"))
                {
                    var tWidth = Math.Abs((ent as Entity).GeometricExtents.MaxPoint.X - (ent as Entity).GeometricExtents.MinPoint.X);
                    if (tWidth > textWidth && (ent as DBText).TextString.Contains("X"))
                    {
                        textWidth = tWidth;
                        textModel = (ent as DBText).TextString;
                    }
                    dBObjects.Add(ent);
                }
                return;
            }
            if (ent is AlignedDimension || ent is Arc || ent is Line || ent is Circle || ent is Polyline)//炸成线就退出
            {
                return;
            }
            if (ent.IsTCHText())//天正单行文字,先炸后添加
            {
                var texts = ent.ExplodeTCHText();
                foreach (var text in texts)
                {
                    var tWidth = Math.Abs((text as Entity).GeometricExtents.MaxPoint.X - (text as Entity).GeometricExtents.MinPoint.X);
                    if (tWidth > textWidth && (text as DBText).TextString.Trim().Contains("X"))
                    {
                        textWidth = tWidth;

                        textModel = (text as DBText).TextString;
                    }
                    dBObjects.Add((DBObject)text);
                }
                return;
            }
            try
            {
                var dbObjs = new DBObjectCollection();
                ent.Explode(dbObjs);
                foreach (var obj in dbObjs)
                {
                    if (obj is Entity ent1)
                    {
                        ExplodeText(ent1, dBObjects, ref textWidth, ref textModel);
                    }
                }
            }
            catch (Exception)
            {

            }
        }
    }

    public class ThExtractFireHydrant//室内消火栓平面
    {
        public List<Entity> Results { get; private set; }
        public DBObjectCollection DBobjs { get; private set; }

        public ThExtractFireHydrant()
        {

        }
        public void Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsHYDTPipeLayer(o.Layer))
                   .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);


                DBobjs = new DBObjectCollection();
                foreach (var db in dbObjs)
                {
                    if (db is BlockReference)
                    {
                        if (IsFireHydrant((db as BlockReference).GetEffectiveName()))
                        {
                            DBobjs.Add((DBObject)db);
                        }
                        else
                        {
                            var objs = new DBObjectCollection();

                            var blockRecordId = (db as BlockReference).BlockTableRecord;
                            var btr = acadDatabase.Blocks.Element(blockRecordId);

                            int indx = 0;
                            var indxFlag = false;
                            foreach (var entId in btr)
                            {
                                var dbObj = acadDatabase.Element<Entity>(entId);
                                if (dbObj is BlockReference)
                                {
                                    if (IsFireHydrant((dbObj as BlockReference).GetEffectiveName()))
                                    {
                                        indxFlag = true;
                                        break;
                                    }
                                }
                                indx += 1;
                            }

                            (db as BlockReference).Explode(objs);
                            if (indxFlag)
                            {
                                if (indx > objs.Count - 1)
                                {
                                    continue;
                                }
                                DBobjs.Add((DBObject)objs[indx]);
                            }

                        }
                    }
                }
            }
        }
        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-FRPT-HYDT" || layer.ToUpper() == "0";
        }

        private bool IsFireHydrant(string valve)
        {
            return valve.ToUpper().Contains("室内消火栓平面");
        }
    }

    public class ThExtractPipeDN//管径提取
    {
        public List<Entity> Results { get; private set; }
        public DBObjectCollection DBObjs { get; private set; }
        public DBObjectCollection DBObjsResult { get; private set; }

        public ThExtractPipeDN()
        {

        }

        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsPipeDNLayer(o.Layer)).ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                DBObjs = spatialIndex.SelectCrossingPolygon(polygon);

                DBObjsResult = new DBObjectCollection();
                foreach (var db in DBObjs)
                {
                    if (db is DBText)
                    {
                        if ((db as DBText).TextString.Contains("DN"))
                        {
                            DBObjsResult.Add((DBObject)db);
                        }
                    }
                    else if (db is Line)
                    {
                        continue;
                    }
                    else if (db is BlockReference)
                    {
                        if ((db as BlockReference).GetEffectiveName().Equals("消火栓环管标记"))
                        {
                            continue;
                        }
                        if ((db as BlockReference).GetEffectiveName().Equals("消火栓环管节点标记"))
                        {
                            continue;
                        }
                        var objID = (db as BlockReference).ObjectId;
                        var val = objID.GetDynBlockValue("可见性");
                        ;
                        if (val?.Contains("DN") == true)
                        {
                            var DNtext = new DBText();
                            DNtext.TextString = val;
                            DNtext.Position = (db as BlockReference).Position;
                            DNtext.Rotation = (db as BlockReference).Rotation;
                            DBObjsResult.Add(DNtext);
                        }
                        else
                        {
                            var objs = new DBObjectCollection();
                            (db as BlockReference).Explode(objs);
                            ;
                            foreach (var obj in objs)
                            {
                                if (obj is DBText)
                                {
                                    ;
                                }
                                else if (obj is BlockReference)
                                {
                                    ;
                                    var objs1 = new DBObjectCollection();
                                    (obj as BlockReference).Explode(objs1);
                                    ;
                                    foreach (var obj1 in objs1)
                                    {
                                        if (obj1 is DBText)
                                        {
                                            ;
                                            if ((obj1 as DBText)?.TextString.Contains("DN") == true)
                                            {
                                                ;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }

                    }
                    else
                    {
                        var objs = new DBObjectCollection();
                        (db as Entity).Explode(objs);
                        ;
                        foreach (var obj in objs)
                        {
                            if (obj is DBText)
                            {
                                DBObjsResult.Add((DBObject)obj);
                            }
                        }
                    }

                }
                return DBObjsResult;
            }
        }

        private bool IsPipeDNLayer(string layer)
        {
            return (layer.ToUpper().Equals("W-FRPT-NOTE") ||
                    layer.ToUpper().Equals("W-FRPT-HYDT-DIMS") ||
                    layer.ToUpper().Equals("W-DRAI-DIMS") ||
                    layer.ToUpper().Equals("W-RAIN-DIMS") ||
                    layer.ToUpper().Equals("TWT_TEXT"));
        }

        public Dictionary<Point3dEx, string> GetSlashDic(Dictionary<Line, List<Point3d>> leadLineDic, Dictionary<Line, List<Line>> segLineDic)
        {
            var slashDic = new Dictionary<Point3dEx, string>();//存放斜点的标注
            foreach (var lead in leadLineDic.Keys)
            {
                var nums = 0;

                if (leadLineDic[lead].Count < segLineDic[lead].Count)
                {
                    nums = leadLineDic[lead].Count;
                }
                else
                {
                    nums = segLineDic[lead].Count;
                }

                for (int i = 0; i < nums; i++)
                {
                    var slashPt = leadLineDic[lead][i];//对于每个斜边
                    var line = segLineDic[lead][i];//提取每个短线
                    var rectArea = ThFireHydrantSelectArea.CreateArea(line);//创建提取区域

                    var spatialIndex = new ThCADCoreNTSSpatialIndex(DBObjsResult.ToDBObjectList().ToCollection());
                    var dbObj = spatialIndex.SelectCrossingPolygon(rectArea);
                    if (dbObj.Count == 0)
                    {
                        continue;//跳过空边
                    }

                    if ((dbObj[0] as DBText).TextString.Contains("DN"))
                    {
                        slashDic.Add(new Point3dEx(slashPt), (dbObj[0] as DBText).TextString);
                        continue;
                    }
                    dynamic acObj = dbObj[0].AcadObject;
                    slashDic.Add(new Point3dEx(slashPt), acObj.TextString);
                }
            }
            return slashDic;
        }
    }


    public class ThExtractPipeDNLine//提取管径标注引线
    {
        public IEnumerable<Entity> Results { get; private set; }
        public DBObjectCollection DBObjs { get; private set; }
        public DBObjectCollection DBObjResults { get; private set; }
        public List<Line> LabelPosition { get; private set; }

        public ThExtractPipeDNLine()
        {
            DBObjResults = new DBObjectCollection();
        }

        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsPipeDNLayer(o.Layer));

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                DBObjs = spatialIndex.SelectCrossingPolygon(polygon);

                foreach (var db in DBObjs)
                {
                    if (db is DBText)
                    {
                        continue;
                    }
                    if (db is Line)//线段直接添加
                    {
                        DBObjResults.Add((DBObject)db);
                    }
                    else if (db is Polyline)//多段线打断添加
                    {
                        var pline = db as Polyline;
                        for (int i = 0; i < pline.NumberOfVertices - 1; i++)
                        {
                            var pt1 = pline.GetPoint3dAt(i);
                            var pt2 = pline.GetPoint3dAt(i + 1);
                            DBObjResults.Add((DBObject)new Line(pt1, pt2));
                        }
                    }
                    else
                    {
                        var br = new DBObjectCollection();
                        (db as Entity).Explode(br);
                        foreach (var l in br)
                        {
                            if (l is Line)
                            {
                                DBObjResults.Add((DBObject)l);
                            }
                        }
                    }
                }
                return DBObjResults;
            }
        }

        private bool IsPipeDNLayer(string layer)
        {
            return (layer.ToUpper().Contains("W-") && layer.ToUpper().Contains("-DIMS")) ||
                   layer.ToUpper().Equals("W-FRPT-NOTE");
        }

        public List<Point3d> ExtractSlash()
        {
            var SlashPts = new List<Point3d>();
            foreach (var db in DBObjResults)
            {
                var line = db as Line;
                if (PointAngle.IsSplashLine(line))
                {
                    var pt1 = line.StartPoint;
                    var pt2 = line.EndPoint;
                    var pt = new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);//中点作为斜线代表点
                    SlashPts.Add(pt);
                }
            }
            return SlashPts;
        }

        public Dictionary<Line, List<Point3d>> ExtractleadLine(List<Point3d> SlashPts)
        {
            var leadLines = new List<Line>();
            var leadLineDic = new Dictionary<Line, List<Point3d>>();
            foreach (var db in DBObjResults)
            {
                var line = db as Line;
                var ptList = new List<Point3d>();
                if (!PointAngle.IsSplashLine(line))//不是斜线才可能是引线
                {
                    foreach (var pt in SlashPts)//遍历斜线点
                    {
                        if (PtOnLine.PtIsOnLine(pt, line))//斜线在引线上
                        {
                            ptList.Add(pt);//添加
                        }
                    }
                }

                if (ptList.Count != 0)//引线存在斜线
                {
                    //对斜线点进行排序，按照从左到右或者从上到下
                    Sort.PointsSort(ref ptList);
                    leadLineDic.Add(line, ptList);
                }
            }
            return leadLineDic;
        }

        public Dictionary<Line, List<Line>> ExtractSegLine(Dictionary<Line, List<Point3d>> leadLineDic)
        {
            var segLineDic = new Dictionary<Line, List<Line>>();
            foreach (var lead in leadLineDic.Keys)
            {
                var lineList = new List<Line>();
                foreach (var db in DBObjResults)
                {
                    var line = db as Line;
                    if (!PointAngle.IsSplashLine(line) && !leadLineDic.Keys.Contains(line))
                    {
                        if (PtOnLine.PtIsOnLine(line.StartPoint, lead) || PtOnLine.PtIsOnLine(line.EndPoint, lead))
                        {
                            lineList.Add(line);
                        }
                    }
                }
                Sort.LinesSort(ref lineList);
                segLineDic.Add(lead, lineList);
            }
            return segLineDic;
        }



    }


    public class ThExtractStopLine
    {
        public List<Point3dEx> Extract(Database database, Point3dCollection polygon)
        {
            var objs = new DBObjectCollection();
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => o.Layer == "W-FRPT-HYDT-EQPM");
                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                
                dbObjs.Cast<Entity>()
                      .Where(e => IsTCHequipment(e))
                      .ForEach(e => objs.Add(Explode(e)));

                var pts = new List<Point3dEx>();
                objs.Cast<Entity>()
                    .ForEach(e => pts.Add(new Point3dEx((e as BlockReference).Position)));
                return pts;
            }
        }
        private bool IsTCHequipment(Entity entity)
        {
            string dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.StartsWith("TCH") && dxfName.Contains("EQUIPMENT");
        }
        private DBObject Explode(Entity entity)
        {
            var dbObjs = new DBObjectCollection();
            entity.Explode(dbObjs);
            return dbObjs[0];
        }
    }
}

