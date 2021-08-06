﻿using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Engine;
using Linq2Acad;
using Dreambuild.AutoCAD;

namespace ThMEPEngineCore.Temp
{
    public class ThFaDoorOpeningExtractor : ThDoorOpeningExtractor, IBuildGeometry, ISetStorey,IExtract
    {
        private Dictionary<Entity, List<string>> FireDoorNeibourIds { get; set; }
        private List<StoreyInfo> StoreyInfos { get; set; }
        private const string NeibourFireApartIdsPropertyName = "NeibourFireApartIds";
        private const string UsePropertyName = "Useage";
        public ThFaDoorOpeningExtractor()
        {
            FireDoorNeibourIds = new Dictionary<Entity, List<string>>();
        }
        public new void Extract(Database database, Point3dCollection pts)
        {
            if (UseDb3Engine)
            {
                var doorEngine = new ThDB3DoorRecognitionEngine();
                doorEngine.Recognize(database, pts);
                Doors = doorEngine.Elements.Cast<ThIfcDoor>().ToList();
            }
            else
            {
                Doors = ExtractMsDoors(database, pts);
            }

            // Buffer 10
            var bufferService = new ThNTSBufferService();
            Doors.ForEach(o => o.Outline = bufferService.Buffer(o.Outline, 10));
        }
        private List<ThIfcDoor> ExtractMsDoors(Database database, Point3dCollection pts)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var results = new List<ThIfcDoor>();
                acadDatabase.ModelSpace
                    .OfType<Polyline>()
                    .Where(o => o.Layer == this.ElementLayer)
                    .ForEach(o =>
                    {
                        if (o.ColorIndex == 90)
                        {
                            results.Add(new ThIfcDoor() { Outline = o.Clone() as Polyline, Spec = "FM" });
                        }
                        else
                        {
                            results.Add(new ThIfcDoor() { Outline = o.Clone() as Polyline, Spec = "" });
                        }
                    });
                if (pts.Count >= 3)
                {
                    var objs = results.Select(o => o.Outline).ToCollection();
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                    objs = spatialIndex.SelectCrossingPolygon(pts);
                    results = results.Where(o => objs.Contains(o.Outline)).ToList();
                }
                return results;
            }
        }
        public new List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Doors.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                if (GroupSwitch)
                {
                    var parentId = BuildString(GroupOwner, o.Outline);
                    if (string.IsNullOrEmpty(parentId))
                    {
                        var storeyInfo = Query(o.Outline);
                        parentId = storeyInfo.Id;
                    }
                    geometry.Properties.Add(ParentIdPropertyName, parentId);
                }
                if(FireDoorNeibourIds.ContainsKey(o.Outline))
                {
                    geometry.Properties.Add(NeibourFireApartIdsPropertyName,string.Join(",", FireDoorNeibourIds[o.Outline]));
                }
                if(IsFireDoor(o))
                {
                    geometry.Properties.Add(UsePropertyName, "防火门");
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
                else if (neibours.Count > 2)
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

        public void Set(List<StoreyInfo> storeyInfos)
        {
            StoreyInfos = storeyInfos;
        }

        public StoreyInfo Query(Entity entity)
        {
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity)); ;
            return results.Count() > 0 ? results.First() : new StoreyInfo();
        }
    }
}
