using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;

namespace TianHua.Mep.UI.Command
{
    public class ThExtractWallLinesCmd: ThMEPBaseCommand,IDisposable
    {
        /// <summary>
        /// 配置的墙体图层
        /// </summary>
        private List<string> WallLayers { get; set; } = new List<string>();
        /// <summary>
        /// 返回提取的墙线
        /// </summary>
        public DBObjectCollection Walls { get; private set; }
        public Point3dCollection RangePts { get; private set; }
        public ThExtractWallLinesCmd(List<string> wallLayers)
        {
            ActionName = "提取墙线";
            CommandName = "XXXX";
            WallLayers = wallLayers;
            Walls = new DBObjectCollection();
            RangePts = new Point3dCollection();
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                RangePts = GetRange(); //获取布置范围
                if (RangePts.Count < 3)
                {
                    return;
                }
                Walls = new DBObjectCollection();
                var originObjs = GetRoomDatas(acadDb.Database, RangePts);
                var wallObjs = GetConfigWalls(acadDb.Database, RangePts);
                Walls = Walls.Union(originObjs);
                Walls = Walls.Union(wallObjs);
            }
        }

        private DBObjectCollection GetConfigWalls(Database database, Point3dCollection frame)
        {
            var layers = new List<string>();
            var defaultPCLayers = ThPCArchitectureWallLayerManager.CurveXrefLayers(database);
            layers.AddRange(defaultPCLayers);
            layers.AddRange(WallLayers.Where(o => !defaultPCLayers.Contains(o)));

            var wallVisitor = new ThWallExtractionVisitor()
            {
                LayerFilter = layers,
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(wallVisitor);
            extractor.Extract(database);

            var totalObjs = wallVisitor.Results
                .Select(o => o.Geometry).ToCollection();

            var transformer = new ThMEPOriginTransformer(totalObjs);
            var newFrame = transformer.Transform(frame);
            transformer.Transform(totalObjs);
            var results = SelectCrossPolygon(totalObjs, newFrame);
            transformer.Reset(totalObjs);
            var restObjs = totalObjs.Difference(results);
            restObjs.MDispose();
            return results;
        }
        private DBObjectCollection SelectCrossPolygon(DBObjectCollection objs, Point3dCollection polygon)
        {
            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                return spatialIndex.SelectCrossingPolygon(polygon);
            }
            else
            {
                return objs;
            }            
        }
        private DBObjectCollection GetRoomDatas(Database database,Point3dCollection frame)
        {
            var data = new ThRoomdata(false);
            data.Build(database, frame);
            return data.MergeData();
        }
        private Point3dCollection GetRange()
        {
            var frame = ThWindowInteraction.GetPolyline(
                    PointCollector.Shape.Window, new List<string> { "请框选一个范围" });
            if (frame.Area < 1e-4)
            {
                return new Point3dCollection();
            }
            var nFrame = ThMEPFrameService.Normalize(frame);
            return nFrame.Vertices();
        }
    }
}
