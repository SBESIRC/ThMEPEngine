using System;
using System.Linq;
using System.Collections.Generic;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPArchitecture.ParkingStallArrangement.Extractor
{
    internal class ThObstacleGeoJsonFactory : ThExtractorBase,IGroup
    {
        public DBObjectCollection Obstacles { get; private set; }
        public ThObstacleGeoJsonFactory(DBObjectCollection obstacles)
        {
            Obstacles = obstacles;
            Category = BuiltInCategory.Obstacle.ToString();
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var results = new List<ThGeometry>();
            Obstacles.OfType<Polyline>().ForEach(p =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, Guid.NewGuid().ToString());
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, BuildString(GroupOwner, p));
                geometry.Boundary = p;
                results.Add(geometry);
            });
            return results;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            throw new NotImplementedException();
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            var spatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(Obstacles);
            groupId.ForEach(o =>
            {
               spatialIndex
                .SelectWindowPolygon(o.Key)
                .OfType<Entity>()
                .ForEach(e=> GroupOwner.Add(e,new List<string>() { o.Value }));
            });
        }
    }
}
