using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using TianHua.Mep.UI.Data;

namespace TianHua.Mep.UI.Command
{
    public class ThExtractRoomDataCmd : ThMEPBaseCommand, IDisposable
    {
        private DBObjectCollection shearwalls;
        private DBObjectCollection columns;
        private DBObjectCollection doors;
        private DBObjectCollection walls;
        private double NeibourRangeDistance = 200.0;
        /// <summary>
        /// 配置的墙体图层
        /// </summary>
        private List<string> WallLayers { get; set; } = new List<string>();
        public Point3dCollection RangePts { get; private set; }
        public bool YnExtractShearWall { get; set; }

        #region ---------- 提取的对象 ----------
        /// <summary>
        /// 返回提取的剪力墙
        /// </summary>
        public DBObjectCollection ShearWalls => shearwalls;
        // <summary>
        /// 返回提取的柱
        /// </summary>
        public DBObjectCollection Columns => columns;
        /// <summary>
        /// 返回提取的门
        /// </summary>
        public DBObjectCollection Doors => doors;
        /// <summary>
        /// 返回提取的墙线
        /// 除了ShearWalls、Columns、Doors之外的物体都当做墙线处理
        /// </summary>
        public DBObjectCollection Walls => walls;
        #endregion
        public ThExtractRoomDataCmd(List<string> wallLayers)
        {
            ActionName = "提取房间框线";
            CommandName = "THEROC";
            WallLayers = wallLayers;
            doors = new DBObjectCollection();
            walls = new DBObjectCollection();
            columns = new DBObjectCollection();
            shearwalls = new DBObjectCollection();
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
                // 提取数据 + 收集数据
                var roomData = GetRoomData(acadDb.Database, RangePts);
                var wallObjs = GetConfigWalls(acadDb.Database, RangePts);
                var otherShearWalls = GetOtherShearwalls(acadDb.Database, RangePts);
                // 收集建筑墙线  
                walls = walls.Union(wallObjs);
                walls = walls.Union(roomData.Slabs);
                walls = walls.Union(roomData.Windows);
                walls = walls.Union(roomData.Cornices);
                walls = walls.Union(roomData.CurtainWalls);
                walls = walls.Union(roomData.RoomSplitlines);
                walls = walls.Union(roomData.ArchitectureWalls);
                // 收集门和柱
                doors = doors.Union(roomData.Doors);
                columns = columns.Union(roomData.Columns);
                // 收集剪力墙
                shearwalls = shearwalls.Union(roomData.ShearWalls);
                // 其它剪力墙在此处不收集，需要处理

                // 把数据移动到近似原点
                var centerPt = RangePts.Envelope().CenterPoint();
                var transformer = new ThMEPOriginTransformer(centerPt);
                transformer.Transform(walls);
                transformer.Transform(doors);
                transformer.Transform(columns);
                transformer.Transform(shearwalls);
                transformer.Transform(otherShearWalls);

                // 过滤
                // 过滤柱子内的柱子
                var innerObjs = FilterInnerColumns(columns, otherShearWalls); // last指向otherShearwall
                innerObjs.OfType<DBObject>().ForEach(e => otherShearWalls.Remove(e));
                innerObjs.MDispose();

                // 过滤孤立元素
                // 孤立元素定义：附近多少距离以内没有东西算孤立元素。
                var totalObjs = new DBObjectCollection();
                walls.OfType<DBObject>().ForEach(o => totalObjs.Add(o));
                doors.OfType<DBObject>().ForEach(o => totalObjs.Add(o));
                columns.OfType<DBObject>().ForEach(o => totalObjs.Add(o));
                shearwalls.OfType<DBObject>().ForEach(o => totalObjs.Add(o));               
                otherShearWalls.OfType<DBObject>().ForEach(o => totalObjs.Add(o));
                var objSpatialIndex = new ThCADCoreNTSSpatialIndex(totalObjs);

                // 过滤孤立柱
                var isolatedColumns = FilterIsolatedElements(columns, objSpatialIndex, NeibourRangeDistance);
                // 过滤孤立的“其它剪力墙”
                var isolatedOtherShearWalls = FilterIsolatedElements(otherShearWalls, objSpatialIndex, NeibourRangeDistance);
                // 移除孤立元素并释放
                isolatedColumns.OfType<DBObject>().ForEach(e => columns.Remove(e));
                isolatedOtherShearWalls.OfType<DBObject>().ForEach(e => otherShearWalls.Remove(e));
                isolatedColumns.MDispose();
                isolatedOtherShearWalls.MDispose();

                // 把“其它剪力墙”添加到剪力墙中
                shearwalls = shearwalls.Union(otherShearWalls);

                // 把数据还原到原位置
                transformer.Reset(walls);
                transformer.Reset(doors);
                transformer.Reset(columns);
                transformer.Reset(shearwalls);

                // 把Mpolygon转成Curve
                walls = ToCurves(walls, true);
                columns = ToCurves(columns, true);
                shearwalls = ToCurves(shearwalls,true); 
            }
        }

        private DBObjectCollection FilterIsolatedElements(DBObjectCollection polygons, ThCADCoreNTSSpatialIndex spatialIndex,double rangeTolerance)
        {
            var isolatedElements = new DBObjectCollection();
            var bufferService = new ThNTSBufferService();
            polygons.OfType<Entity>().ForEach(e =>
            {
                var enlargePolygon = bufferService.Buffer(e, rangeTolerance);
                var neibourObjs = spatialIndex.SelectCrossingPolygon(enlargePolygon);
                if(neibourObjs.Count==1 && neibourObjs.Contains(e))
                {
                    isolatedElements.Add(e);
                }
                enlargePolygon.Dispose();
            });
            return isolatedElements;    
        }

        private DBObjectCollection FilterInnerColumns(DBObjectCollection columns, DBObjectCollection otherShearwalls)
        {
            // 由于提取其它剪力墙，导致柱子内部也有物体
            var sptialIndex = new ThCADCoreNTSSpatialIndex(otherShearwalls);
            var results = new DBObjectCollection(); // 返回在column内部的元素
            columns.OfType<Entity>().ForEach(e =>
            {
                var innerObjs = sptialIndex.SelectWindowPolygon(e);
                innerObjs.OfType<DBObject>().ForEach(o => results.Add(o));
            });
            return results;
        }

        private DBObjectCollection ToCurves(DBObjectCollection objs,bool disposeMpolygon=false)
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
                    if(disposeMpolygon)
                    {
                        mPolygon.Dispose();
                    }
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
