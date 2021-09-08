using DotNetARX;
using System.Linq;
using ThCADExtension;
using ThMEPWSS.Engine;
using ThMEPEngineCore.IO;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
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
        public List<string> DrainageBlkNames { get; set; }
        public List<string> FloorDrainBlkNames { get; set; }

        public ThDrainFacilityExtractor()
        {
            DrainageBlkNames = new List<string>();
            CollectingWells = new List<Entity>();
            DrainageDitches = new List<Entity>();
            FloorDrainBlkNames = new List<string>();
            Category = BuiltInCategory.DrainageFacility.ToString();
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
            var objs = new DBObjectCollection();

            //根据图层规则找到指定的Polyline,Line
            using (var curveEngine = new ThDrainageWellRecognitionEngine())
            {
                curveEngine.Recognize(database, pts);
                curveEngine.Geos.ForEach(o => objs.Add(o));
            }

            //根据图层规则,找到在图层的块
            using (var blockEngine = new ThDrainageWellBlockRecognitionEngine())
            {
                blockEngine.Visitor.CheckQualifiedBlockName = (Entity e) => true;
                blockEngine.Recognize(database, pts);
                blockEngine.RecognizeMS(database, pts);
                blockEngine.Geos.Cast<BlockReference>().ForEach(o =>
                {
                    ThDrawTool.Explode(o)
                        .Cast<Entity>()
                        .Where(p => p is Line || p is Polyline)
                        .ForEach(p => objs.Add(p));
                });                
            }

            //只看图块
            using (var blockEngine = new ThDrainageWellBlockRecognitionEngine())
            {
                blockEngine.Visitor.CheckQualifiedBlockName = CheckBlockNameQualified;
                blockEngine.Visitor.CheckQualifiedLayer = (Entity e) => true;
                blockEngine.Recognize(database, pts);
                blockEngine.RecognizeMS(database, pts);
                blockEngine.Geos.Cast<BlockReference>().ForEach(o =>
                {
                    ThDrawTool.Explode(o)
                        .Cast<Entity>()
                        .Where(p => p is Line || p is Polyline)
                        .ForEach(p => objs.Add(p));
                });
            }

            // 创建集水井和地沟
            Build(objs, pts);
            
            // 获取地漏
            if(FloorDrainBlkNames.Count>0)
            {
                using (var floorDrainEngine = new ThFloorDrainRecognitionEngine())
                {
                    floorDrainEngine.BlkNames = FloorDrainBlkNames.ToHashSet();
                    floorDrainEngine.FilterSwitch = true;
                    floorDrainEngine.OffsetDis = 300.0;
                    floorDrainEngine.Recognize(database, pts);
                    floorDrainEngine.RecognizeMS(database, pts);
                    FloorDrains = floorDrainEngine.Elements.Select(o => o.Outline).ToList();
                }
            } 
        }
        private void Build(DBObjectCollection objs, Point3dCollection pts)
        {
            var center = pts.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            transformer.Transform(objs);
            var breakService = new ThBreakDrainageFacilityService();
            breakService.Break(objs);
            CollectingWells = breakService.CollectingWells;
            DrainageDitches = breakService.DrainageDitches;
            CollectingWells.ForEach(c => transformer.Reset(c));
            DrainageDitches.ForEach(d => transformer.Reset(d));
        }
        private bool CheckBlockNameQualified(Entity entity)
        {
            if (entity is BlockReference br)
            {
                string name = br.GetEffectiveName().ToUpper();
                return DrainageBlkNames.Where(o => name.Contains(o.ToUpper())).Any();
            }
            return false;
        }
        public void Print(Database database)
        {
            CollectingWells.CreateGroup(database, ColorIndex);
            DrainageDitches.CreateGroup(database, ColorIndex);
            FloorDrains.CreateGroup(database, ColorIndex);
        }
    }
}
