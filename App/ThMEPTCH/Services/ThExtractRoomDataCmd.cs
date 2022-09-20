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
using ThMEPTCH.CAD;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;

namespace ThMEPTCH.Services
{
    public class ThExtractRoomDataCmd : ThMEPBaseCommand, IDisposable
    {
        private DBObjectCollection _shearwalls;
        private DBObjectCollection _otherShearwalls;
        private DBObjectCollection _columns;
        private DBObjectCollection _doors;
        private DBObjectCollection _walls;
        private Point3dCollection _rangePts;
        private double NeibourRangeDistance = 200.0;
        /// <summary>
        /// 配置的墙体图层
        /// </summary>
        private List<string> WallLayers { get; set; } = new List<string>();
        public Point3dCollection RangePts => _rangePts;
        public bool YnExtractShearWall { get; set; }

        #region ---------- 提取的对象 ----------
        /// <summary>
        /// 返回提取的剪力墙
        /// </summary>
        public DBObjectCollection ShearWalls
        {
            get
            {
                var shearWalls = new DBObjectCollection();
                shearWalls = shearWalls.Union(_shearwalls);
                shearWalls = shearWalls.Union(_otherShearwalls);
                return shearWalls;
            }
        }
        // <summary>
        /// 返回提取的柱
        /// </summary>
        public DBObjectCollection Columns => _columns;
        /// <summary>
        /// 返回提取的门
        /// </summary>
        public DBObjectCollection Doors => _doors;
        /// <summary>
        /// 返回提取的墙线
        /// 除了ShearWalls、Columns、Doors之外的物体都当做墙线处理
        /// </summary>
        public DBObjectCollection Walls => _walls;
        #endregion
        public ThExtractRoomDataCmd(List<string> wallLayers)
        {
            ActionName = "提取房间框线";
            CommandName = "THEROC";
            WallLayers = wallLayers;
            _doors = new DBObjectCollection();
            _walls = new DBObjectCollection();
            _columns = new DBObjectCollection();
            _shearwalls = new DBObjectCollection();
            _otherShearwalls = new DBObjectCollection();
            _rangePts = new Point3dCollection();
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                _rangePts = ThAuxiliaryUtils.GetRange(); //获取布置范围
                if (_rangePts.Count < 3)
                {
                    return;
                }
                // 提取数据 + 收集数据
                var roomData = GetRoomData(acadDb.Database, _rangePts);
                var wallObjs = GetConfigWalls(acadDb.Database, _rangePts);               
                var tchwallElements = GetTCHWalls(acadDb.Database, _rangePts);
                var tchDoorElements = GetTCHDoors(acadDb.Database, _rangePts);
                _otherShearwalls = GetOtherShearwalls(acadDb.Database, _rangePts);
                // 收集建筑墙线  
                _walls = _walls.Union(wallObjs);               
                _walls = _walls.Union(roomData.Slabs);
                _walls = _walls.Union(roomData.Windows);
                _walls = _walls.Union(roomData.Cornices);
                _walls = _walls.Union(roomData.CurtainWalls);
                _walls = _walls.Union(roomData.RoomSplitlines);
                _walls = _walls.Union(roomData.ArchitectureWalls);
                // 收集门
                _doors = _doors.Union(roomData.Doors);               
                // 收集柱
                _columns = _columns.Union(roomData.Columns);
                // 收集剪力墙
                _shearwalls = _shearwalls.Union(roomData.ShearWalls);
                // 其它剪力墙在此处不收集，需要处理

                // 把数据移动到近似原点
                var centerPt = _rangePts.Envelope().CenterPoint();
                var transformer = new ThMEPOriginTransformer(centerPt);
                transformer.Transform(_walls);
                transformer.Transform(_doors);
                transformer.Transform(_columns);
                transformer.Transform(_shearwalls);
                transformer.Transform(_otherShearwalls);

                var tchDoors = tchDoorElements.Select(o => o.Geometry).ToCollection();
                var tchwalls = tchwallElements.Select(o => o.Geometry).ToCollection();
                transformer.Transform(tchwalls);
                transformer.Transform(tchDoors);

                // 对天正的门造洞(暂时不考虑弧门,暂时默认tchDoors没有弧门;暂时不考虑弧墙)
                var linearWalls = tchwallElements
                    .Where(o => o.Data is TArchWall archWall && archWall.IsArc == false)
                    .Select(o => o.Geometry).ToCollection();
               
                var doorOpenings = CreateLinearDoorOpening(linearWalls, tchDoors);
                tchDoors.MDispose();
                tchDoors = doorOpenings.Values.ToCollection(); // 只收集天正的的门洞

                // 用DB的门过滤天正的门洞
                var dbBufferDoors = _doors.BufferPolygons(5.0);
                var innerTchDoors = FilterInnerObjs(dbBufferDoors, tchDoors);
                tchDoors = tchDoors.Difference(innerTchDoors);
                innerTchDoors.MDispose();
                dbBufferDoors.MDispose();

                // 用天正的门过滤DB的门
                var tchBufferDoors = tchDoors.BufferPolygons(5.0);
                var innerDbDoors = FilterInnerObjs(tchBufferDoors, _doors);
                _doors = _doors.Difference(innerDbDoors);
                innerDbDoors.MDispose();
                tchBufferDoors.MDispose();

                // 把天正的门洞放入DB门中
                _doors = _doors.Union(tchDoors);
                // 把天正的墙放入建筑墙中
                _walls = _walls.Union(tchwalls);

                // 把柱子内部的元素过滤掉
                FilterColumnInnerObjs();

                // 过滤孤立元素(附近多少距离以内没有东西算孤立元素。)     
                FilterIsolatedColumns();
                FilterIsolatedOtherShearwalls();

                // 把数据还原到原位置
                transformer.Reset(_walls);
                transformer.Reset(_doors);
                transformer.Reset(_columns);
                transformer.Reset(_shearwalls);
                transformer.Reset(_otherShearwalls);

                // 把Mpolygon转成Curve
                _walls = ToCurves(_walls, true);
                _columns = ToCurves(_columns, true);
                _shearwalls = ToCurves(_shearwalls,true);
                _otherShearwalls = ToCurves(_shearwalls, true);
            }
        }

        private void FilterIsolatedColumns()
        {
            // 过滤孤立柱
            var objs = new DBObjectCollection();
            objs = objs.Union(_doors);
            objs = objs.Union(_walls);
            objs = objs.Union(_shearwalls);
            objs = objs.Union(_otherShearwalls);
            var objSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var isolatedObjs = FilterIsolatedElements(_columns, objSpatialIndex, NeibourRangeDistance);
            isolatedObjs.OfType<DBObject>().ForEach(o => _columns.Remove(o));
            isolatedObjs.MDispose();
        }

        private void FilterIsolatedOtherShearwalls()
        {
            // 过滤其它剪力墙
            var objs = new DBObjectCollection();
            objs = objs.Union(_doors);
            objs = objs.Union(_walls);
            objs = objs.Union(_columns);
            objs = objs.Union(_shearwalls);     
            var objSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var isolatedObjs = FilterIsolatedElements(_otherShearwalls, objSpatialIndex, NeibourRangeDistance);
            isolatedObjs.OfType<DBObject>().ForEach(o => _otherShearwalls.Remove(o));
            isolatedObjs.MDispose();
        }

        private void FilterColumnInnerObjs()
        {
            var columnInnerWalls = FilterInnerObjs(_columns, _walls);
            columnInnerWalls.OfType<DBObject>().ForEach(e => _walls.Remove(e));
            columnInnerWalls.MDispose();

            var columnInnerDoors = FilterInnerObjs(_columns, _doors);
            columnInnerDoors.OfType<DBObject>().ForEach(e => _doors.Remove(e));
            columnInnerDoors.MDispose();

            var columnInnerShearwalls = FilterInnerObjs(_columns, _shearwalls);
            columnInnerShearwalls.OfType<DBObject>().ForEach(e => _shearwalls.Remove(e));
            columnInnerShearwalls.MDispose();

            var columnInnerOtherShearwalls = FilterInnerObjs(_columns, _otherShearwalls);
            columnInnerOtherShearwalls.OfType<DBObject>().ForEach(e => _otherShearwalls.Remove(e));
            columnInnerOtherShearwalls.MDispose();
        }

        private Dictionary<Polyline,Polyline> CreateLinearDoorOpening(
            DBObjectCollection linearWalls, 
            DBObjectCollection linearDoors)
        {
            var doorSpatialIndex = new ThCADCoreNTSSpatialIndex(linearDoors);
            var bufferService = new ThNTSBufferService();
            var results = new Dictionary<Polyline, Polyline>();
            // 修正墙里的门
            linearWalls.OfType<Polyline>().ForEach(wall =>
            {
                var bufferObj = bufferService.Buffer(wall, 5.0);
                var innerDoors =  doorSpatialIndex.SelectWindowPolygon(bufferObj);
                innerDoors.OfType<Polyline>().ForEach(door =>
                {
                    var doorOpening =  CreateDoorOpening(wall, door);
                    if (doorOpening != null && !results.ContainsKey(door))
                    {
                        results.Add(door, doorOpening);
                    }
                });
            });
            return results;
        }

        private Polyline CreateDoorOpening(Polyline linearWall, Polyline linearDoor)
        {
            var shortPairs = GetRectangleShortPair(linearDoor);
            var longPairs = GetRectangleLongPair(linearWall);
            if (shortPairs.Count==2 && longPairs.Count == 2)
            {
                var firstPt = shortPairs[0].Item1.GetMidPt(shortPairs[0].Item2);
                var secondPt = shortPairs[1].Item1.GetMidPt(shortPairs[1].Item2);
                var pt1 = ThGeometryTool.GetProjectPtOnLine(firstPt, longPairs[0].Item1, longPairs[0].Item2);
                var pt2 = ThGeometryTool.GetProjectPtOnLine(secondPt, longPairs[0].Item1, longPairs[0].Item2);
                var pt3 = ThGeometryTool.GetProjectPtOnLine(firstPt, longPairs[1].Item1, longPairs[1].Item2);
                var pt4 = ThGeometryTool.GetProjectPtOnLine(secondPt, longPairs[1].Item1, longPairs[1].Item2);
                var pts = new Point3dCollection() { pt1, pt3, pt4, pt2 };
                return pts.CreatePolyline();
            }
            else
            {
                return null;
            }
        }

        private List<Tuple<Point3d, Point3d>> GetRectangleShortPair(Polyline rectangle)
        {
            var results = new List<Tuple<Point3d,Point3d>>();   
            var edges = GetRectangleEdges(rectangle);
            edges = edges.Where(o => o.Item1.DistanceTo(o.Item2) > 1e-6).ToList();
            if(edges.Count==4)
            {
                var first = edges[0];
                var third = edges[2];
                var second = edges[1];               
                var fourth = edges[3];
                if(first.Item1.DistanceTo(first.Item2)< second.Item1.DistanceTo(second.Item2))
                {
                    results.Add(first);
                    results.Add(third);
                }
                else
                {
                    results.Add(second);
                    results.Add(fourth);
                }
            }            
            return results;
        }

        private List<Tuple<Point3d, Point3d>> GetRectangleLongPair(Polyline rectangle)
        {
            var results = new List<Tuple<Point3d, Point3d>>();
            var edges = GetRectangleEdges(rectangle);
            edges = edges.Where(o => o.Item1.DistanceTo(o.Item2) > 1e-6).ToList();
            if (edges.Count == 4)
            {
                var first = edges[0];
                var third = edges[2];
                var second = edges[1];
                var fourth = edges[3];
                if (first.Item1.DistanceTo(first.Item2) > second.Item1.DistanceTo(second.Item2))
                {
                    results.Add(first);
                    results.Add(third);
                }
                else
                {
                    results.Add(second);
                    results.Add(fourth);
                }
            }
            return results;
        }
        private List<Tuple<Point3d, Point3d>> GetRectangleEdges(Polyline rectangle)
        {
            var edges = new List<Tuple<Point3d, Point3d>>();
            for (int i = 0; i < rectangle.NumberOfVertices; i++)
            {
                var segType = rectangle.GetSegmentType(i);
                if (segType != SegmentType.Line)
                {
                    continue;
                }
                var lineSeg = rectangle.GetLineSegmentAt(i);
                edges.Add(Tuple.Create(lineSeg.StartPoint, lineSeg.EndPoint));
            }
            return edges;
        }

        private DBObjectCollection FilterIsolatedElements(DBObjectCollection polygons, ThCADCoreNTSSpatialIndex spatialIndex,double rangeTolerance)
        {
            var isolatedElements = new DBObjectCollection();
            var bufferService = new ThNTSBufferService();
            polygons.OfType<Entity>().ForEach(e =>
            {
                var enlargePolygon = bufferService.Buffer(e, rangeTolerance);
                var neibourObjs = spatialIndex.SelectCrossingPolygon(enlargePolygon);
                if(neibourObjs.Count== 0)
                {
                    isolatedElements.Add(e);
                }
                enlargePolygon.Dispose();
            });
            return isolatedElements;    
        }

        private DBObjectCollection FilterInnerObjs(DBObjectCollection firstPolygons, DBObjectCollection secondPolygons)
        {
            // 过滤在First内部的元素
            var sptialIndex = new ThCADCoreNTSSpatialIndex(secondPolygons);
            var results = new DBObjectCollection();
            firstPolygons.OfType<Entity>().ForEach(e =>
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
        private List<ThRawIfcBuildingElementData> GetTCHWalls(Database database,Point3dCollection polygon)
        {
            var visitor = new ThTCHArchWallExtractionVisitor();
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);

            var geometries = visitor.Results.Select(o => o.Geometry).ToCollection();
            var transformer = new ThMEPOriginTransformer();
            if (polygon.Count>=3)
            {
                var center = polygon.Envelope().CenterPoint();
                transformer = new ThMEPOriginTransformer(center);
            }
            else
            {
                transformer = new ThMEPOriginTransformer(geometries);
            }
            var newFrame = transformer.Transform(polygon);
            transformer.Transform(geometries);
            var filterObjs = SelectCrossPolygon(geometries, newFrame);
            transformer.Reset(geometries);
            var results = visitor.Results.Where(o => filterObjs.Contains(o.Geometry)).ToList();

            // 释放
            var restObjs = geometries.Difference(filterObjs);
            restObjs.MDispose();
            return results;
        }

        private List<ThRawIfcBuildingElementData> GetTCHDoors(Database database, Point3dCollection polygon)
        {
            var visitor = new ThTCHDoorExtractionVisitor();
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);

            var geometries = visitor.Results.Select(o => o.Geometry).ToCollection();
            var transformer = new ThMEPOriginTransformer();
            if (polygon.Count >= 3)
            {
                var center = polygon.Envelope().CenterPoint();
                transformer = new ThMEPOriginTransformer(center);
            }
            else
            {
                transformer = new ThMEPOriginTransformer(geometries);
            }
            var newFrame = transformer.Transform(polygon);
            transformer.Transform(geometries);
            var filterObjs = SelectCrossPolygon(geometries, newFrame);
            transformer.Reset(geometries);
            var results = visitor.Results.Where(o => filterObjs.Contains(o.Geometry)).ToList();

            // 释放
            var restObjs = geometries.Difference(filterObjs);
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
    }
}
