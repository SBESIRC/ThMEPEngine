using NFox.Cad;
using DotNetARX;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPElectrical.AFASRegion;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.FireAlarm.Interface;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace FireAlarm.Data
{
    public class ThFaPlaceCoverageExtractor : ThExtractorBase, IPrint, ITransformer,IGroup,ISetStorey
    {
        public List<Entity> CanLayoutAreas { get; set; }
        public ThMEPOriginTransformer Transformer
        {
            get
            {
                return transformer;
            }
            set
            {
                transformer = value;
            }
        }

        public ThFaPlaceCoverageExtractor()
        {
            CanLayoutAreas = new List<Entity>();
            Category = "PlaceCoverage";
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            CanLayoutAreas.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            var cmd = new AFASRegion();
            cmd.BufferDistance = 500;
            //获取可布置区域
            var poly = pts.CreatePolyline();
            CanLayoutAreas = cmd.DivideRoomWithPlacementRegion(poly);
            CanLayoutAreas.ForEach(e=>transformer.Transform(e)); //移动到原点，和之前所有的Extractor保持一致
        }

        public void Print(Database database)
        {
            CanLayoutAreas.CreateGroup(database, ColorIndex);
        }

        public void Reset()
        {
            Transformer.Reset(CanLayoutAreas.ToCollection());
        }

        public void Transform()
        {
            Transformer.Transform(CanLayoutAreas.ToCollection());
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
        }

        public void Set(List<ThStoreyInfo> storeyInfos)
        {            
        }

        public ThStoreyInfo Query(Entity entity)
        {
            return null;
        }
    }
}
