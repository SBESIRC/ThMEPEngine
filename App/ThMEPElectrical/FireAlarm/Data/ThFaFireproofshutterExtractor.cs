using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPElectrical.FireAlarm.Interfacce;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;

namespace FireAlarm.Data
{
    public class ThFaFireproofshutterExtractor : ThFireproofShutterExtractor, ISetStorey
    {
        private Dictionary<Entity, List<string>> FireDoorNeibourIds { get; set; }
        private List<ThStoreyInfo> StoreyInfos { get; set; }

        public ThFaFireproofshutterExtractor()
        {
            StoreyInfos = new List<ThStoreyInfo>();
            FireDoorNeibourIds = new Dictionary<Entity, List<string>>();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            var extractService = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            extractService.Extract(database, pts);
            ThCleanEntityService clean = new ThCleanEntityService();
            FireproofShutter = extractService.Polys
                .Where(o => o.Area >= SmallAreaTolerance)
                .Select(o => clean.Clean(o))
                .Cast<Polyline>()
                .ToList();
            //对Clean的结果进一步过虑
            FireproofShutter = FireproofShutter.ToCollection().FilterSmallArea(1.0).Cast<Polyline>().ToList();

            FireproofShutter.ForEach(o =>
            {
                if(!IsClosed(o))
                {
                    var bufferService = new ThNTSBufferService();
                    o = bufferService.Buffer(o, 100) as Polyline;
                }
            });
        }
        public new List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            FireproofShutter.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                var parentId = BuildString(GroupOwner, o);
                if (string.IsNullOrEmpty(parentId))
                {
                    var storeyInfo = Query(o);
                    parentId = storeyInfo.Id;
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, parentId);
                if (FireDoorNeibourIds.ContainsKey(o))
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.NeibourFireApartIdsPropertyName, string.Join(",", FireDoorNeibourIds[o]));
                }
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public void SetTags(Dictionary<Entity, string> fireApartIds)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(fireApartIds.Select(o => o.Key).ToCollection());
            var bufferService = new ThNTSBufferService();
            FireproofShutter.ForEach(o =>
            {
                var enlarge = bufferService.Buffer(o, 5.0);
                var neibours = spatialIndex.SelectCrossingPolygon(enlarge);
                if (neibours.Count == 2)
                {
                    FireDoorNeibourIds.Add(o, neibours.Cast<Entity>().Select(e => fireApartIds[e]).ToList());
                }
                else if (neibours.Count > 2)
                {
                    throw new NotSupportedException();
                }
            });
        }

        public void Set(List<ThStoreyInfo> storeyInfos)
        {
            StoreyInfos = storeyInfos;
        }

        public ThStoreyInfo Query(Entity entity)
        {
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity));
            return results.Count() > 0 ? results.First() : new ThStoreyInfo();
        }
        public override List<Entity> GetEntities()
        {
            return FireproofShutter.Cast<Entity>().ToList();
        }

        private Polyline BufferSingle(Polyline polyline)
        {
            return polyline.BufferPL(100).Cast<Polyline>().OrderByDescending(o=>o.Area).First();
        }
        private bool IsClosed(Polyline polyline)
        {
            var newPoly = ThCleanEntityService.MakeValid(polyline);
            if(newPoly.StartPoint.DistanceTo(newPoly.EndPoint)<=1.0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
