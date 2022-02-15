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

using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.FireAlarmArea;

namespace ThMEPElectrical.FireAlarmArea.Service
{
    class ThFaGasRoomTypeService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Room"></param>
        /// <param name="roomFrameDict"></param>
        /// <returns></returns>
        public static Dictionary<Polyline, ThFaSmokeCommon.layoutType> GetGasSensorType(List<ThGeometry> Room, Dictionary<ThGeometry, Polyline> roomFrameDict)
        {
            var frameSensorType = new Dictionary<Polyline, ThFaSmokeCommon.layoutType>();
            string roomConfigUrl = ThCADCommon.RoomConfigPath();
            var roomTableTree = ThAFASRoomUtils.ReadRoomConfigTable(roomConfigUrl);
            var gasTag = ThFaSmokeCommon.gasTag;
            var expPrfTag = ThFaSmokeCommon.expPrfTag;

            foreach (var room in Room)
            {
                var typeInt = ThFaSmokeCommon.layoutType.noName;
                var roomName = room.Properties[ThExtractorPropertyNameManager.NamePropertyName].ToString();

                if (typeInt != 0 && roomName != "")
                {
                    var tagList = RoomConfigTreeService.GetRoomTag(roomTableTree, roomName);
                    if (tagList.Contains(expPrfTag) && tagList.Contains(gasTag))
                    {
                        typeInt = ThFaSmokeCommon.layoutType.gasPrf;
                    }
                    else if (tagList.Contains(gasTag))
                    {
                        typeInt = ThFaSmokeCommon.layoutType.gas;
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

                var printPt = roomFrameDict[room].GetPoint3dAt(0);
                ThMEPEngineCore.Diagnostics.DrawUtils.ShowGeometry(new Autodesk.AutoCAD.Geometry.Point3d(printPt.X, printPt.Y - 300, 0), string.Format("roomTypeGas:{0}", typeInt.ToString()), "l0roomTypeGas", 121, 25, 200);

            }

            return frameSensorType;
        }
    }
}
