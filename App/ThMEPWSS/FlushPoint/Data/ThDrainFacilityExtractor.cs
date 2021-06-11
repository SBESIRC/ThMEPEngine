using DotNetARX;
using System.Linq;
using ThMEPWSS.Engine;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPWSS.FlushPoint.Data
{
    public class ThDrainFacilityExtractor : ThExtractorBase, IPrint
    {
        /// <summary>
        /// 集水井
        /// </summary>
        public List<Entity> CollectingWells { get; private set; }
        /// <summary>
        /// 排水沟
        /// </summary>
        public List<Entity> DrainageDitches { get; private set; }

        /// <summary>
        /// 地漏
        /// </summary>
        public List<Entity> FloorDrains { get; private set; }

        public ThDrainFacilityExtractor()
        {
            Category = BuiltInCategory.DrainageFacility.ToString();
            CollectingWells = new List<Entity>();
            DrainageDitches = new List<Entity>();
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var results = new List<ThGeometry>();
            results.AddRange(BuildCollectWellGeos());
            results.AddRange(BuildDrainageDitchGeos());
            results.AddRange(BuildFloorDrains());
            return results;
        }

        private List<ThGeometry> BuildCollectWellGeos()
        {
            var geos = new List<ThGeometry>();
            CollectingWells.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "CollectingWell"); //集水井
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        private List<ThGeometry> BuildDrainageDitchGeos()
        {
            var geos = new List<ThGeometry>();
            DrainageDitches.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "DrainageDitch"); //排水沟
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        private List<ThGeometry> BuildFloorDrains()
        {
            var geos = new List<ThGeometry>();
            FloorDrains.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "FloorDrain"); //地漏
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            using (var curveEngine = new ThDrainageWellRecognitionEngine())
            using (var blockEngine = new ThDrainageWellBlockRecognitionEngine())
            {
                curveEngine.Recognize(database, pts);
                blockEngine.Recognize(database, pts);

                var objs = new DBObjectCollection();
                curveEngine.Geos.ForEach(o => objs.Add(o));
                blockEngine.Geos.Cast<BlockReference>().ForEach(o =>
                {
                    ThDrawTool.Explode(o)
                        .Cast<Entity>()
                        .Where(p => p is Line || p is Polyline)
                        .ForEach(p => objs.Add(p));
                });

                var breakService = new ThBreakDrainageFacilityService();
                breakService.Break(objs);


                CollectingWells = breakService.CollectingWells;
                DrainageDitches = breakService.DrainageDitches;
            }

            // 地漏
            using (var floorDrainEngine = new ThFloorDrainRecognitionEngine())
            {
                floorDrainEngine.FilterSwitch = true;
                floorDrainEngine.OffsetDis = 300.0;
                floorDrainEngine.Recognize(database, pts);
                FloorDrains = floorDrainEngine.Elements.Select(o => o.Outline).ToList();
            }
        }

        public void Print(Database database)
        {
            CollectingWells.CreateGroup(database, ColorIndex);
            DrainageDitches.CreateGroup(database, ColorIndex);
            FloorDrains.CreateGroup(database, ColorIndex);
        }
    }
}
