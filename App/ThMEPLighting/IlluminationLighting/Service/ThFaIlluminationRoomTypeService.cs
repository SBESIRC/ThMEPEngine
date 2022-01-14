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

using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS;


namespace ThMEPLighting.IlluminationLighting.Service
{
    class ThFaIlluminationRoomTypeService
    {
        /// <summary>
        /// 读配置表.
        /// </summary>
        /// <param name="frameList"></param>
        /// <returns></returns>
        public static Dictionary<Polyline, ThIlluminationCommon.LayoutType> GetIllunimationType(List<ThGeometry> Room, Dictionary<ThGeometry, Polyline> roomFrameDict)
        {
            var frameLightType = new Dictionary<Polyline, ThIlluminationCommon.LayoutType>();
            string roomConfigUrl = ThCADCommon.RoomConfigPath();
            var roomTableTree = ThAFASRoomUtils.ReadRoomConfigTable(roomConfigUrl);
            var stairName = ThFaCommon.stairName;
            var evacuationTag = ThIlluminationCommon.EvacuationTag;
            var normalTag = ThIlluminationCommon.NormalTag;

            foreach (var room in Room)
            {
                var typeInt = ThIlluminationCommon.LayoutType.noName;
                var roomName = room.Properties[ThExtractorPropertyNameManager.NamePropertyName].ToString();

                if (ThAFASRoomUtils.IsRoom(roomTableTree, roomName, stairName))
                {
                    typeInt = ThIlluminationCommon.LayoutType.stair;
                }
                else if (roomName != "")
                {
                    var tagList = RoomConfigTreeService.GetRoomTag(roomTableTree, roomName);
                    if (tagList.Contains(normalTag) && tagList.Contains(evacuationTag))
                    {
                        typeInt = ThIlluminationCommon.LayoutType.normalEvac;
                    }
                    else if (tagList.Contains(normalTag))
                    {
                        typeInt = ThIlluminationCommon.LayoutType.normal;
                    }
                    else if (tagList.Contains(evacuationTag))
                    {
                        typeInt = ThIlluminationCommon.LayoutType.evacuation;
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
    }
}
