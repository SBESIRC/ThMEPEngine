using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPStructure.Reinforcement.Service;

namespace ThMEPStructure.Reinforcement.Data.YJK
{
    /// <summary>
    /// 提取墙柱
    /// </summary>
    internal class ThExtractWallColumnService
    {
        public DBObjectCollection Elements { get; set; }
        private double SmallAreaTolerance = 100.0;
        private List<string> WallColumnLayers { get; set; }
        public ThExtractWallColumnService(List<string> wallColumnLayers)
        {
            Elements = new DBObjectCollection();
            WallColumnLayers = wallColumnLayers;
        }
        public void Extract(Database db,Point3dCollection pts)
        {
            // 从本地和块中获取指定图层的对象
            var objs = ExtractWallColumns(db, WallColumnLayers);

            // 获取提取对象中的线
            var cloneLines = objs.OfType<Line>().Select(o => o.Clone() as Line).ToCollection();
            cloneLines= cloneLines.Union(
                objs.OfType<Polyline>().SelectMany(o=>o.GetLines()).ToCollection());

            // 移到近原点处
            var transformer = new ThMEPOriginTransformer(cloneLines);
            transformer.Transform(cloneLines);
            var newPts = transformer.Transform(pts);

            // 按范围过滤
            var filterLines = cloneLines.SelectCrossPolygon(newPts);
            // 清理
            var cleanLines = filterLines.Clean(); 
            // 延伸
            var extendLines = cleanLines.Extend(1.0);
            cleanLines.DisposeEx();

            // 造面
            var allPolygons = extendLines.PolygonsEx();
            var results = allPolygons.PostProcess(SmallAreaTolerance);
            var restPolygons = allPolygons.Difference(results);
            extendLines.DisposeEx();            
            restPolygons.DisposeEx();
            cloneLines.DisposeEx();

            // 还原到原始位置
            transformer.Reset(results);

            // 返回结果
            results.OfType<Entity>().ForEach(e => Elements.Add(e));
        }

        private DBObjectCollection ExtractWallColumns(Database database,List<string> layerFilter)
        {
            var visitor = new ThWallColumnExtractionVisitor()
            {
                LayerFilter = layerFilter.ToHashSet(),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);
            return visitor.Results.Select(o => o.Geometry).ToCollection();
        }
    }
}
