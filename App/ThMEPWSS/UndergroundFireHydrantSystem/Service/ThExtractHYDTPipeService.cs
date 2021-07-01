using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NetTopologySuite.Geometries;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThUtilExtensionsNs;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public class ThExtractHYDTPipeService//提取管道
    {
        public IEnumerable<Curve> Results { get; private set; }
        public ThExtractHYDTPipeService()
        {
        }

        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var lines = ThDrainageSystemServiceGeoCollector.GetLines(acadDatabase.ModelSpace.OfType<Entity>().ToList(), layer => layer is "W-FRPT-1-HYDT-PIPE" or "W-FRPT-HYDT-PIPE");
                return GeoFac.CreateIntersectsSelector(lines.Select(x => x.ToLineString()).ToList())(polygon.ToRect().ToPolygon()).SelectMany(x => x.ToDbCollection().OfType<DBObject>()).ToCollection();
            }
        }
        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-FRPT-1-HYDT-PIPE" || layer.ToUpper() == "W-FRPT-HYDT-PIPE";
        }
        private bool IsHYDTPipeElement(Curve curve)
        {
            return curve is Polyline || curve is Line;
        }
    }


    public class ThExtractValveService//提取阀门
    {
        public IEnumerable<Entity> Results { get; private set; }
        public bool IsBkReference { get; private set; }

        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsHYDTPipeLayer(o.Layer));

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                string objName = "";

                for (int i = 0; i < dbObjs.Count; i++)
                {
                    var db = dbObjs[i];
                    if (db is BlockReference)
                    {
                        objName = (db as BlockReference).GetEffectiveName();
                        IsBkReference = true;
                    }
                    else
                    {
                        IsBkReference = false;
                        var ad = (db as Entity).AcadObject;
                        dynamic o = ad;
                        objName = o.ObjectName;

                    }
                    if (!IsValve(objName))
                    {
                        dbObjs.RemoveAt(i);
                        i -= 1;
                    }
                }


                ;
                foreach (var v in dbObjs)
                {
                    var ad = (v as Entity).AcadObject;
                    dynamic o = ad;
                    objName = o.ObjectName;
                    if (!objName.ToUpper().Contains("VALVE"))
                    {
                        ;
                    }
                    ;

                }
                return dbObjs;
            }
        }
        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-FRPT-HYDT-EQPM";
        }

        private bool IsValve(string valve)
        {
            return valve.ToUpper() == "蝶阀" || valve.ToUpper().Contains("VALVE");
        }
    }

    public class ThExtractHydrant//提取管道末端标记
    {
        public IEnumerable<BlockReference> Results { get; private set; }
        public IEnumerable<Circle> Results1 { get; private set; }
        public DBObjectCollection DBObjs { get; private set; }
        public DBObjectCollection DBObjs1 { get; private set; }
        public List<Point3dEx> HydrantPosition { get; private set; }
        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase.ModelSpace
                   .OfType<BlockReference>()
                   .Where(o => o.Layer.ToUpper() == "W-FRPT-HYDT-EQPM" && IsValve(o.GetEffectiveName()));

                Results1 = acadDatabase.ModelSpace
                   .OfType<Circle>()
                   .Where(o => o.Layer.ToUpper() == "W-FRPT-EXTG");

                var rects = new List<Polyline>();
                Results1.ToList().ForEach(o => rects.Add(o.ToRectangle()));

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                DBObjs = spatialIndex.SelectCrossingPolygon(polygon);

                var spatialIndex1 = new ThCADCoreNTSSpatialIndex(rects.ToCollection());
                DBObjs1 = spatialIndex1.SelectCrossingPolygon(polygon);

                if (DBObjs.Count != 0)
                {

                    return DBObjs;
                }
                else
                {

                    return DBObjs1;
                }

            }
        }


        private bool IsValve(string valve)
        {
            return valve.ToUpper() == "A$C4BDB4B3D";
        }


        public List<Point3dEx> CreatePointList()
        {
            HydrantPosition = new List<Point3dEx>();
            var pt = new Point3dEx(new Point3d());
            if (DBObjs.Count != 0)
            {
                foreach (var db in DBObjs)
                {
                    var br = db as BlockReference;
                    pt = new Point3dEx(br.Position);
                    HydrantPosition.Add(pt);
                }
            }
            else
            {
                foreach (var db in DBObjs1)
                {
                    var br = db as Polyline;
                    pt = new Point3dEx(br.StartPoint);
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
                   .Where(o => IsHYDTPipeLayer(o.Layer) && IsValve(o.GetEffectiveName()));
                ;
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
                   .Where(o => IsHYDTPipeLayer(o.Layer) && IsValve(o.GetEffectiveName()));

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
            return valve.ToUpper() == "消火栓环管节点标记";
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


    public class ThExtractLabelLine
    {
        public IEnumerable<Curve> Results { get; private set; }
        public DBObjectCollection DBObjs { get; private set; }
        public List<Line> LabelPosition { get; private set; }

        public ThExtractLabelLine()
        {
        }

        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase
                   .ModelSpace
                   .OfType<Curve>()
                   .Where(o => IsHYDTPipeElement(o) && IsHYDTPipeLayer(o.Layer));

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                DBObjs = spatialIndex.SelectCrossingPolygon(polygon);
                return DBObjs;
            }
        }
        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-RAIN-DIMS" ||
                   layer.ToUpper() == "W-DRAI-DIMS" ||
                   layer.ToUpper() == "W-WSUP-DIMS" ||
                   layer.ToUpper() == "W-FRPT-HYDT-DIMS";
        }
        private bool IsHYDTPipeElement(Curve curve)
        {
            return curve is Polyline || curve is Line;
        }

        public List<Line> CreateLabelLineList()
        {
            LabelPosition = new List<Line>();
            foreach (var db in DBObjs)
            {
                if(db is Polyline)
                {
                    var fl = db as Polyline;
                    var ptPre = fl.GetPoint3dAt(0);
                    for (int i = 1; i < fl.NumberOfVertices; i++)
                    {
                        var pti = fl.GetPoint3dAt(i);
                        var pt1 = new Point3dEx(ptPre.X, ptPre.Y, 0);
                        var pt2 = new Point3dEx(pti.X, pti.Y, 0);
                        
                        LabelPosition.Add(new Line(pt1._pt, pt2._pt));
                        ptPre = fl.GetPoint3dAt(i);
                    }
                    
                }
                else
                {
                    var line = db as Line;
                    var br = db as BlockReference;

                    LabelPosition.Add(new Line(line.StartPoint, line.EndPoint));
                }
                
            }
            return LabelPosition;
        }
    }



    public class ThExtractLabelText//文字提取
    {
        public List<DBText> Results { get; private set; }
        public DBObjectCollection DBObjs { get; private set; }

        public ThExtractLabelText()
        {
            Results = new List<DBText>();
        }

        public void Extract(Database database)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase
                   .ModelSpace
                   .OfType<DBText>()
                   .Where(o => IsHYDTPipeLayer(o.Layer)).ToList(); 
            }
        }
        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-RAIN-DIMS" || layer.ToUpper() == "W-FRPT-HYDT-DIMS" || layer.ToUpper() == "W-WSUP-DIMS";
        }
    }


    public class ThExtractFireHydrant//室内消火栓平面
    {
        public List<BlockReference> Results { get; private set; }
        public DBObjectCollection DBobj { get; private set; }

        public ThExtractFireHydrant()
        {

        }
        public void Extract(Database database)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase
                   .ModelSpace
                   .OfType<BlockReference>()
                   .Where(o => IsHYDTPipeLayer(o.Layer) && IsFireHydrant(o.GetEffectiveName()))
                   .ToList();

            }
        }
        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-FRPT-HYDT";
        }

        private bool IsFireHydrant(string valve)
        {
            return valve.ToUpper() == "室内消火栓平面";
        }
    }




}
