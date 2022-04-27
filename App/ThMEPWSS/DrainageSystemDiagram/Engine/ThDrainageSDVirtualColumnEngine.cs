using System;
using System.Collections.Generic;
using System.Linq;

using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPWSS.DrainageSystemDiagram.Service;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDVirtualColumnEngine
    {
        public static List<Polyline> getVirtualColumn(Dictionary<string, List<ThTerminalToilet>> groupList, Dictionary<string, (string, string)> islandPair, Dictionary<string, List<ThTerminalToilet>> allToiInGroup, Dictionary<ThTerminalToilet, Point3d> virtualPtDict)
        {
            var virtualColumn = new List<Polyline>();

            foreach (var group in groupList)
            {
                if (islandPair.ContainsKey(group.Key))
                {
                    //岛
                    var polys = getVitualColumnForIsland(groupList, islandPair[group.Key]);
                    virtualColumn.AddRange(polys);
                }
                else if (allToiInGroup.ContainsKey(group.Key))
                {
                    //小空间
                    var toilet = allToiInGroup[group.Key];
                    foreach (var t in toilet)
                    {
                        var poly = getVitualColumnForToilet(t);
                        if (poly != null && poly.NumberOfVertices > 0)
                        {
                            virtualColumn.Add(poly);
                        }
                    }
                }
                else
                {
                    //普通组
                    if (group.Value.SelectMany(x => x.SupplyCoolOnWall).Count() == 1)
                    {
                        var poly = getVitualColumnForToilet(group.Value.First());
                        if (poly != null && poly.NumberOfVertices > 0)
                        {
                            virtualColumn.Add(poly);
                        }
                    }
                }
            }
            return virtualColumn;
        }

        private static List<Polyline> getVitualColumnForIsland(Dictionary<string, List<ThTerminalToilet>> groupList, (string, string) island)
        {
            List<Polyline> columns = new List<Polyline>();

            var island1 = groupList[island.Item1];
            var island2 = groupList[island.Item2];

            columns.Add(getVitualColumnForStrightGroup(island1));
            columns.Add(getVitualColumnForStrightGroup(island2));

            return columns;

        }

        private static Polyline getVitualColumnForStrightGroup(List<ThTerminalToilet> group)
        {
            Polyline column = new Polyline();

            var pts = group.SelectMany(x => x.SupplyCoolOnWall).ToList();
            var orderPts = ThDrainageSDCommonService.orderPtInStrightLine(pts);
            var dir = group.First().Dir;

            double length = ThDrainageSDCommon.LengthSublink * 2;

            var pt1 = orderPts.First();
            var pt2 = orderPts.Last();
            var pt3 = pt2 + length * dir;
            var pt4 = pt1 + length * dir;


            column.AddVertexAt(column.NumberOfVertices, pt1.ToPoint2d(), 0, 0, 0);
            column.AddVertexAt(column.NumberOfVertices, pt2.ToPoint2d(), 0, 0, 0);
            column.AddVertexAt(column.NumberOfVertices, pt3.ToPoint2d(), 0, 0, 0);
            column.AddVertexAt(column.NumberOfVertices, pt4.ToPoint2d(), 0, 0, 0);

            column.Closed = true;

            return column;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="toilet"></param>
        /// <returns></returns>
        private static Polyline getVitualColumnForToilet(ThTerminalToilet toilet)
        {
            Polyline column = new Polyline();

            double verticalLength = ThDrainageSDCommon.LengthSublink - 100;
            double length = 100;

            Vector3d dir = toilet.Dir;
            Vector3d dirLeft = toilet.Dir.RotateBy(90 * Math.PI / 180, -Vector3d.ZAxis).GetNormal();
            Vector3d dirRight = toilet.Dir.RotateBy(90 * Math.PI / 180, Vector3d.ZAxis).GetNormal();

            var pt1 = toilet.SupplyCoolOnWall.First() + dirLeft * (length / 2);
            var pt2 = pt1 + dir * verticalLength;
            var pt3 = pt2 + dirRight * length;
            var pt4 = pt3 - dir * verticalLength;

            column.AddVertexAt(column.NumberOfVertices, pt1.ToPoint2d(), 0, 0, 0);
            column.AddVertexAt(column.NumberOfVertices, pt2.ToPoint2d(), 0, 0, 0);
            column.AddVertexAt(column.NumberOfVertices, pt3.ToPoint2d(), 0, 0, 0);
            column.AddVertexAt(column.NumberOfVertices, pt4.ToPoint2d(), 0, 0, 0);

            column.Closed = true;

            return column;
        }
    }
}
