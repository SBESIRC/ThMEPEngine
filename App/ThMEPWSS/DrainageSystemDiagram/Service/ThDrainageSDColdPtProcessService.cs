using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThCADCore.NTS;


namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDColdPtProcessService
    {

        public static Dictionary<string, List<ThIfcSanitaryTerminalToilate>> classifyToilate(List<ThIfcSanitaryTerminalToilate> toilateList)
        {
            Dictionary<string, List<ThIfcSanitaryTerminalToilate>> groupToilate = new Dictionary<string, List<ThIfcSanitaryTerminalToilate>>();

            //classify
            for (int i = 0; i < toilateList.Count; i++)
            {
                string groupid = toilateList[i].GroupId;
                if (groupid != null && groupToilate.ContainsKey(groupid) == false)
                {
                    groupToilate.Add(groupid, new List<ThIfcSanitaryTerminalToilate>() { toilateList[i] });
                }
                else if (groupid != null)
                {
                    groupToilate[groupid].Add(toilateList[i]);
                }
            }

            //debug draw
            for (int i = 0; i < groupToilate.Count(); i++)
            {
                groupToilate.ElementAt(i).Value.ForEach(toilate =>
                 {
                     toilate.SupplyCoolOnWall.ForEach(x => DrawUtils.ShowGeometry(x, "l0group", (Int16)(i % 6), 25, 40, "C"));
                 });
            }


            return groupToilate;

        }


        /// <summary>
        /// 不支持3组岛合并 只有两两合并.可能有bug
        /// </summary>
        /// <param name="groupList"></param>
        /// <returns></returns>
        public static Dictionary<string, (string,string)> mergeIsland(Dictionary<string, List<ThIfcSanitaryTerminalToilate>> groupList)
        {
            int TolSameIsland = 800;

            Dictionary<string, (string, string)> mergeIslandGroup = new Dictionary<string, (string, string)> ();

            var islandGroup = groupList.Where(x => x.Key.Contains(DrainageSDCommon.islandTag)).ToList();
            
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

        /// <summary>
        /// 有问题 没写完
        /// </summary>
        /// <param name="pl"></param>
        /// <returns></returns>
        private static List<Point3d> findConcavePT(Polyline pl)
        {
            var concavePtList = new List<Point3d>();

            var convexPt = pl.GetPoint3dAt(0);
            int convexIdx = 0;

            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                var pt = pl.GetPoint3dAt(i % pl.NumberOfVertices);
                if (pt.X < convexPt.X)
                {
                    convexPt = pt;
                    convexIdx = i;
                }
                else if (pt.X == convexPt.X && pt.Y > convexPt.Y)
                {
                    convexPt = pt;
                    convexIdx = i;
                }
            }
            DrawUtils.ShowGeometry(convexPt, "l0convex", 3, 25, 20, "S");

            Vector3d preConvex = pl.GetPoint3dAt((convexIdx - 1) % pl.NumberOfVertices) - convexPt;
            Vector3d nextConvex = pl.GetPoint3dAt((convexIdx + 1) % pl.NumberOfVertices) - convexPt;
            var a = preConvex.CrossProduct(nextConvex);

            for (int i = convexIdx + 1; i < pl.NumberOfVertices + convexIdx; i++)
            {
                var thisPt = pl.GetPoint3dAt(i % pl.NumberOfVertices);
                var prePT = pl.GetPoint3dAt((i - 1) % pl.NumberOfVertices);
                var nextPT = pl.GetPoint3dAt((i + 1) % pl.NumberOfVertices);

                Vector3d preV = thisPt - prePT;
                Vector3d nextV = nextPT - thisPt;
                var b = preV.CrossProduct(nextV);

                //if (a*b >0)
                //{

                //}
            }


            return concavePtList;
        }
    }
}
