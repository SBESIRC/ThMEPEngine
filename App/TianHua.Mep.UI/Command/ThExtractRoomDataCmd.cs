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
using TianHua.Mep.UI.Data;
using Dreambuild.AutoCAD;

namespace TianHua.Mep.UI.Command
{
    public class ThExtractRoomDataCmd : ThMEPBaseCommand, IDisposable
    {
        #region ---------- 提取的对象 ----------
        /// <summary>
        /// 返回提取的剪力墙
        /// </summary>
        public DBObjectCollection ShearWalls { get; private set; }
        // <summary>
        /// 返回提取的柱
        /// </summary>
        public DBObjectCollection Columns { get; private set; }
        /// <summary>
        /// 返回提取的门
        /// </summary>
        public DBObjectCollection Doors { get; private set; }
        /// <summary>
        /// 返回提取的墙线
        /// 除了ShearWalls、Columns、Doors之外的物体都当做墙线处理
        /// </summary>
        public DBObjectCollection Walls { get; private set; }
        #endregion
        /// <summary>
        /// 配置的墙体图层
        /// </summary>
        private List<string> WallLayers { get; set; } = new List<string>();
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
            ShearWalls = new DBObjectCollection();
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
                // 获取默认围合房间的数据
                var roomData = GetRoomData(acadDb.Database, RangePts);

                // 收集墙线                
                var wallObjs = GetConfigWalls(acadDb.Database, RangePts);
                Walls = Walls.Union(wallObjs);                
                Walls = Walls.Union(roomData.Slabs);
                Walls = Walls.Union(roomData.Windows);
                Walls = Walls.Union(roomData.Cornices);
                Walls = Walls.Union(roomData.CurtainWalls);
                Walls = Walls.Union(roomData.RoomSplitlines);
                Walls = Walls.Union(roomData.ArchitectureWalls);

                // 收集门和柱
                Doors = Doors.Union(roomData.Doors);
                Columns = Columns.Union(roomData.Columns);

                // 收集剪力墙
                ShearWalls = ShearWalls.Union(roomData.ShearWalls);
                var otherShearWalls = GetOtherShearwalls(acadDb.Database, RangePts);
                ShearWalls = ShearWalls.Union(otherShearWalls);

                // 过滤孤立柱
                var centerPt = RangePts.Envelope().CenterPoint();
                var transformer = new ThMEPOriginTransformer(centerPt);
                transformer.Transform(Walls);
                transformer.Transform(Doors);
                transformer.Transform(Columns);
                transformer.Transform(ShearWalls);
                var wallSpatialIndex = new ThCADCoreNTSSpatialIndex(Walls);
                var doorSpatialIndex = new ThCADCoreNTSSpatialIndex(Doors);
                var shearwallSpatialIndex = new ThCADCoreNTSSpatialIndex(ShearWalls);
                var bufferService = new ThNTSBufferService();
                Columns = Columns.OfType<Entity>().Where(o =>
                {
                    var entity = bufferService.Buffer(o, 5.0);
                    bool result = HasNeibours(entity, wallSpatialIndex) ||
                    HasNeibours(entity, doorSpatialIndex) ||
                    HasNeibours(entity, shearwallSpatialIndex);
                    entity.Dispose();
                    return result;
                }).ToCollection();
                transformer.Reset(Walls);
                transformer.Reset(Doors);
                transformer.Reset(Columns);
                transformer.Reset(ShearWalls);

                // 转成Curve
                ShearWalls = ToCurves(ShearWalls);
                Columns = ToCurves(Columns);
                Walls = ToCurves(Walls);
            }
        }

        private bool HasNeibours(Entity entity,ThCADCoreNTSSpatialIndex spatialIndex)
        {
            return spatialIndex.SelectCrossingPolygon(entity).Count > 0;
        }

        private DBObjectCollection ToCurves(DBObjectCollection objs)
        {
            var results = new DBObjectCollection();
            objs.OfType<Entity>().ForEach(e =>
            {
                if(e is Curve curve)
                {
                    results.Add(curve);
                }
                else if(e is MPolygon mPolygon)
                {
                    results = results.Union(ToCurves(mPolygon));
                }                
            });
            return results;
        }

        private DBObjectCollection ToCurves(MPolygon mPolygon)
        {
            var results = new DBObjectCollection();
            results.Add(mPolygon.Shell());
            mPolygon.Holes().ForEach(o => results.Add(o));
            return results;
        }
        private DBObjectCollection GetConfigWalls(Database database, Point3dCollection frame)
        {
            //把图层配置提取的墙线，合并到Walls中
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
        private ThRoomdata GetRoomData(Database database, Point3dCollection frame)
        {
            var data = new ThRoomdata(false)
            {
                YnExtractShearWall = this.YnExtractShearWall,
            };
            data.Build(database, frame);
            return data;
        }

        private DBObjectCollection GetOtherShearwalls(Database database, Point3dCollection frame)
        {
            var otherShearWallEngine = new ThOtherShearWallRecognitionEngine();
            otherShearWallEngine.Recognize(database, frame);
            return otherShearWallEngine.Geometries;
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
