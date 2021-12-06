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

using ThMEPEngineCore.AreaLayout.GridLayout.Command;
using ThMEPEngineCore.AreaLayout.GridLayout.Data;
using ThMEPEngineCore.AreaLayout.CenterLineLayout.Command;

using ThMEPLighting.IlluminationLighting.Common;


namespace ThMEPLighting.IlluminationLighting.Service
{
    class ThFaAreaLayoutRoomTypeService
    {
        /// <summary>
        /// 读配置表.
        /// </summary>
        /// <param name="frameList"></param>
        /// <returns></returns>
        public static Dictionary<Polyline, ThIlluminationCommon.layoutType> getAreaLightType(List<ThGeometry> Room, Dictionary<ThGeometry, Polyline> roomFrameDict)
        {
            var frameLightType = new Dictionary<Polyline, ThIlluminationCommon.layoutType>();
            string roomConfigUrl = ThCADCommon.RoomConfigPath();
            var roomTableTree = ThIlluminationUtils.ReadRoomConfigTable(roomConfigUrl);
            var stairName = ThIlluminationCommon.stairName;
            var evacuationTag = ThIlluminationCommon.evacuationTag;
            var normalTag = ThIlluminationCommon.normalTag;


            foreach (var room in Room)
            {
                var typeInt = ThIlluminationCommon.layoutType.noName;
                var roomName = room.Properties[ThExtractorPropertyNameManager.NamePropertyName].ToString();

                if (isRoom(roomTableTree, roomName, stairName))
                {
                    typeInt = ThIlluminationCommon.layoutType.stair;
                }
                else if (roomName != "")
                {
                    var tagList = RoomConfigTreeService.getRoomTag(roomTableTree, roomName);
                    if (tagList.Contains(normalTag) && tagList.Contains(evacuationTag))
                    {
                        typeInt = ThIlluminationCommon.layoutType.normalEvac;
                    }
                    else if (tagList.Contains(normalTag))
                    {
                        typeInt = ThIlluminationCommon.layoutType.normal;
                    }
                    else if (tagList.Contains(evacuationTag))
                    {
                        typeInt = ThIlluminationCommon.layoutType.evacuation;
                    }
                }

                if (room.Boundary is MPolygon)
                {
                    frameLightType.Add(roomFrameDict[room], typeInt);
                }
                else if (room.Boundary is Polyline frame)
                {
                    frameLightType.Add(frame, typeInt);
                }
            }

            return frameLightType;

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
