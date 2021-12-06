using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;

using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Stair;
using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.FireAlarmArea.Model;

namespace ThMEPElectrical.FireAlarmArea.Service
{
    class ThFASmokeStairService
    {
        /// <summary>
        /// 楼梯部分布置
        /// 最终结果点位写到layoutParameter
        /// 返回最终布置点位，方向
        /// </summary>
        /// <param name="dataQuery"></param>
        /// <param name="layoutParameter"></param>
        /// <returns></returns>
        public static List<ThLayoutPt> LayoutStair(ThAFASSmokeLayoutParameter layoutParameter)
        {
            var transformer = layoutParameter.transformer;
            var pts = layoutParameter.framePts;
            var scale = layoutParameter.Scale;
            var stairNormalPts = new List<Point3d>();
            var stairEmgPts = new List<Point3d>();
            var resultPts = new List<ThLayoutPt>();

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //boundary 到原位置
                var stairBoundary = layoutParameter.RoomType.Where(x => x.Value == ThFaSmokeCommon.layoutType.stair).Select(x => x.Key).ToList();
                stairBoundary.ForEach(x => transformer.Reset(x));
                var stairEngine = new ThStairEquimentLayout();
                var stairFireDetector = stairEngine.StairFireDetector(acadDatabase.Database, stairBoundary, pts, scale);
                var stairFirePts = stairFireDetector.Select(x => x.Key).ToList();
                foreach (var r in stairFireDetector)
                {
                    resultPts.Add(new ThLayoutPt() { Pt = r.Key, Angle = r.Value, BlkName = layoutParameter.BlkNameSmoke });
                }

                //楼梯间结果，楼梯房间框线转到原点位置
                stairBoundary.ForEach(x => transformer.Transform(x));
                stairFirePts = stairFirePts.Select(x => transformer.Transform(x)).ToList();

                layoutParameter.StairPartResult.AddRange(stairFirePts);
                ////

                return resultPts;
            }
        }

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
