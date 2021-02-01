using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.ConnectPipe.Dijkstra;
using ThMEPEngineCore.CAD;

namespace ThMEPElectrical.ConnectPipe.Service
{
    public class PathfindingUitlsService
    {
        public Polyline Pathfinding(KeyValuePair<Polyline, List<Polyline>> holeInfo, List<List<Polyline>> mainPolys, List<List<Polyline>> endingPolys, List<Polyline> fingdingPoly)
        {
            var sPts = FindingPolyPoints(fingdingPoly);
            var polyPts = mainPolys.Select(x => FindingPolyPoints(x)).ToList();

            var checkPolys = endingPolys.SelectMany(x => x).ToList();
            double maxLength = double.PositiveInfinity;
            Polyline maxPoly = null;
            List<Polyline> allPolys = new List<Polyline>(); ;
            foreach (var sPt in sPts)
            {
                foreach (var polyPt in polyPts)
                {
                    var connectPt = polyPt.OrderBy(x => x.DistanceTo(sPt)).First();
                    var connectPoly = new Polyline();
                    connectPoly.AddVertexAt(0, sPt.ToPoint2D(), 0, 0, 0);
                    connectPoly.AddVertexAt(1, connectPt.ToPoint2D(), 0, 0, 0);
                    allPolys.Add(connectPoly);
                    if (!CheckService.CheckConnectLines(holeInfo, connectPoly, checkPolys))
                    {
                        continue;
                    }

                    var findingCheckPolys = new List<Polyline>(checkPolys);
                    findingCheckPolys.Add(connectPoly);
                    DijkstraAlgorithm dijkstra = new DijkstraAlgorithm(findingCheckPolys.Cast<Curve>().ToList());
                    var length = dijkstra.FindingAllPathMinLength(sPt).OrderByDescending(x => x).First();
                    if (length < maxLength)
                    {
                        maxLength = length;
                        maxPoly = connectPoly;
                    }
                }
            }

            if (maxPoly == null)
            {
                maxPoly = allPolys.OrderBy(x => x.Length).First();
            }
            return maxPoly;
        }

        /// <summary>
        /// 找到点位
        /// </summary>
        /// <param name="fingdingPoly"></param>
        /// <returns></returns>
        private List<Point3d> FindingPolyPoints(List<Polyline> fingdingPoly)
        {
            List<Point3d> sPts = new List<Point3d>();
            foreach (var poly in fingdingPoly)
            {
                if (sPts.Where(x => x.IsEqualTo(poly.StartPoint, new Tolerance(1, 1))).Count() <= 0)
                {
                    sPts.Add(poly.StartPoint);
                }

                if (sPts.Where(x => x.IsEqualTo(poly.EndPoint, new Tolerance(1, 1))).Count() <= 0)
                {
                    sPts.Add(poly.EndPoint);
                }
            }

            return sPts;
        }
    }
}
