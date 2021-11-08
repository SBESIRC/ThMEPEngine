using NFox.Cad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPElectrical.AFAS.Data
{
    public class ThFaEStoreyExtractor : ThEStoreyExtractor, IPrint, IGroup, ITransformer
    {
        public ThFaEStoreyExtractor()
        {
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Storeys.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, o.Id);
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.FloorTypePropertyName, o.StoreyType);
                geometry.Properties.Add(ThExtractorPropertyNameManager.FloorNumberPropertyName, o.StoreyNumber);
                geometry.Properties.Add(ThExtractorPropertyNameManager.BasePointPropertyName, o.BasePoint);
                geometry.Boundary = null;
                geos.Add(geometry);
            });
            return geos;
        }
        public void Group(Dictionary<Entity, string> groupId)
        {
            //
        }
        public void Transform()
        {
            Transformer.Transform(Storeys.Select(o => o.Boundary).ToCollection());
        }

        public void Reset()
        {
            Transformer.Reset(Storeys.Select(o => o.Boundary).ToCollection());
        }

        public new Dictionary<Entity, string> StoreyIds
        {
            get
            {
                var result = new Dictionary<Entity, string>();
                Storeys.ForEach(o => result.Add(o.Boundary, o.Id));
                return result;
            }
        }

        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }
    }
}
