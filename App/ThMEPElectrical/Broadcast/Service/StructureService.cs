using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPElectrical.Broadcast.Service
{
    public class StructureService
    {
        /// <summary>
        /// 获取停车线周边构建信息
        /// </summary>
        /// <param name="polylines"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public List<Polyline> GetStruct(List<Line> lines, List<Polyline> polys, double tol) 
        {
            var resPolys = lines.SelectMany(x =>
            {
                var linePoly = StructUtils.ExpandLine(x, tol);
                var polyCollection = new DBObjectCollection() { linePoly };
                return polys.Where(y => y.Intersection(polyCollection).Count > 0).ToList();
                //return linePoly.Intersection(polyCollection).Cast<Polyline>().ToList();
            }).ToList();

            return resPolys;
        }

        /// <summary>
        /// 沿着线将柱分隔成上下两部分
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public List<List<Polyline>> SeparateColumnsByLine(List<Polyline> polyline, List<Line> lines, double length)
        {
            var newLines = lines.Select(x => x.Normalize()).ToList();
            List<Polyline> linePolys = new List<Polyline>();
            foreach (var line in newLines)
            {
                var bufferLength = length;
                var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                if (Math.Abs(lineDir.X) > Math.Abs(lineDir.Y))
                {
                    if (lineDir.X < 0)
                    {
                        bufferLength = -bufferLength;
                    }
                }
                else
                {
                    if (lineDir.Y < 0)
                    {
                        bufferLength = -bufferLength;
                    }
                }

                linePolys.AddRange(new DBObjectCollection() { line }.SingleSidedBuffer(bufferLength).Cast<Polyline>().ToList());
            }
           
            List<Polyline> upPolyline = new List<Polyline>();
            List<Polyline> downPolyline = new List<Polyline>();
            foreach (var poly in polyline)
            {
                var intersecRes = linePolys.Where(x => x.Contains(poly) || x.Intersects(poly)).ToList();
                if (intersecRes.Count > 0)
                {
                    upPolyline.Add(poly);
                }
                else
                {
                    downPolyline.Add(poly);
                }
            }

            return new List<List<Polyline>>() { upPolyline, downPolyline };
        }

        /// <summary>
        /// 沿着线将柱分隔成上下两部分
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public List<List<Polyline>> SeparateColumnsByLine(List<Polyline> polyline, List<Line> lines)
        {
            Dictionary<Line, List<Polyline>> groupDic = new Dictionary<Line, List<Polyline>>();
            foreach (var poly in polyline)
            {
                var closetLine = lines.OrderBy(x => x.Distance(poly)).First();
                if (groupDic.ContainsKey(closetLine))
                {
                    groupDic[closetLine].Add(poly);
                }
                else
                {
                    groupDic.Add(closetLine, new List<Polyline>() { poly });
                }
            }

            List<Polyline> upPolyline = new List<Polyline>();
            List<Polyline> downPolyline = new List<Polyline>();
            foreach (var lineDic in groupDic)
            {
                Vector3d xDir = (lineDic.Key.EndPoint - lineDic.Key.StartPoint).GetNormal();
                Vector3d yDir = Vector3d.ZAxis.CrossProduct(xDir);
                Vector3d zDir = Vector3d.ZAxis;
                Matrix3d matrix = new Matrix3d(
                    new double[] {
                    xDir.X, yDir.X, zDir.X, lineDic.Key.StartPoint.X,
                    xDir.Y, yDir.Y, zDir.Y, lineDic.Key.StartPoint.Y,
                    xDir.Z, yDir.Z, zDir.Z, lineDic.Key.StartPoint.Z,
                    0.0, 0.0, 0.0, 1.0
                    });

                foreach (var poly in lineDic.Value)
                {
                    var transPt = StructUtils.GetStructCenter(poly).TransformBy(matrix.Inverse());
                    if (transPt.Y < 0)
                    {
                        downPolyline.Add(poly);
                    }
                    else
                    {
                        upPolyline.Add(poly);
                    }
                }
            }
            
            return new List<List<Polyline>>() { upPolyline, downPolyline };
        }
    }
}
