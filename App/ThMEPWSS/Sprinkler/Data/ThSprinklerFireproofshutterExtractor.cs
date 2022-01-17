using NFox.Cad;
using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using ThMEPWSS.Sprinkler.Service;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPWSS.Sprinkler.Data
{
    public class ThSprinklerFireproofshutterExtractor : ThFireproofShutterExtractor, ITransformer
    {
        private Dictionary<Entity, List<string>> FireDoorNeibourIds { get; set; }
        private List<ThStoreyInfo> StoreyInfos { get; set; }

        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }

        public ThSprinklerFireproofshutterExtractor()
        {
            StoreyInfos = new List<ThStoreyInfo>();
            FireDoorNeibourIds = new Dictionary<Entity, List<string>>();
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            var localFireproofShutters = new DBObjectCollection();
            var instance = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            instance.Extract(database, pts);
            instance.Polys.ForEach(o => Transformer.Transform(o));
            localFireproofShutters = instance.Polys.ToCollection();

            for (int i = 0; i < localFireproofShutters.Count; i++)
            {
                if (!IsClosed(localFireproofShutters[i] as Polyline))
                {
                    var bufferService = new ThNTSBufferService();
                    localFireproofShutters[i] = bufferService.Buffer(localFireproofShutters[i] as Entity, 100) as Polyline;
                }
            }
            var clean = new ThSprinklerCleanEntityService();
            localFireproofShutters = localFireproofShutters.FilterSmallArea(SmallAreaTolerance)
                .Cast<Polyline>()
                .Select(o => clean.Clean(o))
                .Cast<Entity>()
                .ToCollection();
            //对Clean的结果进一步过虑
            localFireproofShutters = localFireproofShutters.FilterSmallArea(SmallAreaTolerance);
            FireproofShutter = localFireproofShutters.Cast<Polyline>().ToList();
        }

        public override List<ThGeometry> BuildGeometries()
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

        public ThStoreyInfo Query(Entity entity)
        {
            var results = StoreyInfos.Where(o => o.Boundary.EntityContains(entity));
            return results.Count() > 0 ? results.First() : new ThStoreyInfo();
        }

        public override List<Entity> GetEntities()
        {
            return FireproofShutter.Cast<Entity>().ToList();
        }

        private bool IsClosed(Polyline polyline)
        {
            if (polyline.StartPoint.DistanceTo(polyline.EndPoint) <= 1.0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Transform()
        {
            Transformer.Transform(FireproofShutter.ToCollection());
        }

        public void Reset()
        {
            Transformer.Reset(FireproofShutter.ToCollection());
        }
    }
}
