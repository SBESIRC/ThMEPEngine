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

namespace ThMEPHVAC.FloorHeatingCoil.Engine
{
    public class ThFloorHeatingCreateSingleRegionEngin
    {
        public static ThFloorHeatingDataProcessService CreateSRData(ThFloorHeatingCoilViewModel vm)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                if (vm.SelectFrame.Count == 0)
                {
                    ThSelectFrameUtil.SelectPolyline().ForEach(x => vm.SelectFrame.Add(x));
                }
                var selectFrames = vm.SelectFrame.ToList();
                if (selectFrames.Count == 0)
                {
                    return new ThFloorHeatingDataProcessService();
                }

                var transformer = new ThMEPOriginTransformer(selectFrames[0].GetPoint3dAt(0));
                transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));

                var dataQuery = ThFloorHeatingCoilUtilServices.GetData(acadDatabase, selectFrames, transformer);

                return dataQuery;
            }
        }

        //private static void PairRoomWithRoomSuggest(ref List<ThRoomSetModel> roomSet, List<BlockReference> roomSuggest, double suggestDistDefualt)
        //{
        //    var roomset = roomSet[0];
        //    foreach (var room in roomset.Room)
        //    {
        //        var suggestInRoom = roomSuggest.Where(x => room.RoomBoundary.Contains(x.Position)).ToList();
        //        if (suggestInRoom.Any())
        //        {
        //            ThFloorHeatingDataProcessService.GetSuggestData(suggestInRoom[0], out var route, out var suggestDist, out var length);
        //            room.SetSuggestDist(suggestDist);
        //        }
        //        else
        //        {
        //            room.SetSuggestDist(suggestDistDefualt);
        //        }
        //    }
        //}

        public static bool CheckValidDataSet(List<ThRoomSetModel> roomSet)
        {
            if (roomSet.Count == 0)
            {
                return false;
            }

            if (roomSet[0].Room.Count == 0)
            {
                return false;
            }

            if (roomSet[0].WaterSeparator == null)
            {
                return false;
            }
            return true;
        }

    }

    public class ThFloorHeatingUpdateSingleRegionEngine
    {
        public static bool PairSingleRegionWithRoomSuggest(ref List<SingleRegion> singleRegion, Dictionary<Polyline, BlockReference> roomPlSuggestDict, double suggestDistDefualt)
        {
            var needUpdateSR = false;
            var noRoomSuggestRegion = new List<SingleRegion>();

            foreach (var sr in singleRegion)
            {
                var suggest = roomPlSuggestDict[sr.OriginalPl];
                if (suggest != null)
                {
                    ThFloorHeatingDataProcessService.GetSuggestData(suggest, out var route, out var suggestDist, out var length);
                    if (suggestDist != -1 && sr.SuggestDist != suggestDist)
                    {
                        sr.SuggestDist = suggestDist;
                        needUpdateSR = true;
                    }
                    if (route != -1)
                    {
                        if (sr.MainPipe.Count > 0 && sr.MainPipe[0] != (int)route)
                        {
                            needUpdateSR = true;
                            sr.MainPipe.Clear();
                            sr.MainPipe.Add((int)route);
                        }
                        if (sr.MainPipe.Count == 0)
                        {
                            needUpdateSR = true;
                            sr.MainPipe.Add((int)route);
                        }
                    }
                    else
                    {
                        //如果重新分配但用户有的没指定，待考虑
                        //noRoomSuggestRegion.Add(sr);
                    }
                }
                else
                {
                    noRoomSuggestRegion.Add(sr);
                }
            }

            if (needUpdateSR == true)
            {
                //只有发现需要更新的时候，匹配不到的块才清空，否则全自动且有房间没放，就一直会刷新了
                foreach (var sr in noRoomSuggestRegion)
                {
                    sr.MainPipe.Clear();
                    sr.SuggestDist = suggestDistDefualt;
                }
            }

            return needUpdateSR;

        }

        //public static void UpdateSRSuggestBlock(ref List<BlockReference> roomSuggest, List<SingleRegion> singleRegion)
        //{
        //    foreach (var sr in singleRegion)
        //    {
        //        var suggestDist = sr.SuggestDist;
        //        var route = -1;
        //        var length = 0.0;
        //        if (sr.MainPipe.Count > 0)
        //        {
        //            route = sr.MainPipe[0];
        //            length = ProcessedData.PipeList[sr.MainPipe[0]].ResultPolys.Sum(x => x.Length);
        //            length = Math.Round(length / 1000, MidpointRounding.AwayFromZero);
        //        }

        //        var suggestInRoom = roomSuggest.Where(x => sr.ClearedPl.Contains(x.Position)).ToList();
        //        if (suggestInRoom.Any())
        //        {
        //            var blk = suggestInRoom[0];
        //            ThFloorHeatingCoilInsertService.UpdateSuggestBlock(blk, route + 1, suggestDist, length, true);
        //        }
        //        else
        //        {
        //            var insertPt = sr.ClearedPl.GetCenter();
        //            ThFloorHeatingCoilInsertService.InsertSuggestBlock(insertPt, route + 1, suggestDist, length, ThFloorHeatingCommon.BlkName_RoomSuggest, true);
        //        }
        //    }
        //}

        public static void UpdateSRSuggestBlock(List<SingleRegion> singleRegion, Dictionary<Polyline, BlockReference> roomPlSuggestDict)
        {
            foreach (var sr in singleRegion)
            {
                var suggestDist = sr.SuggestDist;
                var route = -1;
                var length = 0.0;
                if (sr.MainPipe.Count > 0)
                {
                    route = sr.MainPipe[0];
                    length = ProcessedData.PipeList[sr.MainPipe[0]].ResultPolys.Sum(x => x.Length);
                    length = Math.Round(length / 1000, MidpointRounding.AwayFromZero);
                }
                var suggest = roomPlSuggestDict[sr.OriginalPl];
                if (suggest != null)
                {
                    ThFloorHeatingCoilInsertService.UpdateSuggestBlock(suggest, route + 1, suggestDist, length, true);
                }
                else
                {
                    var insertPt = sr.ClearedPl.GetCenter();
                    ThFloorHeatingCoilInsertService.InsertSuggestBlock(insertPt, route + 1, suggestDist, length, ThFloorHeatingCommon.BlkName_RoomSuggest, true);
                }
            }
        }


    }


}
