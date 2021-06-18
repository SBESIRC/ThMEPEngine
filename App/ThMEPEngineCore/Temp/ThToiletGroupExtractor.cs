using System;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThToiletGroupExtractor : ThExtractorBase, IExtract, IPrint, IBuildGeometry,IGroup
    {
        public List<Polyline> ToiletGroups { get; private set; }
        public Dictionary<Polyline, string> ToiletGroupId { get; private set; }
        private const string AlignmentVectorPropertyName = "AlignmentVector";
        private const string NeibourIdsPropertyName = "NeighborIds";
        public ThToiletGroupExtractor()
        {
            ToiletGroups = new List<Polyline>();
            ToiletGroupId = new Dictionary<Polyline, string>();
            Category = "Area";
            ElementLayer = "卫生间分组";
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            var instance = new ThExtractToiletGroupService()
            {
                ElementLayer= this.ElementLayer,
            };
            instance.Extract(database, pts);
            ToiletGroups = instance.ToiletGroups;
            ToiletGroups.ForEach(o => ToiletGroupId.Add(o, Guid.NewGuid().ToString()));
        }


        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            ToiletGroups.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(IdPropertyName, ToiletGroupId[o]);
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(AlignmentVectorPropertyName,new double[] { 1.000000, 0.000000 });
                geometry.Properties.Add(NeibourIdsPropertyName, new string[] { });
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }        

        public void Print(Database database)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var columnIds = new ObjectIdList();
                ToiletGroups.ForEach(o =>
                {
                    o.ColorIndex = ColorIndex;
                    o.SetDatabaseDefaults();
                    columnIds.Add(db.ModelSpace.Add(o));
                });
                if (columnIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), columnIds);
                }
            }
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
        }
    }
}
