using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractLevelMarkService : ThExtractService,IElevationQuery
    {
        public List<Entity> LevelMarks { get; set; }
        public ThExtractLevelMarkService()
        {
            LevelMarks = new List<Entity>();
        }
        public override void Extract(Database db, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(db))
            {
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if(!IsValidType(ent))
                    {
                        continue;
                    }
                    if(ent is BlockReference bkr)
                    {
                        if (IsElementLayer(bkr.Layer) && IsValidBlockName(bkr.GetEffectiveName()))
                        {
                            LevelMarks.Add(bkr);
                        }
                    }
                }
                if(pts.Count>=3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(LevelMarks.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    LevelMarks = objs.Cast<Entity>().ToList();
                }                
            }
        }

        public override bool IsElementLayer(string layerName)
        {
            
            string[] patterns = ThStructureUtils.OriginalFromXref(layerName).ToUpper().Split('-').Reverse().ToArray();
            if (patterns.Count() < 3)
            {
                return false;
            }
            return (patterns[0] == "HIGH") && (patterns[1] == "LEVL") && (patterns[2] == "AD");
        }
        private bool IsValidBlockName(string blkName)
        {
            return blkName.ToUpper().Contains("A-SIGNHIGH");//侧入式雨水斗
        }

        public List<double> Query(Entity ent)
        {
            var results = new List<double>();
            var polygon = ent.ToNTSPolygonalGeometry();
            var fitlter = LevelMarks
                 .Cast<BlockReference>()
                 .Where(o => polygon.Contains(new DBPoint(o.Position)
                 .ToNTSPoint()))
                 .ToList();
            foreach(var br in fitlter)
            {
                foreach(var ar in br.GetAttributeReferences())
                {
                    //
                    if(ar.Tag=="标高")
                    {
                        double elevation = 0.0;
                        if(double.TryParse(ar.TextString,out elevation))
                        {
                            results.Add(elevation);
                        }
                    }
                }
            }
            return results.Distinct().ToList();
        }
    }
}
