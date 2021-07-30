using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPElectrical.GroundingGrid.Data
{
    /// <summary>
    /// 防雷引下线
    /// </summary>
    public class ThDownConductorExtractor : ThExtractorBase,IGroup,IPrint
    {
        public List<DBPoint> FromUpToHereConductors { get; private set; } //从上至此

        public List<DBPoint> FromHereToDownConductors { get; private set; } //从此往下
        public ThDownConductorExtractor()
        {
            Category = BuiltInCategory.Conductor.ToString();
            FromUpToHereConductors = new List<DBPoint>();
            FromHereToDownConductors = new List<DBPoint>();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            FromUpToHereConductors.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "防雷引下线");
                geometry.Properties.Add(ThExtractorPropertyNameManager.ClassPropertyName, "A"); //A类表示从上至此,从上面引至本层
                geometry.Boundary = o;
                geos.Add(geometry);
            });

            FromHereToDownConductors.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "防雷引下线");
                geometry.Properties.Add(ThExtractorPropertyNameManager.ClassPropertyName, "B"); //B类表示从此往下,从本层引下去
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            var blk1Service = new ThExtractBlockReferenceService()
            {
                BlockName = "E-BGND34", // 防雷，E-GND34，从上至此
            };
            blk1Service.Extract(database, pts);
            FromUpToHereConductors = blk1Service.Blocks.Select(b => new DBPoint(b.Position)).ToList();

            var blk2Service = new ThExtractBlockReferenceService()
            {
                BlockName = "E-BGND33", // 防雷，E-GND34，从此往下
            };

            blk2Service.Extract(database, pts);
            FromHereToDownConductors = blk2Service.Blocks.Select(b => new DBPoint(b.Position)).ToList();
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            FromUpToHereConductors.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            FromHereToDownConductors.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
        }

        public void Print(Database database)
        {
            FromUpToHereConductors
                .Select(o => new Circle(o.Position, Vector3d.ZAxis, 50))
                .Cast<Entity>()
                .ToList()
                .CreateGroup(database, ColorIndex);
            FromHereToDownConductors
                .Select(o => new Circle(o.Position, Vector3d.ZAxis, 50))
                .Cast<Entity>()
                .ToList()
                .CreateGroup(database, ColorIndex);
        }
    }
}
