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
    public class CorrectPipeConnectService
    {
        public List<Polyline> CorrectPipe(List<Polyline> connectPolys)
        {
            return MergeConnectPolys(connectPolys);
        }

        /// <summary>
        /// 合并平齐polyline
        /// </summary>
        /// <param name="connectPolys"></param>
        private List<Polyline> MergeConnectPolys(List<Polyline> connectPolys)
        {
            var polyDic = GeUtils.CalLongestLineByPoly(connectPolys);
            List<Polyline> resPolys = new List<Polyline>();
            int index = 0;
            while (polyDic.Count > 0)
            {
                if (index > 100)
                {
                    break;
                }
                index++;
                var poly = polyDic.First();
                var dir = (poly.Value.EndPoint - poly.Value.StartPoint).GetNormal();
                var connectPolyDic = polyDic.Where(x => (x.Key.StartPoint.IsEqualTo(poly.Key.StartPoint)
                    || x.Key.StartPoint.IsEqualTo(poly.Key.EndPoint)
                    || x.Key.EndPoint.IsEqualTo(poly.Key.StartPoint)
                    || x.Key.EndPoint.IsEqualTo(poly.Key.EndPoint)))
                    .Where(x => {
                        var xDir = (x.Value.EndPoint - x.Value.StartPoint).GetNormal();
                        var dsitance = GeUtils.CalParallelLineDistance(poly.Value, x.Value);
                        if (xDir.IsParallelTo(dir, new Tolerance(0.001, 0.001)) && dsitance > 10 && dsitance < 1000)
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
            
            Polyline polyline = new Polyline();
            polyline.AddVertexAt(0, secPoly.Key.StartPoint.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(1, moveLine.StartPoint.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(2, moveLine.EndPoint.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(3, secPoly.Key.EndPoint.ToPoint2D(), 0, 0, 0);

            return new KeyValuePair<Polyline, Line>(polyline, moveLine);
        }
    }
}
