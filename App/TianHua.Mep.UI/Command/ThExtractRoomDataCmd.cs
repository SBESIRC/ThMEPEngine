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
    public class ThExtractRoomDataCmd : ThMEPBaseCommand, IDisposable
    {
        /// <summary>
        /// 配置的墙体图层
        /// </summary>
        private List<string> WallLayers { get; set; } = new List<string>();
        /// <summary>
        /// 返回提取的墙
        /// </summary>
        public DBObjectCollection Walls { get; private set; }
        // <summary>
        /// 返回提取的柱
        /// </summary>
        public DBObjectCollection Columns { get; private set; }
        public DBObjectCollection Doors { get; private set; }
        public Point3dCollection RangePts { get; private set; }
        public bool YnExtractShearWall { get; set; }
        public ThExtractRoomDataCmd(List<string> wallLayers)
        {
            ActionName = "提取房间框线";
            CommandName = "THEROC";
            WallLayers = wallLayers;
            Doors = new DBObjectCollection();
            Walls = new DBObjectCollection();
            Columns = new DBObjectCollection();
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
                // 把图层配置提取的墙线，合并到Walls中
                var wallObjs = GetConfigWalls(acadDb.Database, RangePts);
                Walls = Walls.Union(wallObjs);

                var roomData = GetRoomData(acadDb.Database, RangePts);                
                Walls = Walls.Union(roomData.Slabs);
                Walls = Walls.Union(roomData.Windows);
                Walls = Walls.Union(roomData.Cornices);
                Walls = Walls.Union(roomData.ShearWalls);
                Walls = Walls.Union(roomData.CurtainWalls);
                Walls = Walls.Union(roomData.RoomSplitlines);
                Walls = Walls.Union(roomData.ArchitectureWalls);

                Doors = Doors.Union(roomData.Doors); 
                Columns = Columns.Union(roomData.Columns);
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
        private ThRoomdata GetRoomData(Database database,Point3dCollection frame)
        {
            var data = new ThRoomdata(false)
            { 
                YnExtractShearWall=this.YnExtractShearWall,
            };
            data.Build(database, frame);
            return data;
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
