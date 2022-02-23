using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using ThMEPWSS.Sprinkler.Service;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPWSS.Sprinkler.Data
{
    public class ThSprinklerRoomExtractor : ThRoomExtractor, ITransformer
    {
        public bool IsWithHole { get; set; }
        private List<ThStoreyInfo> StoreyInfos { get; set; }

        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }

        public ThSprinklerRoomExtractor()
        {
            IsWithHole = true;
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
                rooms[i].Boundary = ThSprinklerHandleNonClosedPolylineService.Handle(rooms[i].Boundary as Polyline);
            }
            //过滤无效的房间框线
            rooms = rooms.Where(o => (o.Boundary as Polyline).Area >= 1.0).ToList();

            //造房间
            var roomBuilder = new ThRoomBuilderEngine();
            roomBuilder.Build(rooms, marks, IsWithHole);
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

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            var service = new ThSprinklerRoomService();
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
                string LayoutContent = "";
                if (service.CannotLayoutArea(o))
                {
                    LayoutContent = "不可布区域";
                }
                else
                {
                    LayoutContent = "必布区域";
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.PlacementPropertyName, LayoutContent);
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
