using System;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.FireAlarm.Interfacce;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace FireAlarm.Data
{
    public class ThFaDoorOpeningExtractor : ThExtractorBase,IPrint,IGroup, ISetStorey
    {
        private List<ThIfcRoom> Rooms { get; set; }
        public List<ThIfcDoor> Doors { get; private set; }
        private List<ThStoreyInfo> StoreyInfos { get; set; }
        private Dictionary<Entity, List<string>> FireDoorNeibourIds { get; set; }       
        public ThFaDoorOpeningExtractor()
        {
            Doors = new List<ThIfcDoor>();
            StoreyInfos = new List<ThStoreyInfo>();
            Category = BuiltInCategory.Door.ToString();
            FireDoorNeibourIds = new Dictionary<Entity, List<string>>();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            //From DB3
            var db3doors = new List<ThIfcDoor>();
            using (var doorEngine = new ThDB3DoorRecognitionEngine())
            {
                doorEngine.Recognize(database, pts);
                db3doors = doorEngine.Elements.Cast<ThIfcDoor>().ToList();
            }
            //From Local
            var localdoors = new List<ThIfcDoor>();
            var instance = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            instance.Extract(database, pts);
            localdoors = instance.Polys
                .Where(o => o.Area >= SmallAreaTolerance)
                .Select(o => new ThIfcDoor() { Outline = o }).ToList()
                .ToList();
            //对Clean的结果进一步过虑
            localdoors.ForEach(o => o.Outline = ThCleanEntityService.Buffer(o.Outline as Polyline, 25));

            //处理重叠
            var conflictService = new ThHandleConflictService(
                db3doors.Select(o => o.Outline).ToList(),
                localdoors.Select(o => o.Outline).ToList());
            conflictService.Handle();
            Doors.AddRange(db3doors.Where(o => conflictService.Results.Contains(o.Outline)).ToList());
            var originDoorEntites = Doors.Select(o => o.Outline).ToList();
            Doors.AddRange(conflictService.Results
                .Where(o => !originDoorEntites.Contains(o))
                .Select(o=> new ThIfcDoor { Outline = o })
                .ToList());
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            var exteriorDoorService = new ThIsExteriorDoorService(this.Rooms.Select(o=>o.Boundary).ToList());

            Doors.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                var parentId = BuildString(GroupOwner, o.Outline);
                if (string.IsNullOrEmpty(parentId))
                {
                    var storeyInfo = Query(o.Outline);
                    parentId = storeyInfo.Id;
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, parentId);
                if (FireDoorNeibourIds.ContainsKey(o.Outline))
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.NeibourFireApartIdsPropertyName, string.Join(",", FireDoorNeibourIds[o.Outline]));
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.UseagePropertyName, IsFireDoor(o) ? "防火门" : "");
                if(exteriorDoorService.IsExteriorDoor(o.Outline as Polyline))
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.TagPropertyName, "外门");
                }
                else
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.TagPropertyName, "");
                }
                geometry.Boundary = o.Outline;
                geos.Add(geometry);
            });
            return geos;
        }
        public void SetTags(Dictionary<Entity, string> fireApartIds)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(fireApartIds.Select(o=>o.Key).ToCollection());
            var bufferService = new ThNTSBufferService();
            var fireDoors = Doors.Where(o => IsFireDoor(o)).ToList();
            fireDoors.ForEach(o =>
            {
                var enlarge = bufferService.Buffer(o.Outline, 5.0);
                var neibours = spatialIndex.SelectCrossingPolygon(enlarge);
                if (neibours.Count==2)
                {
                    FireDoorNeibourIds.Add(o.Outline,neibours.Cast<Entity>().Select(e => fireApartIds[e]).ToList());
                }
                else if(neibours.Count>2)
                {
                    throw new NotSupportedException();
                }
            });
        }
        private bool IsFireDoor(ThIfcDoor door)
        {
            //ToDO
            return door.Spec.ToUpper().Contains("FM");
        }

        public void Set(List<ThStoreyInfo> storeyInfos)
        {
            StoreyInfos = storeyInfos;
        }

        public ThStoreyInfo Query(Entity entity)
        {
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity)); ;
            return results.Count() > 0 ? results.First() : new ThStoreyInfo();
        }

        public void Print(Database database)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var doorIds = new ObjectIdList();
                Doors.ForEach(o =>
                {
                    o.Outline.ColorIndex = ColorIndex;
                    o.Outline.SetDatabaseDefaults();
                    doorIds.Add(db.ModelSpace.Add(o.Outline));
                });
                if (doorIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), doorIds);
                }
            }
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            Doors.ForEach(o => GroupOwner.Add(o.Outline, FindCurveGroupIds(groupId, o.Outline)));
        }

        public override List<Entity> GetEntities()
        {
            return Doors.Select(o =>o.Outline).ToList();
        }
        public override void SetRooms(List<ThIfcRoom> rooms)
        {
            this.Rooms = rooms;
        }
    }
}
