using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using NFox.Cad;


namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDCoolPtProcessService
    {

        public static Dictionary<string, List<ThTerminalToilate>> classifyToilate(List<ThTerminalToilate> toilateList)
        {
            Dictionary<string, List<ThTerminalToilate>> groupToilate = new Dictionary<string, List<ThTerminalToilate>>();

            //classify
            for (int i = 0; i < toilateList.Count; i++)
            {
                string groupid = toilateList[i].GroupId;
                if (groupid != null && groupToilate.ContainsKey(groupid) == false)
                {
                    groupToilate.Add(groupid, new List<ThTerminalToilate>() { toilateList[i] });
                }
                else if (groupid != null)
                {
                    groupToilate[groupid].Add(toilateList[i]);
                }
            }

            return groupToilate;

        }


        /// <summary>
        /// 不支持3组岛合并 只有两两合并.可能有bug
        /// </summary>
        /// <param name="groupList"></param>
        /// <returns></returns>
        public static Dictionary<string, (string, string)> mergeIsland(Dictionary<string, List<ThTerminalToilate>> groupList)
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

        public static void classifySmallRoomGroup(ref Dictionary<string, List<ThTerminalToilate>> groupList, List<ThToilateRoom> roomList)
        {
            var groupSmall = groupList.Where(x => x.Key.Contains(ThDrainageSDCommon.tagSmallRoom)).ToList();
            var groupSmallString = groupSmall.Select(x => x.Key).ToList();

            foreach (var groupName in groupSmallString)
            {
                var room = findRoomToilateBelongsTo(groupList[groupName][0], roomList);
                var subGroup = new Dictionary<Line, List<ThTerminalToilate>>();

                foreach (var toi in groupList[groupName])
                {
                    var wall = findToilateBelongsToWall(toi, room);
                    if (subGroup.ContainsKey(wall) == false)
                    {
                        subGroup.Add(wall, new List<ThTerminalToilate>() { toi });
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

        public static Line findToilateToWall(ThTerminalToilate toilate, List<ThToilateRoom> roomList)
        {
            Line wall = null;
            var room = findRoomToilateBelongsTo(toilate, roomList);

            if (room != null)
            {
                wall = findToilateBelongsToWall(toilate, room);
            }

            return wall;
        }

        public static ThToilateRoom findRoomToilateBelongsTo(ThTerminalToilate toilate, List<ThToilateRoom> roomList)
        {
            var room = roomList.Where(x => x.toilate.Contains(toilate)).FirstOrDefault();
            return room;
        }

        private static Line findToilateBelongsToWall(ThTerminalToilate toilate, ThToilateRoom room)
        {
            var tol = new Tolerance(10, 10);
            var walls = room.wallList;

            var wall = walls.Where(x => x.ToCurve3d().IsOn(toilate.SupplyCoolOnWall.First(), tol)).FirstOrDefault();
            return wall;
        }

    }
}
