using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.GeojsonExtractor.Service
{
    public class ThExtractTextService:ThExtractService
    {
        public List<Entity> Texts { get; set; }
        public TextType TextType { get; set; }
        public ThExtractTextService()
        {
            Texts = new List<Entity>();
            TextType = TextType.Both;
        }

        public override void Extract(Database db,Point3dCollection pts)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {                
                if(TextType == TextType.DBText || TextType == TextType.Both)
                {
                    Texts.AddRange(acadDatabase.ModelSpace
                    .OfType<DBText>()
                    .Where(o => IsElementLayer(o.Layer))
                    .Select(o => o.Clone() as DBText)
                    .ToList());
                }
                if (TextType == TextType.MText || TextType == TextType.Both)
                {
                    Texts.AddRange(acadDatabase.ModelSpace
                    .OfType<MText>()
                    .Where(o => IsElementLayer(o.Layer))
                    .Select(o => o.Clone() as MText)
                    .ToList());
                }
                if (pts.Count>=3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(Texts.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    Texts = objs.Cast<Entity>().ToList();
                }
            }
        }        
    }
    public enum TextType
    {
        DBText,
        MText,
        Both
    }
}
