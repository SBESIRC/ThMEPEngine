using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;

using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertDuctCenterLineExtractionEngine
    {
        public ThBConvertDuctCenterLineExtractionEngine()
        {
            Results = new List<Curve>();
        }

        public List<Curve> Results { get; set; }

        public List<string> CenterLineLayer = new List<string>
        {
            "H-FIRE-DUCT-MID",
            "H-DUCT-FIRE-MID",
            "H-DUAL-DUCT-MID",
            "H-DUCT-DUAL-MID",
        };

        public void Extract(Database database, Point3dCollection frame)
        {
            var ductEngine = new ThTCHDuctRecognitionEngine();
            ductEngine.Recognize(database, frame);
            ductEngine.Elements.OfType<ThIfcDuctSegment>().ForEach(o =>
            {
                Results.Add(o.Outline as Curve);
            });
            Results.AddRange(AIDuctCenterLineExtract(database));
        }

        private List<Curve> AIDuctCenterLineExtract(Database database)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var results = new List<Curve>();
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is BlockReference blkRef)
                    {
                        if (blkRef.BlockTableRecord.IsNull)
                        {
                            continue;
                        }
                        var blockTableRecord = acadDatabase.Blocks.Element(blkRef.BlockTableRecord);
                        if (IsBuildElementBlock(blockTableRecord))
                        {
                            var data = new ThBlockReferenceData(blkRef.ObjectId);
                            var objs = data.VisibleEntities();
                            if (objs.Count == 0)
                            {
                                foreach (var objId in blockTableRecord)
                                {
                                    objs.Add(objId);
                                }
                            }
                            foreach (ObjectId objId in objs)
                            {
                                var dbObj = acadDatabase.Element<Entity>(objId);
                                if (dbObj is Curve curve && CenterLineLayer.Contains(ThMEPXRefService.OriginalFromXref(dbObj.Layer)))
                                {
                                    results.Add(curve.Clone() as Curve);
                                }
                            }
                        }
                    }
                }
                return results;
            }
        }

        public bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 忽略动态块
            if (blockTableRecord.IsDynamicBlock)
            {
                return false;
            }

            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return false;
            }

            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable)
            {
                return false;
            }

            return true;
        }
    }
}
