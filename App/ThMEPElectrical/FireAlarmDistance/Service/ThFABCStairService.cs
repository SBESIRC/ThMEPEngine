﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Stair;
using ThMEPEngineCore.Model;

using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.FireAlarmDistance.Model;

namespace ThMEPElectrical.FireAlarmDistance.Service
{
    public class ThFABCStairService
    {

        /// <summary>
        /// 楼梯部分布置
        /// 最终结果点位写到layoutParameter
        /// 返回最终布置点位，方向
        /// </summary>
        /// <param name="dataQuery"></param>
        /// <param name="layoutParameter"></param>
        /// <returns></returns>
        public static List<ThLayoutPt> LayoutStair(ThAFASBCLayoutParameter layoutParameter)
        {
            var pts = layoutParameter.framePts;
            var scale = layoutParameter.Scale;
            var resultPts = new List<ThLayoutPt>();
            var obstacle = new List<Polyline>();

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //obstacle
                obstacle.AddRange(layoutParameter.Data.DoorOpenings.Select(x => x.Boundary as Polyline).ToList());
                obstacle.AddRange(layoutParameter.Data.Windows.Select(x => x.Boundary as Polyline).ToList());

                var stairBoundary = FindStairRoomBoundary(layoutParameter.Data.Rooms);
               
                var stairEngine = new ThStairEquimentLayout();
                var stairFireDetector = stairEngine.StairBroadcast(acadDatabase.Database, stairBoundary, obstacle, pts, scale);
                var stairFirePts = stairFireDetector.Select(x => x.Key).ToList();
                foreach (var r in stairFireDetector)
                {
                    resultPts.Add(new ThLayoutPt() { Pt = r.Key, Angle = r.Value, BlkName = layoutParameter.BlkNameBroadcast });
                }

                layoutParameter.StairPartResult.AddRange(stairFirePts);
                ////

                return resultPts;
            }
        }

        private static List<Polyline> FindStairRoomBoundary(List<ThGeometry> Room)
        {
            string roomConfigUrl = ThCADCommon.RoomConfigPath();
            var roomTableTree = ThAFASRoomUtils.ReadRoomConfigTable(roomConfigUrl);
            var stairName = ThFaCommon.stairName;
            var stairBoundary = new List<Polyline>();
            for (int i = 0; i < Room.Count; i++)
            {
                var room = Room[i];
                var roomName = room.Properties[ThExtractorPropertyNameManager.NamePropertyName].ToString();
                if (ThAFASRoomUtils.IsRoom(roomTableTree, roomName, stairName))
                {
                    stairBoundary.Add(room.Boundary as Polyline);
                }
            }
            return stairBoundary;
        }

        public static void CleanStairRoom(ThAFASBCLayoutParameter layoutParameter)
        {
            var Room = layoutParameter.Data.Rooms;
            var tempClean = new List<ThGeometry>();
            for (int i = 0; i < Room.Count; i++)
            {
                var roomBoundary = Room[i].Boundary as Polyline;
                var blkInStair = layoutParameter.StairPartResult.Where(x => roomBoundary.Contains(x));
                if (blkInStair.Count() > 0)
                {
                    tempClean.Add(Room[i]);
                }
            }

            layoutParameter.Data.Data.RemoveAll(x => tempClean.Contains(x));

        }

        public static void CleanStairRoomPt(List<Point3d> PtInStair, List<Polyline> Room, ref List<Point3d> PtResult)
        {
            var PtClean = new List<Point3d>();

            for (int i = 0; i < PtInStair.Count; i++)
            {
                var ptStair = PtInStair[i];
                var room = Room.Where(x => x.Contains(ptStair)).FirstOrDefault();
                if (room != null)
                {
                    var PtNeedClean = PtResult.Where(x => room.ContainsOrOnBoundary (x)).ToList();
                    PtClean.AddRange(PtNeedClean);
                }
            }

            PtResult.RemoveAll(x => PtClean.Contains(x));
        }
    }
}
