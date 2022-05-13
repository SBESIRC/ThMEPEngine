using NFox.Cad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
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
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
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
        //圆弧打散的长度
        private double ArcTesslateLength = 50.0;

        public ThDrainFacilityExtractor()
        {
            FloorDrains = new List<Entity>();            
            CollectingWells = new List<Entity>();
            DrainageDitches = new List<Entity>();
            DrainageBlkNames = new List<string>();
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
            //目前通过识别和分离算法获取排水沟和集水井的算法不稳
            //2022.05.11 杨工和设计师沟通先取消掉这种方式
            //RecognizeCollectWellByCurve(database, pts);
            //通过块配置的方式，获取集水井的块并获取对应块的OBB
            RecognizeCollectWellByBlock(database, pts);
            RecognizeFloorDrainByBlock(database, pts);
            DuplicatedRemove(); //对集水井去重
        }

        private void RecognizeCollectWellByCurve(Database database, Point3dCollection pts)
        {
            var objs = new DBObjectCollection();

            // 根据图层规则找到指定的Polyline,Line
            using (var curveEngine = new ThDrainageWellRecognitionEngine())
            {
                curveEngine.Recognize(database, pts);
                curveEngine.Geos.ForEach(o => objs.Add(o));
            }

            // 根据图层规则,找到在图层的块
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

            // 用以上提取的数据源通过算法识别集水井和地沟
            Build(objs, pts);
        }

        private void RecognizeCollectWellByBlock(Database database, Point3dCollection pts)
        {
            if (DrainageBlkNames.Count == 0)
            {
                return;
            }
            // 只看图块
            // 根据指定图块名，来获取集水井，把块中的几何图层提取，然后求OBB
            using (var blockEngine = new ThDrainageWellBlockRecognitionEngine())
            {
                blockEngine.Visitor.CheckQualifiedBlockName = CheckBlockNameQualified;
                blockEngine.Visitor.CheckQualifiedLayer = (Entity e) => true;
                blockEngine.Recognize(database, pts);
                blockEngine.RecognizeMS(database, pts);
                blockEngine.Geos
                    .OfType<Polyline>()              
                    .Where(p => p.Area > 1.0)
                    .ForEach(p => CollectingWells.Add(p));
            }
        }

        private void RecognizeFloorDrainByBlock(Database database, Point3dCollection pts)
        {
            // 获取地漏
            if (FloorDrainBlkNames.Count == 0)
            {
                return;
            }
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

        private void DuplicatedRemove()
        {
            DuplicatedRemove(FloorDrains);
            DuplicatedRemove(CollectingWells);
            DuplicatedRemove(DrainageDitches);
        }

        private void DuplicatedRemove(List<Entity> entities)
        {
            var results = ThCADCoreNTSGeometryFilter.GeometryEquality(entities.ToCollection());
            entities = results.OfType<Entity>().ToList();
        }

        private Curve Tesslate(Entity curve,double tesslateLength =100.0)
        {
            if(curve is Line line)
            {
                return line;
            }
            if (curve is Arc arc)
            {
                return arc.TessellateArcWithArc(tesslateLength);
            }
            if (curve is Circle circle)
            {
                return circle.TessellateCircleWithArc(tesslateLength);
            }
            if (curve is Polyline polyline)
            {
                return polyline.Tessellate(tesslateLength);
            }
            return new Line();
        }
        private void Build(DBObjectCollection objs, Point3dCollection pts)
        {
            var center = pts.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            transformer.Transform(objs);
            var newPts = transformer.Transform(pts);
            var spatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(objs);
            objs = spatialIndex.SelectCrossingPolygon(newPts);

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
