using System;
using NFox.Cad;
using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Engine;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.IO;
using ThMEPElectrical.FireAlarm.Interfacce;

namespace FireAlarm.Data
{
    public class ThFaRoomExtractor : ThRoomExtractor, IGroup, ISetStorey
    {
        private List<ThStoreyInfo> StoreyInfos { get; set; }

        public ThFaRoomExtractor()
        {
            StoreyInfos = new List<ThStoreyInfo>();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            //获取本地的房间框线
            var roomEngine = new ThRoomOutlineRecognitionEngine();
            roomEngine.RecognizeMS(database, pts);
            var rooms = roomEngine.Elements.Cast<ThIfcRoom>().ToList();
            //对于起、终点间距小于一定距离的，做缝合
            rooms.ForEach(o => o.Boundary = ThHandleNonClosedPolylineService.Handle(o.Boundary as Polyline));
            //过滤无效的房间框线
            rooms = rooms.Where(o => (o.Boundary as Polyline).Area >= 1.0).ToList();

            //获取本地的房间标注
            var markEngine = new ThRoomMarkRecognitionEngine();
            markEngine.RecognizeMS(database, pts);
            var marks = markEngine.Elements.Cast<ThIfcTextNote>().ToList();

            //造房间
            var roomBuilder = new ThRoomBuilderEngine();
            roomBuilder.Build(rooms, marks);
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

        private string GetName(List<string> tags)
        {
            var group = tags.GroupBy(o => o);
            var first = group.OrderByDescending(o => o.Count()).First();
            if (first.Count() > 1)
            {
                return first.Key;
            }
            return string.Join(";", tags.ToArray());
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
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
                geometry.Boundary = o.Boundary;
                geos.Add(geometry);
            });
            return geos;
        }

        public ThStoreyInfo Query(Entity entity)
        {
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity));
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
                var ids = FindCurveGroupIds(groupId, newBoundary); //包含房间的元素
                if (ids.Count == 0)
                {
                    var objs = spatialIndex.SelectCrossingPolygon(room.Boundary);
                    if(objs.Count>0)
                    {
                        var fireApart = GetMaximumIntersectFireApart(room.Boundary, objs);
                        GroupOwner.Add(room.Boundary, new List<string> { groupId[fireApart] });
                    }
                }
                else
                {
                    GroupOwner.Add(room.Boundary, ids);
                }
            }
        }
        private Entity GetMaximumIntersectFireApart(Entity room,DBObjectCollection fireAparts)
        {
            //获取与房间相交面积最大的防火分区
            var roomPolygon = room.ToNTSPolygon();
            var areaDic = new Dictionary<Entity, double>();
            foreach(Entity fireApart in fireAparts)
            {
                var firePolygon = fireApart.ToNTSPolygon();
                double intersectArea = 0.0;
                foreach(Entity intersect in roomPolygon.Intersection(firePolygon).ToDbCollection())
                {
                    intersectArea += intersect.ToNTSPolygon().Area;
                }
                areaDic.Add(fireApart, intersectArea);
            }
            return areaDic.OrderByDescending(o => o.Value).First().Key;
        }
    }
}
