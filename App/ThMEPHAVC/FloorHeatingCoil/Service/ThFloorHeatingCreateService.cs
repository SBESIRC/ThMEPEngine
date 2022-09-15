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

namespace ThMEPHVAC.FloorHeatingCoil.Service
{
    public class ThFloorHeatingCreateService
    {
        /// <summary>
        /// data: (0,0,0) selectFrame:far away
        /// </summary>
        /// <param name="vm"></param>
        /// <param name="selectFrames"></param>
        /// <param name="transformer"></param>
        /// <returns></returns>
        //public static ThFloorHeatingDataProcessService CreateSRData(ThFloorHeatingCoilViewModel vm, ref List<Polyline> selectFrames, ref ThMEPOriginTransformer transformer,bool withUI)
        //{
        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    {
        //        if (vm.SelectFrame.Count == 0)
        //        {
        //            ThSelectFrameUtil.SelectPolyline().ForEach(x => vm.SelectFrame.Add(x));
        //        }
        //        selectFrames.AddRange(vm.SelectFrame.ToList());
        //        if (selectFrames.Count == 0)
        //        {
        //            transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));
        //            return new ThFloorHeatingDataProcessService();
        //        }

        //        //transformer = new ThMEPOriginTransformer(selectFrames[0].GetPoint3dAt(0));
        //        //transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));
        //        transformer = ThFloorHeatingCoilUtilServices.GetTransformer(selectFrames,false);

        //        var dataQuery = ThFloorHeatingCoilUtilServices.GetData(acadDatabase, selectFrames, transformer, withUI);

        //        return dataQuery;
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
                        noRoomSuggestRegion.Add(sr);
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

                    var suggest = roomPlSuggestDict[sr.OriginalPl];
                    if (suggest != null)
                    {
                        ThFloorHeatingDataProcessService.GetSuggestData(suggest, out var route, out var suggestDist, out var length);
                        sr.SuggestDist = suggestDist;
                    }

                }
            }

            return needUpdateSR;

        }

        public static void UpdateSRSuggestBlock(List<SingleRegion> regionList, List<SinglePipe> pipeList, Dictionary<Polyline, BlockReference> roomPlSuggestDict, bool updateWaterSeparatorRoom, ThMEPOriginTransformer transformer)
        {
            var routeDict = SortSingleRegionRoute(pipeList);

            foreach (var sr in regionList)
            {
                var suggestDist = sr.SuggestDist;
                var route = -2;
                var length = 0.0;
                if (sr.MainPipe.Count > 0)
                {
                    route = routeDict[sr.MainPipe[0]];
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
                    if (updateWaterSeparatorRoom == false && sr.HaveEquipment == 1)
                    {
                        //住宅模式跳过集分水器在的房间
                        continue;
                    }
                    var insertPt = sr.ClearedPl.GetCenterInPolyline();
                    transformer.Reset(ref insertPt);
                    ThFloorHeatingCoilInsertService.InsertSuggestBlock(insertPt, route + 1, suggestDist, length, ThFloorHeatingCommon.BlkName_RoomSuggest, true);
                }
            }
        }

        private static Dictionary<int, int> SortSingleRegionRoute(List<SinglePipe> pipeList)
        {
            var routeDict = new Dictionary<int, int>();
            var realIdx = 0;
            for (int i = 0; i < pipeList.Count; i++)
            {
                var pipe = pipeList[i];
                if (pipe.ResultPolys != null && pipe.ResultPolys.Count > 0 && pipe.ResultPolys[0].Length > 1)
                {
                    routeDict.Add(i, realIdx);
                    realIdx = realIdx + 1;
                }
                else
                {
                    //长度为零的管线，跳过
                    routeDict.Add(i, -1);
                }

            }
            return routeDict;
        }

    }
}
