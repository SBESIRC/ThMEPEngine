using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPElectrical.AFAS.Service;
using ThMEPElectrical.AFAS.Interface;

namespace ThMEPElectrical.AFAS.Data
{
    public class ThAFASRoomExtractor : ThRoomExtractor, IGroup, ISetStorey, ITransformer
    {
        private List<ThStoreyInfo> StoreyInfos { get; set; }

        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }

        public ThAFASRoomExtractor()
        {
            StoreyInfos = new List<ThStoreyInfo>();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            //获取本地的房间框线
            var roomOutlineExtraction = new ThAIRoomOutlineExtractionEngine();
            roomOutlineExtraction.ExtractFromMS(database);

            //获取本地的房间标注
            var roomMarkExtraction = new ThAIRoomMarkExtractionEngine();
            roomMarkExtraction.ExtractFromMS(database);

            roomOutlineExtraction.Results.ForEach(o => Transformer.Transform(o.Geometry));
            roomMarkExtraction.Results.ForEach(o => Transformer.Transform(o.Geometry));

            var newPts = Transformer.Transform(pts);
            var roomEngine = new ThAIRoomOutlineRecognitionEngine();
            roomEngine.Recognize(roomOutlineExtraction.Results, newPts);
            var rooms = roomEngine.Elements.Cast<ThIfcRoom>().ToList();
            var markEngine = new ThAIRoomMarkRecognitionEngine();
            markEngine.Recognize(roomMarkExtraction.Results, newPts);
            var marks = markEngine.Elements.Cast<ThIfcTextNote>().ToList();

            //对于起、终点间距小于一定距离的，做缝合
            for (int i = 0; i < rooms.Count; i++)
            {
                rooms[i].Boundary = ThHandleNonClosedPolylineService.Handle(rooms[i].Boundary as Curve);
            }
            //过滤无效的房间框线
            rooms = rooms.Where(o => (o.Boundary as Curve).Area >= 1.0).ToList();

            //造房间
            var roomBuilder = new ThRoomBuilderEngine();
            roomBuilder.Build(rooms, marks, true);
            Rooms = rooms;
            //把弧线转成直线
            Clean();

            //设置房间名称
            Rooms.ForEach(o =>
            {
                if (!string.IsNullOrEmpty(o.Name) && !o.Tags.Contains(o.Name))
                {
                    o.Tags.Add(o.Name);
                }
                o.Name = string.Join(";", o.Tags.ToArray());
            });
        }

        public List<ThIfcRoom> SplitByFrames(DBObjectCollection frames)
        {
            var roomCollector = new List<ThIfcRoom>();
            var inFrames = FilterInFrames(Rooms, frames); // 收集完全在Frames里面的房间
            roomCollector.AddRange(inFrames);

            var inFrameBoundaries = inFrames.Select(r => r.Boundary).ToCollection();
            var notInFrames = Rooms.Where(o => !inFrameBoundaries.Contains(o.Boundary)); // 得到不在防火分区里的房间
            var spatialIndex = new ThCADCoreNTSSpatialIndex(frames);
            var bufferService = new ThNTSBufferService();
            notInFrames.ForEach(r =>
            {
                var newBoundary = bufferService.Buffer(r.Boundary, -1.0);
                var objs = spatialIndex.SelectCrossingPolygon(newBoundary);
                if (objs.Count > 0)
                {
                    var inters = Intersection(r.Boundary, objs);
                    inters.OfType<Entity>().ForEach(e =>
                    {
                        var room = new ThIfcRoom()
                        {
                            Boundary = e,
                            Tags = r.Tags,
                            Name = r.Name,
                            Uuid = Guid.NewGuid().ToString(),
                        };
                        roomCollector.Add(room);
                    });
                }
            });
            return roomCollector;
        }

        public void UpdateRooms(List<ThIfcRoom> newRooms)
        {
            this.Rooms = newRooms;
        }

        private List<ThIfcRoom> FilterInFrames(List<ThIfcRoom> rooms,
            DBObjectCollection frames, double edgeTolerance = 1.0)
        {
            var objs = rooms.Select(o => o.Boundary).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var collector = new DBObjectCollection(); // 收集在frames里面的房间
            var bufferService = new ThNTSBufferService();
            frames.OfType<Entity>().ForEach(e =>
            {
                var newFrame = bufferService.Buffer(e, edgeTolerance); // 用于解决边界重叠
                var inners = spatialIndex.SelectWindowPolygon(newFrame);
                collector = collector.Union(inners);
            });
            return rooms.Where(r => collector.Contains(r.Boundary)).ToList();
        }

        private DBObjectCollection Intersection(Entity entity, DBObjectCollection objs)
        {
            //减去不在Entity里面的东西
            var results = ThCADCoreNTSEntityExtension.Intersection(entity, objs, true);
            results = ClearZeroPolygon(results); //清除面积为零
            results = MakeValid(results); //解决自交的Case
            results = ClearZeroPolygon(results); //清除面积为零
            results = DuplicatedRemove(results); //去重
            return results;
        }

        private DBObjectCollection ClearZeroPolygon(DBObjectCollection objs, double areaTolerance = 1.0)
        {
            return objs.Cast<Entity>().Where(o =>
            {
                if (o is Polyline polyline)
                {
                    return polyline.Area > areaTolerance;
                }
                else if (o is MPolygon mPolygon)
                {
                    return mPolygon.Area > areaTolerance;
                }
                else
                {
                    return false;
                }
            }).ToCollection();
        }

        private DBObjectCollection MakeValid(DBObjectCollection polygons)
        {
            var results = new DBObjectCollection();
            polygons.Cast<Entity>().ForEach(o =>
            {
                if (o is Polyline polyline)
                {
                    var res = polyline.MakeValid();
                    res.Cast<Entity>().ForEach(e => results.Add(e));
                }
                else if (o is MPolygon mPolygon)
                {
                    var res = mPolygon.MakeValid(true);
                    res.Cast<Entity>().ForEach(e => results.Add(e));
                }
            });
            return results;
        }

        private DBObjectCollection DuplicatedRemove(DBObjectCollection objs)
        {
            return ThCADCoreNTSGeometryFilter.GeometryEquality(objs);
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            var service = new ThMEPEngineCoreRoomService();
            service.Initialize();
            Rooms.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                var parentId = BuildString(GroupOwner, o.Boundary);
                if (string.IsNullOrEmpty(parentId))
                {
                    var storeyInfo = Query(o.Boundary);
                    parentId = storeyInfo.Id;
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, parentId);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, o.Name);
                string privacyContent = "";
                //string LayoutContent = "";
                if (o.Tags.Count > 0)
                {
                    var labels = service.GetLabels(o);
                    if (labels.Count > 0)
                    {
                        if (service.IsPublic(labels))
                            privacyContent = "公有";
                        else if (service.IsPrivate(labels))
                            privacyContent = "私有";
                    }
                }
                //if (service.MustLayoutArea(o))
                //    LayoutContent = "必布区域";
                //else if (service.CannotLayoutArea(o))
                //    LayoutContent = "不可布区域";

                geometry.Properties.Add(ThExtractorPropertyNameManager.PrivacyPropertyName, privacyContent);
                //geometry.Properties.Add(ThExtractorPropertyNameManager.PlacementPropertyName, LayoutContent);
                geometry.Boundary = o.Boundary;
                geos.Add(geometry);
            });
            return geos;
        }

        public ThStoreyInfo Query(Entity entity)
        {
            var results = StoreyInfos.Where(o => o.Boundary.EntityContains(entity));
            return results.Count() > 0 ? results.First() : new ThStoreyInfo();
        }

        public void Set(List<ThStoreyInfo> storeyInfos)
        {
            StoreyInfos = storeyInfos;
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            var bufferService = new ThNTSBufferService();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(groupId.Keys.ToCollection());
            foreach (ThIfcRoom room in Rooms)
            {
                var newBoundary = bufferService.Buffer(room.Boundary, -1.0);
                if (newBoundary == null)
                {
                    continue;
                }
                var ids = FindCurveGroupIds(groupId, newBoundary); //包含房间的元素
                if (ids.Count == 0)
                {
                    var objs = spatialIndex.SelectCrossingPolygon(room.Boundary);
                    if (objs.Count > 0)
                    {
                        var fireApart = GetMaximumIntersectFireApart(room.Boundary, objs);
                        if (GroupOwner.ContainsKey(room.Boundary) == false)
                        {
                            GroupOwner.Add(room.Boundary, new List<string> { groupId[fireApart] });
                        }
                        else
                        {
                            GroupOwner[room.Boundary] = new List<string> { groupId[fireApart] };
                        }
                    }
                }
                else
                {
                    if (GroupOwner.ContainsKey(room.Boundary) == false)
                    {
                        GroupOwner.Add(room.Boundary, ids);
                    }
                    else
                    {
                        GroupOwner[room.Boundary] = ids;
                    }
                }
            }
        }
        private Entity GetMaximumIntersectFireApart(Entity room, DBObjectCollection fireAparts)
        {
            //获取与房间相交面积最大的防火分区
            var roomPolygon = room.ToNTSPolygonalGeometry();
            var areaDic = new Dictionary<Entity, double>();
            foreach (Entity fireApart in fireAparts)
            {
                var bufferService = new ThNTSBufferService();
                var objs = fireApart.ToNTSPolygonalGeometry().Buffer(0).ToDbCollection(true);
                double intersectArea = 0.0;
                foreach (Entity obj in objs)
                {
                    var newFireApart = bufferService.Buffer(obj, -1.0);
                    var firePolygon = newFireApart.ToNTSPolygonalGeometry();
                    foreach (Entity intersect in roomPolygon.Intersection(firePolygon).ToDbCollection())
                    {
                        if (intersect is Polyline || intersect is MPolygon)
                        {
                            intersectArea += intersect.ToNTSPolygonalGeometry().Area;
                        }
                    }
                }
                areaDic.Add(fireApart, intersectArea);
            }
            return areaDic.OrderByDescending(o => o.Value).First().Key;
        }

        public void Transform()
        {
            Rooms.ForEach(o => Transformer.Transform(o.Boundary));
        }

        public void Reset()
        {
            Rooms.ForEach(o => Transformer.Reset(o.Boundary));
        }
    }
}
