using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NFox.Cad;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;

using ThMEPEngineCore.Diagnostics;

namespace ThMEPLighting.EmgLightConnect.Service
{
    public class commentLineService
    {

        public static List<Polyline> getCommentLine(Dictionary<Polyline, (Point3d, Vector3d)> layoutInfo, List<Polyline> columns)
        {
            var buffer = 800;
            var commentColumn = findCommentColumn(layoutInfo, columns);
            var commentLine = new List<Polyline>();
            foreach (var c in commentColumn)
            {
                var cClone = c.WashClone() as Polyline;
                cClone.Closed = true;
                var cBuffer = cClone.Buffer(buffer).Cast<Polyline>().FindByMax(o => o.Area);
                commentLine.Add(cBuffer);
            }

            DrawUtils.ShowGeometry(commentLine, "l0commentline", 2, 30);

            return commentLine;
        }

        private static List<Polyline> findCommentColumn(Dictionary<Polyline, (Point3d, Vector3d)> layoutInfo, List<Polyline> columns)
        {
            var tol = 1000;
            var commentColumn = new List<Polyline>();

            for (int i = 0; i < layoutInfo.Count; i++)
            {
                for (int j = i + 1; j < layoutInfo.Count; j++)
                {
                    var pt1 = layoutInfo.ElementAt(i).Value;
                    var pt2 = layoutInfo.ElementAt(j).Value;

                    if (pt1.Item1.DistanceTo(pt2.Item1) <= tol)
                    {
                        var angle = pt1.Item2.GetAngleTo(pt2.Item2);
                        if (angle <= 20 * Math.PI / .180)
                        {
                            var col = columns.OrderBy(x => x.DistanceTo(pt1.Item1, false)).FirstOrDefault();
                            commentColumn.Add(col);
                        }
                    }
                }
            }

            commentColumn = commentColumn.Distinct().ToList();

            return commentColumn;


        }


    }
}
