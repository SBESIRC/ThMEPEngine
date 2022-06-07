using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPHVAC.FloorHeatingCoil.Data
{
    public class ThFloorHeatingRoomExtractor : ThRoomExtractor, ITransformer
    {
        public bool IsWithHole { get; set; }

        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }

        public ThFloorHeatingRoomExtractor()
        {
            IsWithHole = true;
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            //获取本地的房间框线
            var roomOutlineExtraction = new ThAIRoomOutlineExtractionEngine();
            roomOutlineExtraction.ExtractFromMS(database);
            roomOutlineExtraction.Results.ForEach(o => Transformer.Transform(o.Geometry));

            ////获取本地的房间标注
            //var roomMarkExtraction = new ThAIRoomMarkExtractionEngine();
            //roomMarkExtraction.ExtractFromMS(database);
            //roomMarkExtraction.Results.ForEach(o => Transformer.Transform(o.Geometry));

            var newPts = Transformer.Transform(pts);
            var roomEngine = new ThAIRoomOutlineRecognitionEngine();
            roomEngine.Recognize(roomOutlineExtraction.Results, newPts);
            var rooms = roomEngine.Elements.Cast<ThIfcRoom>().ToList();
            //var markEngine = new ThAIRoomMarkRecognitionEngine();
            //markEngine.Recognize(roomMarkExtraction.Results, newPts);
            //var marks = markEngine.Elements.Cast<ThIfcTextNote>().ToList();

            //对于起、终点间距小于一定距离的，做缝合
            for (int i = 0; i < rooms.Count; i++)
            {
                rooms[i].Boundary = ThHVACHandleNonClosedPolylineService.Handle(rooms[i].Boundary as Polyline);
            }
            //过滤无效的房间框线
            rooms = rooms.Where(o => (o.Boundary as Polyline).Area >= 1.0).ToList();

            //造房间
            var roomBuilder = new ThRoomBuilderEngine();
            var marks = new List<ThIfcTextNote>();
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
            Rooms.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, o.Name);
                geometry.Boundary = o.Boundary;
                geos.Add(geometry);
            });
            return geos;
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
