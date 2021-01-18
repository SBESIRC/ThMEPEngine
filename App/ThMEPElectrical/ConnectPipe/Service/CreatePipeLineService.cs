using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.ConnectPipe.Service
{
    public class CreatePipeLineService
    {
        public void CreatePipe(List<Polyline> connectPolys, List<BlockReference> broadcasts)
        {
            connectPolys = MergeConnectPolys(connectPolys);
            foreach (var cPoly in connectPolys)
            {
                List<BlockReference> connectBroadcast = broadcasts.Where(x => x.Position.IsEqualTo(cPoly.StartPoint)
                    || x.Position.IsEqualTo(cPoly.EndPoint)).ToList();
            }
            using (AcadDatabase db = AcadDatabase.Active())
            {
                foreach (var item in connectPolys)
                {
                    db.ModelSpace.Add(item);
                }
            }
        }

        /// <summary>
        /// 合并平齐polyline
        /// </summary>
        /// <param name="connectPolys"></param>
        private List<Polyline> MergeConnectPolys(List<Polyline> connectPolys)
        {
            var polyDic = CalLongestLineByPoly(connectPolys);
            List<Polyline> resPolys = new List<Polyline>();
            while (polyDic.Count > 0)
            {
                var poly = polyDic.First();
                var dir = (poly.Value.EndPoint - poly.Value.StartPoint).GetNormal();
                var connectPolyDic = polyDic.Where(x => (x.Key.StartPoint.IsEqualTo(poly.Key.StartPoint) 
                    || x.Key.StartPoint.IsEqualTo(poly.Key.EndPoint)
                    || x.Key.EndPoint.IsEqualTo(poly.Key.StartPoint)
                    || x.Key.EndPoint.IsEqualTo(poly.Key.EndPoint)))
                    .Where(x=> {
                        var xDir = (x.Value.EndPoint - x.Value.StartPoint).GetNormal();
                        if (xDir.IsParallelTo(dir, new Tolerance(0.1, 0.1)) && GeUtils.CalParallelLineDistance(poly.Value, x.Value) > 1)
                        {
                            return true;
                        }
                        return false;
                    })
                    .ToList();
                if (connectPolyDic.Count > 0)
                {
                    var firPoly = connectPolyDic.First();
                    var secPoly = poly;
                    if (firPoly.Key.StartPoint.DistanceTo(firPoly.Value.StartPoint) < secPoly.Key.StartPoint.DistanceTo(secPoly.Value.StartPoint))
                    {
                        firPoly = secPoly;
                        secPoly = connectPolyDic.First();
                    }

                    var resPoly = MergePolysByLine(firPoly, secPoly);
                    polyDic.Remove(secPoly.Key);
                    polyDic.Add(resPoly.Key, resPoly.Value);
                    continue;
                }

                resPolys.Add(poly.Key);
                polyDic.Remove(poly.Key);
            }

            return resPolys;
        }

        /// <summary>
        /// 将第二个polyline移动到第一个polyline平齐
        /// </summary>
        /// <param name="firPoly"></param>
        /// <param name="secPoly"></param>
        /// <returns></returns>
        private KeyValuePair<Polyline, Line> MergePolysByLine(KeyValuePair<Polyline, Line> firPoly, KeyValuePair<Polyline, Line> secPoly)
        {
            double distance = GeUtils.CalParallelLineDistance(firPoly.Value, secPoly.Value);
            var moveDir = Vector3d.ZAxis.CrossProduct((secPoly.Value.EndPoint - secPoly.Value.StartPoint).GetNormal());
            var compareDir = (firPoly.Value.StartPoint - secPoly.Value.StartPoint).GetNormal();
            if (moveDir.DotProduct(compareDir) < 0)
            {
                moveDir = -moveDir;
            }
            var moveLine = new Line(secPoly.Value.StartPoint + distance * moveDir, secPoly.Value.EndPoint + distance * moveDir);
            using (AcadDatabase dv= AcadDatabase.Active())
            {
                moveLine.ColorIndex = 3;
                dv.ModelSpace.Add(moveLine);
            }
            Polyline polyline = new Polyline();
            polyline.AddVertexAt(0, secPoly.Key.StartPoint.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(1, moveLine.StartPoint.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(2, moveLine.EndPoint.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(3, secPoly.Key.EndPoint.ToPoint2D(), 0, 0, 0);

            return new KeyValuePair<Polyline, Line>(polyline, moveLine);
        }

        /// <summary>
        /// 找到polyline中的最长线
        /// </summary>
        /// <param name="connectPolys"></param>
        /// <returns></returns>
        private Dictionary<Polyline, Line> CalLongestLineByPoly(List<Polyline> connectPolys)
        {
            Dictionary<Polyline, Line> polyDic = new Dictionary<Polyline, Line>();
            foreach (var poly in connectPolys)
            {
                List<Line> lines = new List<Line>();
                for (int i = 0; i < poly.NumberOfVertices - 1; i++)
                {
                    lines.Add(new Line(poly.GetPoint3dAt(i), poly.GetPoint3dAt(i + 1)));
                }
                var longestLine = lines.OrderByDescending(x => x.Length).First();
                polyDic.Add(poly, longestLine);
            }

            return polyDic;
        }

        private void ConnectBroadcast(Polyline polyline, List<BlockReference> broadcasts)
        {

        }
    }
}
