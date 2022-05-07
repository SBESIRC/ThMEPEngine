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
        /// 配置的幕墙图层
        /// </summary>
        private List<string> PcWallLayers { get; set; } = new List<string>();
        /// <summary>
        /// 返回提取的墙线
        /// </summary>
        public DBObjectCollection Walls { get; private set; }
        public ThExtractWallLinesCmd(List<string> pcWallLayers)
        {
            ActionName = "提取墙线";
            CommandName = "XXXX";
            PcWallLayers = pcWallLayers;
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var pts = GetRange(); //获取布置范围
                if (pts.Count < 3)
                {
                    return;
                }
                Walls = new DBObjectCollection();
                var originObjs = GetRoomDatas(acadDb.Database, pts);
                var pcArchObjs = GetPcArchWalls(acadDb.Database, pts);
                Walls = Walls.Union(originObjs);
                Walls = Walls.Union(pcArchObjs);
            }
        }

        private DBObjectCollection GetPcArchWalls(Database database, Point3dCollection frame)
        {
            var layers = new List<string>();
            var defaultPCLayers = ThPCArchitectureWallLayerManager.CurveXrefLayers(database);
            layers.AddRange(defaultPCLayers);
            layers.AddRange(PcWallLayers.Where(o => !defaultPCLayers.Contains(o)));

            var pcArchWallVisitor = new ThPCArchitectureWallExtractionVisitor()
            {
                LayerFilter = layers,
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(pcArchWallVisitor);
            extractor.Extract(database);

            var totalObjs = pcArchWallVisitor.Results
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
