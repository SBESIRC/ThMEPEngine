using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using NFox.Cad;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDCoolPtProcessService
    {

        public static Dictionary<string, List<ThTerminalToilet>> classifyToilet(List<ThTerminalToilet> toiletList)
        {
            Dictionary<string, List<ThTerminalToilet>> groupToilet = new Dictionary<string, List<ThTerminalToilet>>();

            //classify
            for (int i = 0; i < toiletList.Count; i++)
            {
                string groupid = toiletList[i].GroupId;
                if (groupid != null && groupid!="" && groupToilet.ContainsKey(groupid) == false)
                {
                    groupToilet.Add(groupid, new List<ThTerminalToilet>() { toiletList[i] });
                }
                else if (groupid != null && groupid != "")
                {
                    groupToilet[groupid].Add(toiletList[i]);
                }
            }

            return groupToilet;
        }

        /// <summary>
        /// 不支持3组岛合并 只有两两合并.可能有bug
        /// </summary>
        /// <param name="groupList"></param>
        /// <returns></returns>
        public static Dictionary<string, (string, string)> mergeIsland(Dictionary<string, List<ThTerminalToilet>> groupList)
        {
            int TolSameIsland = 800;

            Dictionary<string, (string, string)> mergeIslandGroup = new Dictionary<string, (string, string)>();

            var islandGroup = groupList.Where(x => x.Key.Contains(ThDrainageSDCommon.tagIsland)).ToList();

            for (int i = 0; i < islandGroup.Count; i++)
            {
                if (mergeIslandGroup.ContainsKey(islandGroup[i].Key))
                {
                    continue;
                }
                var pts1 = islandGroup[i].Value.SelectMany(x => x.SupplyCoolOnWall).ToList();

                for (int j = i + 1; j < islandGroup.Count; j++)
                {
                    var pts2 = islandGroup[j].Value.SelectMany(x => x.SupplyCoolOnWall).ToList();

                    if (distInRange(pts1, pts2, TolSameIsland))
                    {
                        mergeIslandGroup.Add(islandGroup[i].Key, (islandGroup[i].Key, islandGroup[j].Key));
                        mergeIslandGroup.Add(islandGroup[j].Key, (islandGroup[j].Key, islandGroup[i].Key));
                        break;
                    }
                }
            }

            return mergeIslandGroup;

        }

        private static bool distInRange(List<Point3d> pts1, List<Point3d> pts2, int tol)
        {
            var bReturn = false;

            for (int i = 0; i < pts1.Count; i++)
            {
                if (bReturn == true)
                {
                    break;
                }
                for (int j = 0; j < pts2.Count; j++)
                {
                    var dist = pts1[i].DistanceTo(pts2[j]);
                    if (dist <= tol)
                    {
                        bReturn = true;
                        break;
                    }
                }
            }

            return bReturn;
        }

        public static void classifySmallRoomGroup(ref Dictionary<string, List<ThTerminalToilet>> groupList, List<ThToiletRoom> roomList)
        {
            var groupSmall = groupList.Where(x => x.Key.Contains(ThDrainageSDCommon.tagSmallRoom)).ToList();
            var groupSmallString = groupSmall.Select(x => x.Key).ToList();

            foreach (var groupName in groupSmallString)
            {
                var room = findRoomToiletBelongsTo(groupList[groupName][0], roomList);
                var subGroup = new Dictionary<Line, List<ThTerminalToilet>>();

                foreach (var toi in groupList[groupName])
                {
                    var wall = findToiletBelongsToWall(toi, room);
                    if (subGroup.ContainsKey(wall) == false)
                    {
                        subGroup.Add(wall, new List<ThTerminalToilet>() { toi });
                    }
                    else
                    {
                        subGroup[wall].Add(toi);
                    }
                }

                if (subGroup.Count > 1)
                {
                    groupList.Remove(groupName);

                    for (int i = 0; i < subGroup.Count; i++)
                    {
                        groupList.Add(String.Format("{0}-{1}", groupName, i), subGroup.ElementAt(i).Value);
                    }
                }
            }
        }

        public static Line findToiletToWall(ThTerminalToilet toilet, List<ThToiletRoom> roomList)
        {
            Line wall = null;
            var room = findRoomToiletBelongsTo(toilet, roomList);

            if (room != null)
            {
                wall = findToiletBelongsToWall(toilet, room);
            }

            return wall;
        }

        public static ThToiletRoom findRoomToiletBelongsTo(ThTerminalToilet toilet, List<ThToiletRoom> roomList)
        {
            var room = roomList.Where(x => x.toilet.Contains(toilet)).FirstOrDefault();
            return room;
        }

        private static Line findToiletBelongsToWall(ThTerminalToilet toilet, ThToiletRoom room)
        {
            var tol = new Tolerance(10, 10);
            var walls = room.wallList;

            var wall = walls.Where(x => x.ToCurve3d().IsOn(toilet.SupplyCoolOnWall.First(), tol)).FirstOrDefault();
            return wall;
        }
    }
}
