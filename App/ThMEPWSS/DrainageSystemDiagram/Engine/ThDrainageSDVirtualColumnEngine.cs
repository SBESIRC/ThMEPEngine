using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;


namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDVirtualColumnEngine
    {
        public static List<Polyline> getVirtualColumn(Dictionary<string, List<ThIfcSanitaryTerminalToilate>> groupList, Dictionary<string, (string, string)> islandPair, Dictionary<string, List<ThIfcSanitaryTerminalToilate>> allToiInGroup, Dictionary<ThIfcSanitaryTerminalToilate, Point3d> virtualPtDict)
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
                }
                else
                {
                    //普通组
                    //var poly = getVitualColumnForStrightGroup(group.Value);
                    //virtualColumn.Add(poly);
                }


            }
            return virtualColumn;
        }


        private static List<Polyline> getVitualColumnForIsland(Dictionary<string, List<ThIfcSanitaryTerminalToilate>> groupList, (string, string) island)
        {
            List<Polyline> columns = new List<Polyline>();

            var island1 = groupList[island.Item1];
            var island2 = groupList[island.Item2];

            columns.Add(getVitualColumnForStrightGroup(island1));
            columns.Add(getVitualColumnForStrightGroup(island2));

            return columns;

        }

        private static Polyline getVitualColumnForStrightGroup(List<ThIfcSanitaryTerminalToilate> group)
        {
            Polyline column = new Polyline();

            var pts = group.SelectMany(x => x.SupplyCoolOnWall).ToList();
            var orderPts = ThDrainageSDCommonService.orderPtInStrightLine(pts);
            var dir = group.First().Dir;

            double length = DrainageSDCommon.SublinkLength * 2;

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
        /// 不可用
        /// </summary>
        /// <param name="toilate"></param>
        /// <returns></returns>
        private static Polyline getVitualColumnForToilates(ThIfcSanitaryTerminalToilate toilate)
        {
            Polyline column = new Polyline();

            Vector3d dir = toilate.Dir;
            Vector3d dirVerti = toilate.Dir.RotateBy(90 * Math.PI / 180, Vector3d.ZAxis).GetNormal();

            double vertiLength = toilate.Boundary.GetPoint3dAt(2).DistanceTo(toilate.Boundary.GetPoint3dAt(1));
            double length = DrainageSDCommon.SublinkLength * 2;



            return column;

        }
    }

}
