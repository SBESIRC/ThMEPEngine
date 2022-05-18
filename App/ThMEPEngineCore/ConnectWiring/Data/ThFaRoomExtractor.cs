using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.ConnectWiring.Data
{
    public class ThFaRoomExtractor : ThRoomExtractor
    {
        static string roomConfigUrl = ThCADCommon.SupportPath() + "\\房间名称分类处理.xlsx";
        static string RoomNameControl = "房间名称处理";
        List<RoomTableTree> roomTableConfig = null;
        public List<Polyline> holes = new List<Polyline>();
        List<string> holeRooms = new List<string>() {
            "楼梯间",
            "井道",
            "人防井道",
            "天井",
        };
        public override void Extract(Database database, Point3dCollection pts)
        {
            //读取数据
            base.Extract(database, pts);
        }

        public override List<ThGeometry> BuildGeometries()
        {
            //读取配置表
            ReadRoomConfigTable();
            //获取需要当作洞口的房间名
            var allHoleRooms = roomTableConfig.CalRoomLst(holeRooms);
            //数据处理
            var geos = new List<ThGeometry>();
            foreach (var room in Rooms)
            {
                var roomInfos = GetMPolygonInfo(room.Boundary);
                holes.AddRange(roomInfos.Value);
                if (allHoleRooms.Any(x=> room.Tags.Any(y=> RoomConfigTreeService.CompareRoom(x, y))))
                {
                    holes.Add(roomInfos.Key);
                }
                else
                {
                    var geometry = new ThGeometry();
                    if (room.Tags.Count > 0 && !RoomConfigTreeService.IsPublicRoom(roomTableConfig, room.Tags[0]))
                    {
                        geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                        geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, room.Name);
                        geometry.Boundary = roomInfos.Key;
                        geos.Add(geometry);
                    }
                }
            }

            return geos;
        }

        private KeyValuePair<Polyline, List<Polyline>> GetMPolygonInfo(Entity entity)
        {
            List<Polyline> resHoles = new List<Polyline>();
            Polyline polyFrame = null;
            if (entity is Polyline polyline)
            {
                polyFrame = polyline;
            }
            else if (entity is MPolygon mPolygon)
            {
                for (int i = 0; i < mPolygon.Loops().Count; i++)
                {
                    if (i == 0) polyFrame = mPolygon.Loops()[i] as Polyline;
                    else resHoles.Add(mPolygon.Loops()[i]);
                }
            }
            //polyFrame = polyFrame.Buffer(1000)[0] as Polyline;
            return new KeyValuePair<Polyline, List<Polyline>>(polyFrame, resHoles);
        }

        /// <summary>
        /// 读取房间配置表
        /// </summary>
        private void ReadRoomConfigTable()
        {
            ReadExcelService excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(roomConfigUrl, true);
            var table = dataSet.Tables[RoomNameControl];
            if (table != null)
            {
                roomTableConfig = RoomConfigTreeService.CreateRoomTree(table);
            }
        }
    }
}
