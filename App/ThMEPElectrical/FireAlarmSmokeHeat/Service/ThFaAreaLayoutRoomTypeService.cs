using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;
using GeometryExtensions;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.IO.GeoJSON;
using ThMEPEngineCore.Config;

using ThMEPElectrical.AlarmSensorLayout.Command;
using ThMEPElectrical.AlarmSensorLayout.Data;
using ThMEPElectrical.AlarmLayout.Command;

using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.FireAlarmSmokeHeat.Data;


namespace ThMEPElectrical.FireAlarmSmokeHeat.Service
{
    class ThFaAreaLayoutRoomTypeService
    {
        /// <summary>
        /// 读配置表.value:0:楼梯，1：烟感，2：温感，3：烟温感，4:非布置，5：找不到tag（当成非布置处理）
        /// </summary>
        /// <param name="frameList"></param>
        /// <returns></returns>
        public static Dictionary<Polyline, ThFaSmokeCommon.layoutType> getAreaSensorType(List<ThGeometry> Room, Dictionary<ThGeometry, Polyline> roomFrameDict)
        {
            var frameSensorType = new Dictionary<Polyline, ThFaSmokeCommon.layoutType>();
            string roomConfigUrl = ThCADCommon.SupportPath() + "\\房间名称分类处理.xlsx";
            var roomTableTree = ThFireAlarmUtils.ReadRoomConfigTable(roomConfigUrl);
            var stairName = ThFaSmokeCommon.stairName;
            var smokeTag = ThFaSmokeCommon.smokeTag;
            var heatTag = ThFaSmokeCommon.heatTag;
            var nonLayoutTag = ThFaSmokeCommon.nonLayoutTag;

            foreach (var room in Room)
            {
                var typeInt = ThFaSmokeCommon.layoutType.noName;
                var roomName = room.Properties[ThExtractorPropertyNameManager.NamePropertyName].ToString();

                if (isRoom(roomTableTree, roomName, stairName))
                {
                    typeInt = ThFaSmokeCommon.layoutType.stair;
                }
                if (typeInt != 0 && roomName != "")
                {
                    var tagList = RoomConfigTreeService.getRoomTag(roomTableTree, roomName);
                    if (tagList.Contains(smokeTag) && tagList.Contains(heatTag))
                    {
                        typeInt = ThFaSmokeCommon.layoutType.smokeHeat;
                    }
                    else if (tagList.Contains(smokeTag))
                    {
                        typeInt = ThFaSmokeCommon.layoutType.smoke;
                    }
                    else if (tagList.Contains(heatTag))
                    {
                        typeInt = ThFaSmokeCommon.layoutType.heat;
                    }
                    else if (tagList.Contains(nonLayoutTag))
                    {
                        typeInt = ThFaSmokeCommon.layoutType.nonLayout;
                    }
                }

                if (room.Boundary is MPolygon)
                {
                    frameSensorType.Add(roomFrameDict[room], typeInt);
                }
                else if (room.Boundary is Polyline frame)
                {
                    frameSensorType.Add(frame, typeInt);
                }
            }

            return frameSensorType;

        }

        private static bool isRoom(List<RoomTableTree> roomTableTree, string name, string standardName)
        {
            var bReturn = false;
            var nameList = RoomConfigTreeService.CalRoomLst(roomTableTree, standardName);

            if (nameList.Contains(name))
            {
                bReturn = true;
            }
            return bReturn;
        }

    }
}
