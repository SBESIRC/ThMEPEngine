using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute
{
    public class ChamferService
    {
        double length = 150;
        List<RouteModel> routes;
        public ChamferService(List<RouteModel> _routes)
        {
            routes = _routes;
        }

        /// <summary>
        /// 倒角
        /// </summary>
        public List<RouteModel> Chamfer()
        {
            foreach (var route in routes)
            {
                route.route = HandleChamfer(route.route);
            }
            return routes;
        }

        /// <summary>
        /// 处理倒角
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private Polyline HandleChamfer(Polyline polyline)
        {
            var resLines = new List<Line>();
            Line line = null;
            for (int i = 1; i < polyline.NumberOfVertices; i++)
            {
                if (line == null)
                {
                    line = new Line(polyline.GetPoint3dAt(i - 1), polyline.GetPoint3dAt(i));
                    resLines.Add(line);
                    continue;
                }
                var thisLine = new Line(polyline.GetPoint3dAt(i - 1), polyline.GetPoint3dAt(i));
                if (CheckAngle(line, thisLine))
                {
                    var resLine = ChamferByLine(line, thisLine, out Line resLine1, out Line resLine2);
                    if (resLine1 == null)
                    {
                        resLines.Remove(line);
                        resLines.Add(resLine1);
                    }
                    resLines.Add(resLine);
                    if (resLine2 != null)
                    {
                        resLines.Add(resLine2);
                        thisLine = resLine2;
                    }
                    else
                    {
                        line = null;
                    }
                }
                else
                {
                    resLines.Add(thisLine);
                }
                line = thisLine;
            }
            return CreateChamforByLine(resLines);
        }

        /// <summary>
        /// 重新构建倒角之后的polyline
        /// </summary>
        /// <param name="resLines"></param>
        /// <returns></returns>
        private Polyline CreateChamforByLine(List<Line> resLines)
        {
            Polyline resPoly = new Polyline();
            foreach (var rLine in resLines)
            {
                resPoly.AddVertexAt(resPoly.NumberOfVertices, rLine.StartPoint.ToPoint2D(), 0, 0, 0);
            }
            resPoly.AddVertexAt(resPoly.NumberOfVertices, resLines.Last().EndPoint.ToPoint2D(), 0, 0, 0);
            return resPoly;
        }

        /// <summary>
        /// 进行倒角处理
        /// </summary>
        /// <param name="line"></param>
        /// <param name="thisLine"></param>
        /// <param name="resLine1"></param>
        /// <param name="resLine2"></param>
        /// <returns></returns>
        private Line ChamferByLine(Line line, Line thisLine, out Line resLine1, out Line resLine2)
        {
            resLine1 = null;
            resLine2 = null;
            var sp = line.StartPoint;
            var ep = thisLine.EndPoint;
            if (line.Length > length)
            {
                var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                var lineEndP = line.EndPoint - lineDir * length;
                resLine1 = new Line(sp, lineEndP);
                sp = lineEndP;
            }
            if (thisLine.Length > length)
            {
                var lineDir = (thisLine.EndPoint - thisLine.StartPoint).GetNormal();
                var lineSP = thisLine.StartPoint + lineDir * length;
                resLine1 = new Line(lineSP, ep);
                ep = lineSP;
            }

            return new Line(sp, ep);
        }

        /// <summary>
        /// 检验角度是否需要做倒角
        /// </summary>
        /// <param name="line"></param>
        /// <param name="secLine"></param>
        /// <returns></returns>
        private bool CheckAngle(Line line, Line secLine)
        {
            var dir = (line.EndPoint - line.StartPoint).GetNormal();
            var secDir = (secLine.EndPoint - secLine.StartPoint).GetNormal();
            if (dir.DotProduct(secDir) < 0.01)
            {
                return true;
            }

            return false;
        }
    }
}
