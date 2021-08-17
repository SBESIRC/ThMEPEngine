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
using ThMEPElectrical.FireAlarm.Interface;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Algorithm;
using Dreambuild.AutoCAD;

namespace FireAlarm.Data
{
    public class ThFaDoorOpeningExtractor : ThExtractorBase,IPrint,IGroup, ISetStorey, ITransformer
    {
        private List<ThIfcRoom> Rooms { get; set; }
        public List<ThIfcDoor> Doors { get; private set; }
        private List<ThStoreyInfo> StoreyInfos { get; set; }
        private Dictionary<Entity, List<string>> FireDoorNeibourIds { get; set; }

        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }

        public ThFaDoorOpeningExtractor()
        {
            Doors = new List<ThIfcDoor>();
            StoreyInfos = new List<ThStoreyInfo>();
            Category = BuiltInCategory.DoorOpening.ToString();
            FireDoorNeibourIds = new Dictionary<Entity, List<string>>();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            var db3Doors = ExtractDb3Door(database, pts);
            var localDoors = ExtractMsDoor(database, pts);
            //对Clean的结果进一步过虑
            for (int i = 0; i < localDoors.Count; i++)
            {
                localDoors[i].Outline = ThCleanEntityService.Buffer(localDoors[i].Outline as Polyline, 25);
            }

            //处理重叠
            var conflictService = new ThHandleConflictService(
                db3Doors.Select(o => o.Outline).ToList(),
                localDoors.Select(o => o.Outline).ToList());
            conflictService.Handle();
            Doors.AddRange(db3Doors.Where(o => conflictService.Results.Contains(o.Outline)).ToList());
            var originDoorEntites = Doors.Select(o => o.Outline).ToList();
            Doors.AddRange(conflictService.Results
                .Where(o => !originDoorEntites.Contains(o))
                .Select(o=> new ThIfcDoor { Outline = o })
                .ToList());

            var objs = Doors.Select(o => o.Outline).ToCollection().FilterSmallArea(SmallAreaTolerance);
            Doors = Doors.Where(o => objs.Contains(o.Outline)).ToList();
        }
        private List<ThIfcDoor> ExtractDb3Door(Database database, Point3dCollection pts)
        {
            // 构件索引服务
            ThSpatialIndexCacheService.Instance.Add(new List<BuiltInCategory>
            {
                BuiltInCategory.ArchitectureWall,
                BuiltInCategory.Column,
                BuiltInCategory.CurtainWall,
                BuiltInCategory.ShearWall,
                BuiltInCategory.Window
            });
            ThSpatialIndexCacheService.Instance.Transformer = transformer;
            ThSpatialIndexCacheService.Instance.Build(database, pts);

            var doorExtraction = new ThDB3DoorExtractionEngine();
            doorExtraction.Extract(database);
            doorExtraction.Results.ForEach(o =>
            {
                if (o is ThRawDoorMark doorMark)
                {
                    Transformer.Transform(doorMark.Data as Entity);
                }
                Transformer.Transform(o.Geometry);
            });

            var doorEngine = new ThDB3DoorRecognitionEngine();
            var newPts = new Point3dCollection();
            pts.Cast<Point3d>().ForEach(p =>
            {
                var pt = new Point3d(p.X, p.Y, p.Z);
                Transformer.Transform(ref pt);
                newPts.Add(pt);
            });
            doorEngine.Recognize(doorExtraction.Results, newPts);
            var db3Doors = doorEngine.Elements.Cast<ThIfcDoor>().ToList();
            return db3Doors;
        }
        private List<ThIfcDoor> ExtractMsDoor(Database database, Point3dCollection pts)
        {
            var localdoors = new List<ThIfcDoor>();
            var instance = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            instance.Extract(database, pts);
            instance.Polys.ForEach(o => Transformer.Transform(o));
            localdoors = instance.Polys
                .Where(o => o.Area >= SmallAreaTolerance)
                .Select(o => new ThIfcDoor { Outline=o})
                .Cast<ThIfcDoor>()
                .ToList();
            return localdoors;
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

        public void Transform()
        {
            Doors.ForEach(o => Transformer.Transform(o.Outline));
        }

        public void Reset()
        {
            Doors.ForEach(o => Transformer.Reset(o.Outline));
        }
    }
}
