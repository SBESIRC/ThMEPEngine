using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using Linq2Acad;
using ThCADCore.NTS;
using AcHelper;
using Dreambuild.AutoCAD;
using ThCADExtension;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;

using ThMEPHVAC.FloorHeatingCoil.Cmd;
using ThMEPHVAC.FloorHeatingCoil.Data;
using ThMEPHVAC.FloorHeatingCoil.Service;
using ThMEPHVAC.FloorHeatingCoil.Model;
using ThMEPHVAC.FloorHeatingCoil.Heating;
using Autodesk.AutoCAD.EditorInput;
using System.Text.RegularExpressions;

namespace ThMEPHVAC.FloorHeatingCoil.Service
{
    internal class ThFloorHeatingCoilUtilServices
    {
        public static ThFloorHeatingDataProcessService GetData(AcadDatabase acadDatabase, List<Polyline> selectFrames, ThMEPOriginTransformer transformer)
        {
            var dataFactory = new ThFloorHeatingDataFactory()
            {
                Transformer = transformer,
            };
            dataFactory.GetElements(acadDatabase.Database, new Point3dCollection());

            var dataQuery = new ThFloorHeatingDataProcessService()
            {
                WithUI = ThFloorHeatingCoilSetting.Instance.WithUI,
                InputExtractors = dataFactory.Extractors,
                FurnitureObstacleData = dataFactory.SanitaryTerminal,
                RoomSeparateLine = dataFactory.RoomSeparateLine,
                //RoomSuggestDist = dataFactory.RoomSuggestDist,
                WaterSeparatorData = dataFactory.WaterSeparator,
                BathRadiatorData = dataFactory.BathRadiator,
                FurnitureObstacleDataTemp = dataFactory.SenitaryTerminalOBBTemp,

            };
            dataQuery.ProcessDataWithRoom(selectFrames);

            return dataQuery;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="power"></param>
        /// <returns></returns>
        public static double GetNumberFromString(string power)
        {
            double resDouble = -1;
            if (power != null)
            {
                var reg = new Regex(@"[0-9]*[.]?[0-9]+");

                var str = reg.Match(power);
                if (str.Success)
                {
                    resDouble = double.Parse(str.Value);
                }
            }
            return resDouble;
        }

        public static void PassUserParameter(ThFloorHeatingCoilViewModel vm)
        {
            Parameter.PublicRegionConstraint = Convert.ToBoolean(vm.PublicRegionConstraint);
            Parameter.IndependentRoomConstraint = Convert.ToBoolean(vm.IndependentRoomConstraint);
            Parameter.AuxiliaryRoomConstraint = Convert.ToBoolean(vm.AuxiliaryRoomConstraint);
            Parameter.PrivatePublicMode = vm.PrivatePublicMode;
            Parameter.TotalLength = vm.TotalLenthConstraint * 1000;

          //  Parameter.KeyRoomShortSide = vm.MainRoomEdgeTol;



        }

        public static void PairRoomWithRoomSuggest(ref List<ThRoomSetModel> roomSet, Dictionary<Polyline, BlockReference> roomPlSuggestDict, double suggestDistDefualt)
        {
            var roomset = roomSet[0];
            foreach (var room in roomset.Room)
            {
                var suggest = roomPlSuggestDict[room.RoomBoundary];
                if (suggest != null)
                {
                    ThFloorHeatingDataProcessService.GetSuggestData(suggest, out var route, out var suggestDist, out var length);
                    room.SetSuggestDist(suggestDist);
                }
                else
                {
                    room.SetSuggestDist(suggestDistDefualt);
                }
            }
        }

        public static Dictionary<Polyline, BlockReference> PairRoomPlWithRoomSuggest(List<ThFloorHeatingRoom> roomList, List<BlockReference> roomSuggest)
        {
            var roomSuggestDict = new Dictionary<Polyline, BlockReference>();
            foreach (var room in roomList)
            {
                roomSuggestDict.Add(room.RoomBoundary, null);
                var roomCenter = room.RoomBoundary.GetCenter();
                var suggestInRoom = roomSuggest.Where(x => room.RoomBoundary.Contains(x.Position)).ToList();
                if (suggestInRoom.Any())
                {
                    roomSuggestDict[room.RoomBoundary] = suggestInRoom.First();
                }
                else
                {
                    var suggestInOriRoom = roomSuggest.Where(x => room.OriginalBoundary.Contains(x.Position)).ToList();
                    if (suggestInOriRoom.Any())
                    {
                        var minDist = 2000.0;
                        foreach (var suggest in suggestInOriRoom)
                        {
                            var dist = suggest.Position.DistanceTo(roomCenter);
                            if (dist <= minDist)
                            {
                                minDist = dist;
                                roomSuggestDict[room.RoomBoundary] = suggest;
                            }
                        }
                    }
                }
            }
            return roomSuggestDict;

        }
    }
}
