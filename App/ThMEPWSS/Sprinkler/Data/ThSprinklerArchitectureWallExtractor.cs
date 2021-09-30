using NFox.Cad;
using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPWSS.Sprinkler.Data
{
    public class ThSprinklerArchitectureWallExtractor : ThArchitectureExtractor, ITransformer
    {
        private List<ThStoreyInfo> StoreyInfos { get; set; }
        /// <summary>
        /// 从图纸中获取的原始建筑墙元素
        /// 已经移动到原点处
        /// </summary>
        public List<ThRawIfcBuildingElementData> Db3ExtractResults { get; set; }

        public ThSprinklerArchitectureWallExtractor()
        {
            StoreyInfos = new List<ThStoreyInfo>();
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            //提取,并移动到原点
            var db3Walls = ExtractDb3Wall(pts);
            Walls = db3Walls.Cast<Entity>().ToList();
        }

        private DBObjectCollection ExtractDb3Wall(Point3dCollection pts)
        {
            //提取了DB3中的墙，并移动到原点
            var newPts = Transformer.Transform(pts);
            var wallEngine = new ThDB3ArchWallRecognitionEngine();
            wallEngine.Recognize(Db3ExtractResults, newPts);
            return wallEngine.Elements.Select(o => o.Outline).ToCollection();
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Walls.ForEach(o =>
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
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public ThStoreyInfo Query(Entity entity)
        {
            //ToDo
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity));
            return results.Count() > 0 ? results.First() : new ThStoreyInfo();
        }

        public void Transform()
        {
            transformer.Transform(Walls.ToCollection());
        }

        public void Reset()
        {
            Transformer.Reset(Walls.ToCollection());
        }
        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }
    }
}
