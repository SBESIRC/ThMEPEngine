using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using ThMEPStructure.HuaRunPeiJin.Service;

namespace ThMEPStructure.HuaRunPeiJin.Data.YJK
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
            // 获取指定图层的对象
            var objs = db.GetEntitiesFromMS(WallColumnLayers);

            // 获取提取对象中的线
            var cloneLines = objs.OfType<Line>().Select(o => o.Clone() as Line).ToCollection();

            // 移到近原点处
            var transformer = new ThMEPOriginTransformer(cloneLines);
            transformer.Transform(cloneLines);
            var newPts = transformer.Transform(pts);

            // 按范围过滤
            var filterLines = cloneLines.SelectCrossPolygon(newPts);
            var restLines = cloneLines.Difference(filterLines);
            restLines.DisposeEx();

            // 清理
            var cleanLines = filterLines.Clean();
            filterLines.DisposeEx();

            // 延伸
            var extendLines = cleanLines.Extend(1.0);
            cleanLines.DisposeEx();

            // 造面
            var allPolygons = extendLines.PolygonsEx();
            extendLines.DisposeEx();
            var polygons = allPolygons.FilterSmallArea(SmallAreaTolerance);
            var restPolygons = allPolygons.Difference(polygons);
            restPolygons.DisposeEx();

            // 还原到原始位置
            transformer.Reset(restPolygons);

            // 返回结果
            restPolygons.OfType<Entity>().ForEach(e => Elements.Add(e));
        }
    }
}
