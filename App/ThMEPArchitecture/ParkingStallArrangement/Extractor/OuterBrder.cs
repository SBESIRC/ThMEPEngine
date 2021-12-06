﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;

namespace ThMEPArchitecture.ParkingStallArrangement.Extractor
{
    public class OuterBrder
    {
        public DBObjectCollection OuterLines = new DBObjectCollection();//外框线
        public DBObjectCollection BuildingLines = new DBObjectCollection();//建筑物框线
        public DBObjectCollection EquipmentLines = new DBObjectCollection();//机房设备线
        public DBObjectCollection SegmentLines = new DBObjectCollection();//分割线
        public List<Line> SegLines = new List<Line>();//分割线
        public List<Polyline> BuildLines = new List<Polyline>();
        public void Extract(Database database, Point3dCollection polygon)
        {
            var objs = new DBObjectCollection();
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => o is BlockReference || IsCurve(o));
                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);

                dbObjs.Cast<Entity>()
                      .ForEach(e => Explode(e));

                foreach(var db in SegmentLines)
                {
                    var spt = (db as Polyline).StartPoint;
                    var ept = (db as Polyline).EndPoint;
                    SegLines.Add(new Line(spt, ept));
                }
                BuildingLines.Cast<Entity>()
                    .ForEach(e => BuildLines.Add((e as Polyline).DPSimplify(10.0)));
                foreach(var l in BuildLines)
                {
                    acadDatabase.CurrentSpace.Add(l);
                }
            }
        }

        private bool IsOuterLayer(string layer)
        {
            return layer.ToUpper() == "0" ||
                   layer.ToUpper() == "地库边界";
        }
        private bool IsBuildingLayer(string layer)
        {
            return layer.ToUpper() == "0" ||
                   layer.ToUpper() == "障碍物边缘";
        }
        private bool IsEquipmentLayer(string layer)
        {
            return layer.ToUpper() == "0" ||
                   layer.ToUpper() == "机房";
        }
        private bool IsSegLayer(string layer)
        {
            return layer.ToUpper() == "分割线";
        }

        private void Explode(Entity entity)
        {
            if(entity is BlockReference)
            {
                var dbObjs = new DBObjectCollection();
                entity.Explode(dbObjs);
                foreach (var obj in dbObjs)
                {
                    var ent = obj as Entity;
                    AddObjs(ent);
                }
            }
            if(IsCurve(entity))
            {
                AddObjs(entity);
            }
        }
        private void AddObjs(Entity ent)
        {
            if (IsOuterLayer(ent.Layer))
            {
                AddObjs(ent, OuterLines);
            }
            if (IsBuildingLayer(ent.Layer))
            {
                AddObjs(ent, BuildingLines);
            }
            if (IsEquipmentLayer(ent.Layer))
            {
                AddObjs(ent, EquipmentLines);
            }
            if (IsSegLayer(ent.Layer))
            {
                AddObjs(ent, SegmentLines);
            }
        }
        private void AddObjs(Entity entity, DBObjectCollection dbObjs)
        {
            if(entity is Polyline)
            {
                dbObjs.Add(entity);
            }
            if(entity is Line line)
            {
                dbObjs.Add(line.ToPolyline());
            }
        }
        private bool IsCurve(Entity ent)
        {
            return ent is Polyline ||
                   ent is Line;
        }
    }
}
