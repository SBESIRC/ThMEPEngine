using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.IO.ExcelService;

namespace ThMEPEngineCore.ConnectWiring.Data
{
    public class ThFaRoomExtractor : ThRoomExtractor
    {
        static string roomConfigUrl = ThCADCommon.SupportPath() + "\\房间名称分类处理.xlsx";
        static string RoomNameControl = "房间名称处理";
        List<RoomTableTree> roomTableConfig = null;
        public List<Polyline> holes = new List<Polyline>();
        public override void Extract(Database database, Point3dCollection pts)
        {
            //读取数据
            base.Extract(database, pts);
            //读取配置表
            ReadRoomConfigTable();
            //数据处理
            List<Polyline> roomPolys = new List<Polyline>();
            foreach (var room in Rooms)
            {
                if (room.Tags.Count > 0 && !RoomConfigTreeService.IsPublicRoom(roomTableConfig, room.Tags[0]))
                {
                    var roomInfos = GetMPolygonInfo(room.Boundary);
                    roomPolys.Add(roomInfos.Key);
                    holes.AddRange(roomInfos.Value);
                }
            }
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
