using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using Linq2Acad;
using ThCADCore.NTS;
using AcHelper;
using Dreambuild.AutoCAD;
using ThCADExtension;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor.Service;

using ThMEPHVAC.FloorHeatingCoil.Cmd;
using ThMEPHVAC.FloorHeatingCoil.Data;
using ThMEPHVAC.FloorHeatingCoil.Service;
using ThMEPHVAC.FloorHeatingCoil.Model;
using ThMEPHVAC.FloorHeatingCoil.Heating;
using Autodesk.AutoCAD.EditorInput;
using System.Text.RegularExpressions;

namespace ThMEPHVAC.FloorHeatingCoil.Service
{
    internal static class ThFloorHeatingCoilUtilServices
    {
        /// <summary>
        /// debugMode: true:return (0,0,0) false:return far away point transformer
        /// </summary>
        /// <param name="selectFrames"></param>
        /// <param name="debugMode"></param>
        /// <returns></returns>
        public static ThMEPOriginTransformer GetTransformer(List<Polyline> selectFrames, bool debugMode = false)
        {
            var transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));

            if (debugMode == false)
            {
                foreach (var frame in selectFrames)
                {
                    for (int i = 0; i < frame.NumberOfVertices; i++)
                    {
                        var p0 = frame.GetPoint3dAt((i + frame.NumberOfVertices - 1) % frame.NumberOfVertices);
                        var p1 = frame.GetPoint3dAt(i % frame.NumberOfVertices);
                        var p2 = frame.GetPoint3dAt((i + 1) % frame.NumberOfVertices);

                        var dir = (p1 - p0).GetNormal();
                        var dir2 = (p2 - p1).GetNormal();

                        var angle = dir.GetAngleTo(dir2);

                        if (Math.Abs(Math.Cos(angle)) < Math.Cos(Math.PI * 89 / 180))
                        {
                            transformer = new ThMEPOriginTransformer(p1);
                            break;
                        }
                    }
                    if (transformer.Displacement != Matrix3d.Identity)
                    {
                        break;
                    }
                }
            }

            return transformer;
        }
        public static ThFloorHeatingDataProcessService GetData(AcadDatabase acadDatabase, List<Polyline> selectFrames, ThMEPOriginTransformer transformer, bool withUI)
        {
            var dataFactory = new ThFloorHeatingDataFactory()
            {
                Transformer = transformer,
            };
            dataFactory.GetElements(acadDatabase.Database, new Point3dCollection());

            var dataQuery = new ThFloorHeatingDataProcessService()
            {
                WithUI = withUI,
                Transformer = transformer,
                InputExtractors = dataFactory.Extractors,
                RoomSeparateLine = dataFactory.RoomSeparateLine,
                WaterSeparatorData = dataFactory.WaterSeparator,
                BathRadiatorData = dataFactory.BathRadiator,
                FurnitureObstacle = dataFactory.ObstacleObb,

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roomList"></param>
        /// <param name="roomSuggest"></param>
        /// <param name="roomSuggestDict">kay: room boundary, value: ori blk</param>
        public static void PairRoomPlWithRoomSuggest(List<ThFloorHeatingRoom> roomList, Dictionary<BlockReference, BlockReference> roomSuggest, ref Dictionary<Polyline, BlockReference> roomSuggestDict)
        {
            roomSuggestDict = new Dictionary<Polyline, BlockReference>();
            var roomSearchOriginal = new List<ThFloorHeatingRoom>();

            var suggestList = roomSuggest.Select(x => x.Key).ToList();

            foreach (var room in roomList)
            {
                roomSuggestDict.Add(room.RoomBoundary, null);
                var roomCenter = room.RoomBoundary.GetCenterInPolyline();
                var suggestInRoom = suggestList.Where(x => room.RoomBoundary.Contains(x.Position)).ToList();
                if (suggestInRoom.Any())
                {
                    var suggest = suggestInRoom.OrderBy(x => x.Position.DistanceTo(roomCenter)).First();
                    var oriSuggest = roomSuggest[suggest];
                    if (oriSuggest != null)
                    {
                        roomSuggestDict[room.RoomBoundary] = oriSuggest;
                    }
                }
                else
                {
                    roomSearchOriginal.Add(room);
                }

            }

            suggestList = suggestList.Except(roomSuggestDict.Select(x => x.Value)).ToList();

            foreach (var room in roomSearchOriginal)
            {
                var suggestInOriRoom = suggestList.Where(x => room.OriginalBoundary.Contains(x.Position)).ToList();
                var roomCenter = room.RoomBoundary.GetCenterInPolyline();
                if (suggestInOriRoom.Any())
                {
                    var minDist = 2000.0;
                    foreach (var suggest in suggestInOriRoom)
                    {
                        var dist = suggest.Position.DistanceTo(roomCenter);
                        if (dist <= minDist)
                        {
                            minDist = dist;
                            var oriSuggest = roomSuggest[suggest];
                            if (oriSuggest != null)
                            {
                                roomSuggestDict[room.RoomBoundary] = oriSuggest;
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// key: transformed blk clone, value: original blk (for update)
        /// </summary>
        /// <param name="database"></param>
        /// <param name="transformer"></param>
        /// <returns></trans></returns>
        public static Dictionary<BlockReference, BlockReference> GetRoomSuggestData(Database database, ThMEPOriginTransformer transformer)
        {
            var blkDict = new Dictionary<BlockReference, BlockReference>();

            var extractService = new ThExtractBlockReferenceService()
            {
                BlockName = ThFloorHeatingCommon.BlkName_RoomSuggest,
            };
            extractService.Extract(database, new Point3dCollection());
            var roomRouteSuggestBlk = extractService.Blocks.ToList();

            foreach (var blk in roomRouteSuggestBlk)
            {
                var transblk = blk.Clone() as BlockReference;
                transformer.Transform(transblk);
                blkDict.Add(transblk, blk);
            }

            return blkDict;
        }

        public static Point3d GetCenterInPolyline(this Polyline shell)
        {
            var pt = shell.GetCenter();
            if (shell.Contains(pt) == false)
            {
                pt = shell.GetMaximumInscribedCircleCenter();
            }

            return pt;

        }
    }
}
