using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Config;

using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.FireAlarmSmokeHeat;

namespace ThMEPElectrical.FireAlarmCombustibleGas.Service
{
    class ThFaAreaLayoutRoomTypeService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Room"></param>
        /// <param name="roomFrameDict"></param>
        /// <returns></returns>
        public static Dictionary<Polyline, ThFaSmokeCommon.layoutType> GetAreaSensorType(List<ThGeometry> Room, Dictionary<ThGeometry, Polyline> roomFrameDict)
        {
            var frameSensorType = new Dictionary<Polyline, ThFaSmokeCommon.layoutType>();
            string roomConfigUrl = ThCADCommon.SupportPath() + "\\房间名称分类处理.xlsx";
            var roomTableTree = ThFireAlarmUtils.ReadRoomConfigTable(roomConfigUrl);
            var gasTag = ThFaSmokeCommon.gasTag;
            var expPrfTag = ThFaSmokeCommon.expPrfTag;
            var nonLayoutTag = ThFaSmokeCommon.nonLayoutTag;

            foreach (var room in Room)
            {
                var typeInt = ThFaSmokeCommon.layoutType.noName;
                var roomName = room.Properties[ThExtractorPropertyNameManager.NamePropertyName].ToString();

                if (typeInt != 0 && roomName != "")
                {
                    var tagList = RoomConfigTreeService.getRoomTag(roomTableTree, roomName);
                    if(tagList.Contains(expPrfTag) && tagList.Contains(gasTag))
                    {
                        typeInt = ThFaSmokeCommon.layoutType.gasPrf;
                    } else if (tagList.Contains(gasTag))
                    {
                        typeInt = ThFaSmokeCommon.layoutType.gas;
                    } else if (tagList.Contains(nonLayoutTag))
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
    }
}
