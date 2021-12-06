using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Config;

using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS;

namespace ThMEPElectrical.FireAlarmArea.Service
{
    class ThFaSmokeRoomTypeService
    {
        /// <summary>
        /// 读配置表.value:0:楼梯，1：烟感，2：温感，3：烟温感，4:非布置，5：找不到tag（当成非布置处理）
        /// </summary>
        /// <param name="frameList"></param>
        /// <returns></returns>
        public static Dictionary<Polyline, ThFaSmokeCommon.layoutType> GetSmokeSensorType(List<ThGeometry> Room, Dictionary<ThGeometry, Polyline> roomFrameDict)
        {
            var frameSensorType = new Dictionary<Polyline, ThFaSmokeCommon.layoutType>();
            string roomConfigUrl = ThCADCommon.RoomConfigPath();
            var roomTableTree = ThAFASRoomUtils.ReadRoomConfigTable(roomConfigUrl);
            var stairName = ThFaCommon.stairName;
            var smokeTag = ThFaSmokeCommon.smokeTag;
            var heatTag = ThFaSmokeCommon.heatTag;
            var prfTag = ThFaSmokeCommon.expPrfTag;
            var nonLayoutTag = ThFaSmokeCommon.nonLayoutTag;

            foreach (var room in Room)
            {
                var typeInt = ThFaSmokeCommon.layoutType.noName;
                var roomName = room.Properties[ThExtractorPropertyNameManager.NamePropertyName].ToString();

                if (ThAFASRoomUtils.IsRoom(roomTableTree, roomName, stairName))
                {
                    typeInt = ThFaSmokeCommon.layoutType.stair;
                }
                else if (roomName != "")
                {
                    var tagList = RoomConfigTreeService.getRoomTag(roomTableTree, roomName);
                    if (tagList.Contains(smokeTag) && tagList.Contains(heatTag) && tagList.Contains(prfTag))
                    {
                        typeInt = ThFaSmokeCommon.layoutType.smokeHeatPrf;
                    }
                    else if (tagList.Contains(smokeTag) && tagList.Contains(heatTag))
                    {
                        typeInt = ThFaSmokeCommon.layoutType.smokeHeat;
                    }
                    else if (tagList.Contains(smokeTag) && tagList.Contains(prfTag))
                    {
                        typeInt = ThFaSmokeCommon.layoutType.smokePrf;
                    }
                    else if (tagList.Contains(smokeTag))
                    {
                        typeInt = ThFaSmokeCommon.layoutType.smoke;
                    }
                    else if (tagList.Contains(heatTag) && tagList.Contains(prfTag))
                    {
                        typeInt = ThFaSmokeCommon.layoutType.heatPrf;
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

                //如果没找到名字。或者名字没有明确不布置tag ，默认布置烟感
                if (typeInt == ThFaSmokeCommon.layoutType.noName)
                {
                    typeInt = ThFaSmokeCommon.layoutType.smoke;
                }


                frameSensorType.Add(roomFrameDict[room], typeInt);
                DrawUtils.ShowGeometry(roomFrameDict[room].GetPoint3dAt(0), string.Format("roomName:{0}", roomName), "l0roomName", 121, 25, 200);

            }

            return frameSensorType;

        }



    }
}
