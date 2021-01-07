using System;
using System.Linq;
using Linq2Acad;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.EmgLight.Service
{
    class StructureServiceLight
    {
        /// <summary>
        /// 获取停车线周边构建信息
        /// </summary>
        /// <param name="polylines"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public static List<Polyline> GetStruct(List<Line> lines, List<Polyline> polys, double tol)
        {
           
            var resPolys = lines.SelectMany(x =>
            {
                var linePoly = StructUtils.ExpandLine(x, tol,0,tol,0);
                //InsertLightService.ShowGeometry(linePoly, 44);
                return polys.Where(y =>
                {
                    var polyCollection = new DBObjectCollection() { y };
                    return linePoly.Contains(y) || linePoly.Intersects(y);
                }).ToList();
            }).ToList();

            return resPolys;
        }


        /// <summary>
        /// 沿着线将柱分隔成上下两部分
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public static List<List<Polyline>> SeparateColumnsByLine(List<Polyline> polyline, List<Line> lines, double length)
        {

            var linePolys = StructUtils.createRecBuffer(lines, length);
            
            //InsertLightService.ShowGeometry(linePolys, 30);
            List<Polyline> upPolyline = new List<Polyline>();
            List<Polyline> downPolyline = new List<Polyline>();
            foreach (var poly in polyline)
            {
                var intersecRes = linePolys.Where(x => x.Contains(poly) || x.Intersects(poly)).ToList();
                 intersecRes = linePolys.Where(x => x.Contains(poly.StartPoint) || x.Intersects(poly)).ToList();
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



    }
}
